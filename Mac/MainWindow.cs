using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using HomeStream.Qr;
using HomeStream.Server;
using static HomeStream.Mac.MacTheme;
using CoreTheme = HomeStream.Core.Theme;

namespace HomeStream.Mac;

internal sealed class MainWindow : Window
{
    private const int ContentWidth = 400;

    private readonly StreamServer _server;
    private readonly bool[,] _qrMatrix;
    private TextBlock _dirLabel = null!;

    public MainWindow(StreamServer server, string ip)
    {
        _server = server;
        string authUrl = $"http://{ip}:{server.ActualPort}/?auth={server.Auth.Token}";
        _qrMatrix = QrCode.Encode(authUrl);
        BuildUI();
    }

    private void BuildUI()
    {
        Title = CoreTheme.S("홈 스트리밍", "Home Streaming");
        Background = BgBrush;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        SizeToContent = SizeToContent.WidthAndHeight;

        var layout = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(36, 32, 36, 32),
        };

        layout.Children.Add(new TextBlock
        {
            Text = CoreTheme.S("QR 스캔 후 뜨는 주소를 터치하세요", "Scan QR, then tap the address that appears"),
            Foreground = TextMutedBrush,
            FontSize = 14,
            Width = ContentWidth,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14),
        });

        layout.Children.Add(new QrCanvas(_qrMatrix)
        {
            Width = ContentWidth,
            Height = ContentWidth,
            Margin = new Thickness(0, 0, 0, 10),
        });

        string initialDirText = BreakPath(_server.ServeDir, ContentWidth);
        _dirLabel = new TextBlock
        {
            Text = initialDirText,
            Foreground = TextMutedBrush,
            FontSize = 14,
            Width = ContentWidth,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 22, 0, 14),
        };
        layout.Children.Add(_dirLabel);

        var changeBtn = MakeButton(CoreTheme.S("폴더 변경", "Change Folder"), ContentWidth);
        changeBtn.Margin = new Thickness(0, 0, 0, 10);
        changeBtn.Click += async (_, _) =>
        {
            string? path = await PickFolderAsync(this);
            if (path == null || path == _server.ServeDir) return;

            _server.SetServeDir(path);
            _dirLabel.Text = BreakPath(_server.ServeDir, ContentWidth);
        };
        layout.Children.Add(changeBtn);

        layout.Children.Add(new TextBlock
        {
            Text = CoreTheme.S("이 창을 닫으면 서버가 꺼집니다", "Closing this window will stop the server"),
            Foreground = TextDimBrush,
            FontSize = 12,
            Width = ContentWidth,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0),
        });

        Content = layout;
    }

    private static string BreakPath(string path, int maxWidth)
    {
        var typeface = new Typeface("Segoe UI");
        const int fontSize = 14;
        int safeWidth = maxWidth - 10;
        var sb = new StringBuilder();
        string remaining = path;
        while (remaining.Length > 0)
        {
            int lo = 1, hi = remaining.Length, fit = 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var ft = new FormattedText(remaining[..mid], CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, typeface, fontSize, TextMutedBrush);
                if (ft.Width <= safeWidth) { fit = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            sb.Append(remaining[..fit]);
            remaining = remaining[fit..];
            if (remaining.Length > 0) sb.Append('\n');
        }
        return sb.ToString();
    }
}

internal sealed class QrCanvas : Control
{
    private readonly bool[,] _matrix;

    public QrCanvas(bool[,] matrix) => _matrix = matrix;

    public override void Render(DrawingContext context)
    {
        int n = _matrix.GetLength(0);
        double size = Math.Min(Bounds.Width, Bounds.Height);
        int cell = Math.Max(1, (int)(size / n));
        int qrSize = cell * n;
        double ox = (Bounds.Width - qrSize) / 2;
        double oy = (Bounds.Height - qrSize) / 2;

        context.FillRectangle(Brushes.White, new Rect(0, 0, Bounds.Width, Bounds.Height));
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                if (_matrix[r, c])
                    context.FillRectangle(Brushes.Black, new Rect(ox + c * cell, oy + r * cell, cell, cell));
    }
}
