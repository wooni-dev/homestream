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
    private static readonly Color AccentDark = ColorTranslator.FromHtml("#2a2540");
    private static readonly Color QrModule = ColorTranslator.FromHtml("#ececf1");

    private static readonly bool IsKo =
        CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ko";

    private static string S(string ko, string en) => IsKo ? ko : en;

    private readonly StreamServer _server;
    private readonly bool[,] _qrMatrix;
    private Label _dirLabel = null!;

    public MainForm(StreamServer server, string ip)
    {
        _server = server;
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
            Padding = new Padding(36, 32, 36, 32),
        };

        const int contentWidth = 400;

        layout.Controls.Add(MakeLabel(S("QR 스캔 또는 주소로 접속", "Scan QR or enter address"),
            TextMuted, new Font("Segoe UI", 10f), new Padding(0, 0, 0, 14), contentWidth));

        // QR canvas
        int n = _qrMatrix.GetLength(0);
        int cell = Math.Max(1, contentWidth / n);
        int qrSize = cell * n;
        int qrMargin = Math.Max(0, (contentWidth - qrSize) / 2);
        var qrPanel = new Panel
        {
            Width = qrSize,
            Height = qrSize,
            BackColor = BgColor,
            Margin = new Padding(qrMargin, 0, 0, 10),
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

        var dirFont = new Font("Segoe UI", 10f);
        string dirText = BreakPath(_server.ServeDir, dirFont, contentWidth);
        _dirLabel = new Label
        {
            Text = dirText,
            ForeColor = TextMuted,
            Font = dirFont,
            BackColor = Color.Transparent,
            AutoSize = false,
            Width = contentWidth,
            Height = CalcLabelHeight(dirText, dirFont),
            TextAlign = ContentAlignment.TopCenter,
            Margin = new Padding(0, 22, 0, 14),
        };
        layout.Controls.Add(_dirLabel);

        var changeBtn = MakeButton(S("폴더 변경", "Change Folder"), AccentDark, TextMuted, new Padding(0, 0, 0, 10));
        changeBtn.Click += (_, _) =>
        {
            try
            {
                using var dlg = new FolderBrowserDialog { InitialDirectory = _server.ServeDir };
                TopMost = true;
                var result = dlg.ShowDialog(this);
                TopMost = false;
                if (result == DialogResult.OK && dlg.SelectedPath != _server.ServeDir)
                {
                    _server.Stop();
                    _server.Start(dlg.SelectedPath);
                    string newText = BreakPath(_server.ServeDir, _dirLabel.Font, _dirLabel.Width);
                    _dirLabel.Text = newText;
                    _dirLabel.Height = CalcLabelHeight(newText, _dirLabel.Font);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "오류");
            }
        };
        layout.Controls.Add(changeBtn);

        layout.Controls.Add(MakeLabel(
            S("이 창을 X(닫기)로 닫으면 서버가 꺼집니다", "Closing this window will stop the server"),
            TextDim, new Font("Segoe UI", 9f), new Padding(0, 4, 0, 0), contentWidth, height: 40));

        Controls.Add(layout);
        ClientSize = new Size(layout.PreferredSize.Width, layout.PreferredSize.Height);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _server.Stop();
        base.OnFormClosing(e);
    }

    private static Label MakeLabel(string text, Color fg, Font font, Padding margin, int width = 400, int height = 24)
    {
        return new Label
        {
            Text = text,
            ForeColor = fg,
            Font = font,
            BackColor = Color.Transparent,
            AutoSize = false,
            Width = width,
            Height = height,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = margin,
        };
    }

    private static int CalcLabelHeight(string text, Font font)
    {
        int lines = text.Count(c => c == '\n') + 1;
        int lineHeight = TextRenderer.MeasureText("Ag가", font).Height;
        return lines * lineHeight + 10;
    }

    private static string BreakPath(string path, Font font, int maxWidth)
    {
        int safeWidth = maxWidth - 10;
        var sb = new System.Text.StringBuilder();
        string remaining = path;
        while (remaining.Length > 0)
        {
            int lo = 1, hi = remaining.Length, fit = 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                int w = TextRenderer.MeasureText(remaining[..mid], font).Width;
                if (w <= safeWidth) { fit = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            sb.Append(remaining[..fit]);
            remaining = remaining[fit..];
            if (remaining.Length > 0) sb.Append('\n');
        }
        return sb.ToString();
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
            Width = 400,
            Height = 60,
            FlatAppearance = { BorderSize = 0 },
        };
    }

}
