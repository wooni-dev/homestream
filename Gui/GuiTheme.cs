using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace HomeStream.Gui;

internal static class GuiTheme
{
    public static readonly Color BgColor = ColorTranslator.FromHtml("#0e0e12");
    public static readonly Color TextMuted = ColorTranslator.FromHtml("#9a9aae");
    public static readonly Color TextDim = ColorTranslator.FromHtml("#55556a");
    public static readonly Color AccentDark = ColorTranslator.FromHtml("#2a2540");

    public static readonly bool IsKo =
        CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ko";

    public static string S(string ko, string en) => IsKo ? ko : en;

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
