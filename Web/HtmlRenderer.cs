using System.Net;
using System.Text;

namespace HomeStream.Web;

internal static class HtmlRenderer
{
    private static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v" };

    public static byte[] RenderListing(string dispPath, string[] dirs, (string name, long size)[] vids)
    {
        var parts = dispPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string cum = "";
        var crumb = new StringBuilder("<a href=\"/\">HOME</a>");
        foreach (var p in parts)
        {
            cum += "/" + Uri.EscapeDataString(p);
            crumb.Append($" <span style=\"color:#444\">/</span> <a href=\"{cum}/\">{WebUtility.HtmlEncode(p)}</a>");
        }

        var rows = new StringBuilder();
        foreach (var name in dirs)
        {
            string link = Uri.EscapeDataString(name) + "/";
            rows.Append($"<a class=\"item\" href=\"{link}\">" +
                $"<div class=\"ic folder\">[D]</div>" +
                $"<div class=\"meta\"><div class=\"name\">{WebUtility.HtmlEncode(name)}</div></div>" +
                $"<div class=\"chev\">&rsaquo;</div></a>");
        }
        foreach (var (name, size) in vids)
        {
            string link = Uri.EscapeDataString(name) + "?play";
            rows.Append($"<a class=\"item\" href=\"{link}\">" +
                $"<div class=\"ic video\">&#9654;</div>" +
                $"<div class=\"meta\"><div class=\"name\">{WebUtility.HtmlEncode(name)}</div>" +
                $"<div class=\"sz\">{HumanSize(size)}</div></div>" +
                $"<div class=\"chev\">&rsaquo;</div></a>");
        }

        string body = rows.Length > 0 ? rows.ToString() : "<div class=\"empty\">이 폴더에 영상이 없습니다</div>";
        string title = parts.Length > 0 ? parts[^1] : "홈";
        string count = $"폴더 {dirs.Length} · 영상 {vids.Length}";

        string html = $"""
            <!DOCTYPE html><html lang="ko"><head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
            <title>{WebUtility.HtmlEncode(title)}</title><style>{PageCss.Value}</style></head><body>
            <header><h1>{WebUtility.HtmlEncode(title)}</h1><div class="sub">{count}</div>
            <div class="crumb">{crumb}</div></header>
            <div class="list">{body}</div></body></html>
            """;
        return Encoding.UTF8.GetBytes(html);
    }

    public static byte[] RenderPlayer(string urlPath)
    {
        string name = Uri.UnescapeDataString(urlPath.TrimEnd('/').Split('/')[^1]);
        string html = $"""
            <!DOCTYPE html><html lang="ko"><head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
            <title>{WebUtility.HtmlEncode(name)}</title><style>{PageCss.Value}</style></head><body class="pbody">
            <div class="ptop"><a class="back" href="javascript:history.back()">&lsaquo; 뒤로</a>
            <div class="ptitle">{WebUtility.HtmlEncode(name)}</div></div>
            <div class="pwrap">
            <video controls autoplay playsinline preload="metadata" src="{urlPath}"></video>
            </div></body></html>
            """;
        return Encoding.UTF8.GetBytes(html);
    }

    public static bool IsVideoFile(string path) =>
        VideoExts.Contains(Path.GetExtension(path));

    public static string GetMime(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".mp4" => "video/mp4",
        ".mkv" => "video/x-matroska",
        ".avi" => "video/x-msvideo",
        ".mov" => "video/quicktime",
        ".wmv" => "video/x-ms-wmv",
        ".m4v" => "video/x-m4v",
        _ => "application/octet-stream"
    };

    internal static string HumanSize(long n) =>
        n >= 1024L * 1024 * 1024
            ? $"{n / (1024.0 * 1024 * 1024):F1} GB"
            : $"{n / (1024.0 * 1024):F0} MB";
}
