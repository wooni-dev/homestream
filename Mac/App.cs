using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using HomeStream.Server;

namespace HomeStream.Mac;

internal sealed class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = ThemeVariant.Dark;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            string? serveDir = Environment.GetEnvironmentVariable("SERVE_DIR");
            if (!string.IsNullOrEmpty(serveDir) && !Directory.Exists(serveDir))
                serveDir = null;

            if (string.IsNullOrEmpty(serveDir))
                ShowFolderSelect(desktop);
            else
                StartAndShowMain(desktop, serveDir);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowFolderSelect(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var folderWindow = new FolderSelectWindow();
        folderWindow.PathSelected += path => StartAndShowMain(desktop, path);
        folderWindow.Cancelled += () => desktop.Shutdown();
        desktop.MainWindow = folderWindow;
        folderWindow.Show();
    }

    private void StartAndShowMain(IClassicDesktopStyleApplicationLifetime desktop, string serveDir)
    {
        var server = new StreamServer();
        server.Start(serveDir);
        string ip = StreamServer.GetLanIp();

        var mainWindow = new MainWindow(server, ip);
        mainWindow.Closed += (_, _) =>
        {
            server.Stop();
            desktop.Shutdown();
        };

        Window? old = desktop.MainWindow;
        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        old?.Close();
    }
}
