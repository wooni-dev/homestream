using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using HomeStream.Core;

namespace HomeStream.Mac;

internal static class MacTheme
{
    public static readonly IBrush BgBrush = new SolidColorBrush(Color.Parse(Theme.BgColorHex));
    public static readonly IBrush TextMutedBrush = new SolidColorBrush(Color.Parse(Theme.TextMutedHex));
    public static readonly IBrush TextDimBrush = new SolidColorBrush(Color.Parse(Theme.TextDimHex));
    public static readonly IBrush AccentDarkBrush = new SolidColorBrush(Color.Parse(Theme.AccentDarkHex));

    public static Button MakeButton(string text, int width, int height = 60)
    {
        return new Button
        {
            Content = text,
            Background = AccentDarkBrush,
            Foreground = TextMutedBrush,
            BorderThickness = new Avalonia.Thickness(0),
            FontWeight = FontWeight.Bold,
            FontSize = 14,
            Width = width,
            Height = height,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
    }

    public static async Task<string?> PickFolderAsync(Window owner)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Theme.S("폴더 선택", "Select Folder"),
            AllowMultiple = false,
        });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
