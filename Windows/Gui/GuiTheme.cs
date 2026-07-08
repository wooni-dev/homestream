using System.Drawing;
using System.Windows.Forms;
using HomeStream.Core;

namespace HomeStream.Gui;

internal static class GuiTheme
{
    public static readonly Color BgColor = ColorTranslator.FromHtml(Theme.BgColorHex);
    public static readonly Color TextMuted = ColorTranslator.FromHtml(Theme.TextMutedHex);
    public static readonly Color TextDim = ColorTranslator.FromHtml(Theme.TextDimHex);
    public static readonly Color AccentDark = ColorTranslator.FromHtml(Theme.AccentDarkHex);

    public static string S(string ko, string en) => Theme.S(ko, en);

    public static Button MakeButton(string text, Color bg, Color fg, int width, int height = 60, Padding? margin = null)
    {
        return new Button
        {
            Text = text,
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = margin ?? new Padding(3),
            Width = width,
            Height = height,
            FlatAppearance = { BorderSize = 0 },
        };
    }

    public static string? PickFolder(Form owner, string? initialDirectory = null)
    {
        using var dlg = new FolderBrowserDialog();
        if (!string.IsNullOrEmpty(initialDirectory)) dlg.InitialDirectory = initialDirectory;
        owner.TopMost = true;
        var result = dlg.ShowDialog(owner);
        owner.TopMost = false;
        return result == DialogResult.OK ? dlg.SelectedPath : null;
    }
}
