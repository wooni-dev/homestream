using System.Globalization;
using System.Windows.Forms;
using HomeStream.Gui;
using HomeStream.Server;

Application.SetHighDpiMode(HighDpiMode.SystemAware);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

string? serveDir = Environment.GetEnvironmentVariable("SERVE_DIR");
if (string.IsNullOrEmpty(serveDir) || !Directory.Exists(serveDir))
{
    using var dlg = new FolderBrowserDialog
    {
        Description = CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ko"
            ? "스트리밍할 영상 폴더를 선택하세요"
            : "Select video folder to stream"
    };
    if (dlg.ShowDialog() != DialogResult.OK) return;
    serveDir = dlg.SelectedPath;
}

var server = new StreamServer();
server.Start(serveDir);
string ip = StreamServer.GetLanIp();

Application.Run(new MainForm(server, ip));
