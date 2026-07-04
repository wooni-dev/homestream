using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace HomeStream.Gui;

internal sealed class FolderSelectForm : Form
{
    private static readonly Color BgColor = ColorTranslator.FromHtml("#0e0e12");
    private static readonly Color TextMuted = ColorTranslator.FromHtml("#9a9aae");
    private static readonly Color AccentDark = ColorTranslator.FromHtml("#2a2540");

    private static readonly bool IsKo =
        CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ko";

    private static string S(string ko, string en) => IsKo ? ko : en;

    public string SelectedPath { get; private set; } = "";

    public FolderSelectForm()
    {
        Text = S("홈 스트리밍", "Home Streaming");
        BackColor = BgColor;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;

        const int contentWidth = 440;

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BgColor,
            Padding = new Padding(36, 40, 36, 40),
        };

        var label = new Label
        {
            Text = S("스트리밍할 영상 폴더를 선택하세요", "Choose a folder to stream"),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 11f),
            BackColor = Color.Transparent,
            AutoSize = false,
            Width = contentWidth,
            Height = 48,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 20),
        };
        layout.Controls.Add(label);

        var selectBtn = new Button
        {
            Text = S("폴더 선택", "Select Folder"),
            BackColor = AccentDark,
            ForeColor = TextMuted,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Width = contentWidth,
            Height = 60,
            FlatAppearance = { BorderSize = 0 },
        };
        selectBtn.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog();
            TopMost = true;
            var result = dlg.ShowDialog(this);
            TopMost = false;
            if (result != DialogResult.OK) return;

            SelectedPath = dlg.SelectedPath;
            DialogResult = DialogResult.OK;
            Close();
        };
        layout.Controls.Add(selectBtn);

        Controls.Add(layout);
        ClientSize = new Size(layout.PreferredSize.Width, layout.PreferredSize.Height);
    }
}
