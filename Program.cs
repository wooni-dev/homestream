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
        if (string.IsNullOrEmpty(serveDir) || !Directory.Exists(serveDir))
        {
            using var owner = new Form { ShowInTaskbar = false, TopMost = true };
            _ = owner.Handle;
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog(owner) != DialogResult.OK) return;
            serveDir = dlg.SelectedPath;
        }

        var server = new StreamServer();
        server.Start(serveDir);
        string ip = StreamServer.GetLanIp();

        Application.Run(new MainForm(server, ip));
    }
}
