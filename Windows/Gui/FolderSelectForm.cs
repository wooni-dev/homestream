using System.Drawing;
using System.Windows.Forms;
using static HomeStream.Gui.GuiTheme;

namespace HomeStream.Gui;

internal sealed class FolderSelectForm : Form
{
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

        var selectBtn = MakeButton(S("폴더 선택", "Select Folder"), AccentDark, TextMuted, contentWidth);
        selectBtn.Click += (_, _) =>
        {
            string? path = PickFolder(this);
            if (path == null) return;

            SelectedPath = path;
            DialogResult = DialogResult.OK;
            Close();
        };
        layout.Controls.Add(selectBtn);

        Controls.Add(layout);
        ClientSize = new Size(layout.PreferredSize.Width, layout.PreferredSize.Height);
    }
}
