using System.Windows.Forms;
using HomeStream.Gui;
using HomeStream.Server;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string? serveDir = Environment.GetEnvironmentVariable("SERVE_DIR");
        if (!string.IsNullOrEmpty(serveDir) && !Directory.Exists(serveDir))
            serveDir = null;

        if (string.IsNullOrEmpty(serveDir))
        {
            using var folderForm = new FolderSelectForm();
            if (folderForm.ShowDialog() != DialogResult.OK) return;
            serveDir = folderForm.SelectedPath;
        }

        var server = new StreamServer();
        server.Start(serveDir);

        string ip = StreamServer.GetLanIp();
        string testUrl = $"http://{ip}:{server.ActualPort}/?auth={server.Auth.Token}";
        var matrix = HomeStream.Qr.QrCode.Encode(testUrl);
        int sz = matrix.GetLength(0);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"URL({testUrl.Length}): {testUrl}");
        sb.AppendLine($"Matrix: {sz}x{sz}");
        for (int r = 0; r < sz; r++) { for (int c = 0; c < sz; c++) sb.Append(matrix[r, c] ? "##" : "  "); sb.AppendLine(); }
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "qr_dump.txt"), sb.ToString());
        Application.Run(new MainForm(server, ip));
    }
}
