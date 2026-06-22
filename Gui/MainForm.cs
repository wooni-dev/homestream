using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using HomeStream.Qr;
using HomeStream.Server;

namespace HomeStream.Gui;

internal sealed class MainForm : Form
{
    private static readonly Color BgColor = ColorTranslator.FromHtml("#0e0e12");
    private static readonly Color TextMuted = ColorTranslator.FromHtml("#9a9aae");
    private static readonly Color TextDim = ColorTranslator.FromHtml("#55556a");
    private static readonly Color AccentColor = ColorTranslator.FromHtml("#8a8aff");
    private static readonly Color AccentDark = ColorTranslator.FromHtml("#2a2540");
    private static readonly Color AccentLight = ColorTranslator.FromHtml("#a3a3ff");
    private static readonly Color SuccessColor = ColorTranslator.FromHtml("#3aa76d");
    private static readonly Color QrModule = ColorTranslator.FromHtml("#ececf1");

    private static readonly bool IsKo =
        CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ko";

    private static string S(string ko, string en) => IsKo ? ko : en;

    private readonly StreamServer _server;
    private readonly string _ip;
    private readonly bool[,] _qrMatrix;
    private Label _dirLabel = null!;
    private Button _copyBtn = null!;

    public MainForm(StreamServer server, string ip)
    {
        _server = server;
        _ip = ip;
        string authUrl = $"http://{ip}:{server.ActualPort}/?auth={server.Auth.Token}";
        _qrMatrix = QrCode.Encode(authUrl);
        BuildUI(ip, server.ActualPort);
    }

    private void BuildUI(string ip, int port)
    {
        Text = S("홈 스트리밍", "Home Streaming");
        BackColor = BgColor;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BgColor,
            Padding = new Padding(30, 22, 30, 22),
        };

        layout.Controls.Add(MakeLabel(S("QR 스캔 또는 주소로 접속", "Scan QR or enter address"),
            TextMuted, new Font("Segoe UI", 10f), new Padding(0, 0, 0, 6)));

        // QR canvas
        int n = _qrMatrix.GetLength(0);
        int cell = Math.Max(1, 180 / n);
        int qrSize = cell * n;
        var qrPanel = new Panel
        {
            Width = qrSize,
            Height = qrSize,
            BackColor = BgColor,
            Margin = new Padding(0, 0, 0, 10),
        };
        qrPanel.Paint += (_, e) =>
        {
            using var brush = new SolidBrush(QrModule);
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (_qrMatrix[r, c])
                        e.Graphics.FillRectangle(brush, c * cell, r * cell, cell, cell);
        };
        layout.Controls.Add(qrPanel);

        _copyBtn = MakeButton(S("주소 복사", "Copy URL"), AccentColor, BgColor, new Padding(0, 10, 0, 4));
        _copyBtn.Click += async (_, _) =>
        {
            Clipboard.SetText($"http://{ip}:{port}/");
            _copyBtn.Text = S("복사됨!", "Copied!");
            _copyBtn.BackColor = SuccessColor;
            await Task.Delay(1500);
            if (!IsDisposed)
            {
                _copyBtn.Text = S("주소 복사", "Copy URL");
                _copyBtn.BackColor = AccentColor;
            }
        };
        layout.Controls.Add(_copyBtn);

        layout.Controls.Add(MakeLabel(S("서비스 중인 폴더", "Serving folder"),
            TextDim, new Font("Segoe UI", 8f), new Padding(0, 14, 0, 2)));

        _dirLabel = MakeLabel(ShortPath(_server.ServeDir), TextMuted,
            new Font("Segoe UI", 9f), new Padding(0, 0, 0, 6));
        layout.Controls.Add(_dirLabel);

        var changeBtn = MakeButton(S("폴더 변경", "Change Folder"), AccentDark, AccentColor, new Padding(0, 0, 0, 4));
        changeBtn.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog
            {
                InitialDirectory = _server.ServeDir,
                Description = S("스트리밍할 영상 폴더를 선택하세요", "Select video folder to stream")
            };
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedPath != _server.ServeDir)
            {
                _server.Stop();
                _server.Start(dlg.SelectedPath);
                _dirLabel.Text = ShortPath(_server.ServeDir);
            }
        };
        layout.Controls.Add(changeBtn);

        layout.Controls.Add(MakeLabel(
            S("이 창을 X(닫기)로 닫으면 서버가 꺼집니다", "Closing this window will stop the server"),
            TextDim, new Font("Segoe UI", 8f), new Padding(0, 8, 0, 0)));

        Controls.Add(layout);
        ClientSize = new Size(layout.PreferredSize.Width, layout.PreferredSize.Height);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _server.Stop();
        base.OnFormClosing(e);
    }

    private static Label MakeLabel(string text, Color fg, Font font, Padding margin)
    {
        return new Label
        {
            Text = text,
            ForeColor = fg,
            Font = font,
            BackColor = Color.Transparent,
            AutoSize = true,
            Margin = margin,
        };
    }

    private static Button MakeButton(string text, Color bg, Color fg, Padding margin)
    {
        return new Button
        {
            Text = text,
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = margin,
            Width = 220,
            Height = 36,
            FlatAppearance = { BorderSize = 0 },
        };
    }

    private static string ShortPath(string path, int maxLen = 42)
    {
        if (path.Length <= maxLen) return path;
        string[] parts = path.Replace('\\', '/').Split('/');
        return parts.Length > 2 ? ".../" + string.Join("/", parts[^2..]) : path;
    }
}
