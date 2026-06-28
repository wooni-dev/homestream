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

        var server = new StreamServer();
        server.Start();
        if (!string.IsNullOrEmpty(serveDir))
            server.SetServeDir(serveDir);

        string ip = StreamServer.GetLanIp();
        Application.Run(new MainForm(server, ip));
    }
}
