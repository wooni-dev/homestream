using System.Net;
using System.Net.Sockets;
using System.Text;
using HomeStream.Web;

namespace HomeStream.Server;

internal sealed class StreamServer
{
    public int ActualPort { get; private set; }
    public string ServeDir { get; private set; } = "";
    public AuthManager Auth { get; } = new();

    private TcpListener? _tcp;
    private CancellationTokenSource? _cts;

    public void Start(string serveDir)
    {
        Start();
        ServeDir = serveDir;
    }

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        for (int port = 8000; port < 8010; port++)
        {
            try
            {
                var tcp = new TcpListener(IPAddress.Any, port);
                tcp.Start();
                _tcp = tcp;
                ActualPort = port;
                break;
            }
            catch (SocketException)
            {
                if (port == 8009) throw new IOException("포트 8000~8009 모두 사용 중");
            }
        }

        var tcp2 = _tcp!;
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await tcp2.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleClient(client), token);
                }
                catch { break; }
            }
        }, token);
    }

    public void SetServeDir(string serveDir) => ServeDir = serveDir;

    public void Stop()
    {
        _cts?.Cancel();
        try { _tcp?.Stop(); } catch { }
        _tcp = null;
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            {
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 10000;
                var stream = client.GetStream();
                var req = ReadRequest(stream);
                if (req == null) return;
                HandleRequest(stream, req);
            }
        }
        catch { }
    }

    private sealed class HttpRequest
    {
        public string Method = "";
        public string RawPath = "";
        public string UrlPath = "";
        public string Query = "";
        public Dictionary<string, string> Headers = new(StringComparer.OrdinalIgnoreCase);
    }

    private static HttpRequest? ReadRequest(NetworkStream stream)
    {
        // Read until \r\n\r\n
        var buf = new List<byte>(4096);
        var tmp = new byte[1];
        int idle = 0;
        while (buf.Count < 16384)
        {
            int n = stream.Read(tmp, 0, 1);
            if (n == 0) { idle++; if (idle > 3) break; continue; }
            idle = 0;
            buf.Add(tmp[0]);
            if (buf.Count >= 4)
            {
                int e = buf.Count - 1;
                if (buf[e] == '\n' && buf[e - 1] == '\r' && buf[e - 2] == '\n' && buf[e - 3] == '\r')
                    break;
            }
        }

        string rawHeaders = Encoding.ASCII.GetString(buf.ToArray());
        string[] lines = rawHeaders.Split("\r\n", StringSplitOptions.None);
        if (lines.Length == 0) return null;

        string[] parts = lines[0].Split(' ');
        if (parts.Length < 2) return null;

        string rawPath = parts.Length >= 2 ? parts[1] : "/";
        int qi = rawPath.IndexOf('?');
        string urlPath = qi >= 0 ? rawPath[..qi] : rawPath;
        string query = qi >= 0 ? rawPath[(qi + 1)..] : "";

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            int colon = lines[i].IndexOf(':');
            if (colon < 0) continue;
            headers[lines[i][..colon].Trim()] = lines[i][(colon + 1)..].Trim();
        }

        return new HttpRequest
        {
            Method = parts[0],
            RawPath = rawPath,
            UrlPath = Uri.UnescapeDataString(urlPath),
            Query = query,
            Headers = headers
        };
    }

    private void HandleRequest(NetworkStream stream, HttpRequest req)
    {
        if (!CheckAuth(stream, req)) return;

        bool isPlay = req.Query.Contains("play");
        string fsPath = Path.GetFullPath(Path.Combine(ServeDir, req.UrlPath.TrimStart('/')));

        if (!fsPath.StartsWith(ServeDir, StringComparison.OrdinalIgnoreCase))
        {
            SendText(stream, 403, "Forbidden");
            return;
        }

        if (isPlay && File.Exists(fsPath) && HtmlRenderer.IsVideoFile(fsPath))
        {
            byte[] html = HtmlRenderer.RenderPlayer(req.UrlPath);
            SendHtml(stream, html);
            return;
        }

        if (Directory.Exists(fsPath))
        {
            if (!req.UrlPath.EndsWith("/"))
            {
                SendRedirect(stream, req.UrlPath + "/");
                return;
            }
            ServeDirectory(stream, fsPath, req.UrlPath);
            return;
        }

        if (File.Exists(fsPath))
        {
            ServeFile(stream, fsPath, req.Headers.GetValueOrDefault("Range"));
            return;
        }

        SendText(stream, 404, "Not Found");
    }

    private bool CheckAuth(NetworkStream stream, HttpRequest req)
    {
        string cookie = req.Headers.GetValueOrDefault("Cookie", "");
        if (Auth.CheckSession(cookie)) return true;

        string? authVal = ParseQueryParam(req.Query, "auth");
        if (authVal == Auth.Token)
        {
            var (_, setCookie) = Auth.CreateSession();
            string dest = string.IsNullOrEmpty(req.UrlPath) ? "/" : req.UrlPath;
            string response = $"HTTP/1.1 302 Found\r\nLocation: {dest}\r\nSet-Cookie: {setCookie}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(response);
            stream.Write(bytes);
            return false;
        }

        SendText(stream, 403, "QR 코드를 스캔해 주세요.");
        return false;
    }

    private static string? ParseQueryParam(string query, string key)
    {
        foreach (var part in query.Split('&'))
        {
            int eq = part.IndexOf('=');
            if (eq < 0) continue;
            if (part[..eq] == key) return Uri.UnescapeDataString(part[(eq + 1)..]);
        }
        return null;
    }

    private void ServeDirectory(NetworkStream stream, string fsPath, string urlPath)
    {
        var entries = Directory.GetFileSystemEntries(fsPath);
        var dirs = new List<string>();
        var vids = new List<(string, long)>();
        foreach (var entry in entries)
        {
            string name = Path.GetFileName(entry);
            if (Directory.Exists(entry)) dirs.Add(name);
            else if (HtmlRenderer.IsVideoFile(entry)) vids.Add((name, new FileInfo(entry).Length));
        }
        dirs.Sort(StringComparer.CurrentCulture);
        vids.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.CurrentCulture));

        byte[] html = HtmlRenderer.RenderListing(urlPath, dirs.ToArray(), vids.ToArray());
        SendHtml(stream, html);
    }

    private static void ServeFile(NetworkStream stream, string fsPath, string? rangeHeader)
    {
        string mime = HtmlRenderer.GetMime(fsPath);
        using var fs = File.OpenRead(fsPath);
        long size = fs.Length;

        if (rangeHeader != null && rangeHeader.StartsWith("bytes="))
        {
            string[] parts = rangeHeader[6..].Split('-');
            if (!long.TryParse(parts[0], out long start)) start = 0;
            long end = parts.Length > 1 && long.TryParse(parts[1], out long e) ? e : size - 1;

            if (start >= size) { SendText(stream, 416, "Range Not Satisfiable"); return; }
            end = Math.Min(end, size - 1);
            long length = end - start + 1;

            string header = $"HTTP/1.1 206 Partial Content\r\n" +
                $"Content-Type: {mime}\r\nAccept-Ranges: bytes\r\n" +
                $"Content-Range: bytes {start}-{end}/{size}\r\n" +
                $"Content-Length: {length}\r\nConnection: close\r\n\r\n";
            stream.Write(Encoding.ASCII.GetBytes(header));
            fs.Seek(start, SeekOrigin.Begin);
            CopyBytes(fs, stream, length);
        }
        else
        {
            string header = $"HTTP/1.1 200 OK\r\nContent-Type: {mime}\r\n" +
                $"Accept-Ranges: bytes\r\nContent-Length: {size}\r\nConnection: close\r\n\r\n";
            stream.Write(Encoding.ASCII.GetBytes(header));
            CopyBytes(fs, stream, size);
        }
    }

    private static void CopyBytes(FileStream src, NetworkStream dst, long length)
    {
        byte[] buf = new byte[65536];
        long remaining = length;
        try
        {
            while (remaining > 0)
            {
                int read = src.Read(buf, 0, (int)Math.Min(buf.Length, remaining));
                if (read == 0) break;
                dst.Write(buf, 0, read);
                remaining -= read;
            }
        }
        catch (IOException) { }
    }

    private static void SendHtml(NetworkStream stream, byte[] body)
    {
        string header = $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n" +
            $"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n";
        stream.Write(Encoding.ASCII.GetBytes(header));
        stream.Write(body);
    }

    private static void SendText(NetworkStream stream, int status, string msg)
    {
        byte[] body = Encoding.UTF8.GetBytes(msg);
        string header = $"HTTP/1.1 {status} {GetStatusText(status)}\r\n" +
            $"Content-Type: text/plain; charset=utf-8\r\n" +
            $"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n";
        stream.Write(Encoding.ASCII.GetBytes(header));
        stream.Write(body);
    }

    private static void SendRedirect(NetworkStream stream, string location)
    {
        string response = $"HTTP/1.1 302 Found\r\nLocation: {location}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
        stream.Write(Encoding.ASCII.GetBytes(response));
    }

    private static string GetStatusText(int code) => code switch
    {
        200 => "OK", 206 => "Partial Content", 302 => "Found",
        403 => "Forbidden", 404 => "Not Found", 416 => "Range Not Satisfiable",
        _ => "Unknown"
    };

    public static string GetLanIp()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            s.Connect("8.8.8.8", 80);
            return ((IPEndPoint)s.LocalEndPoint!).Address.ToString();
        }
        catch { return "127.0.0.1"; }
    }
}
