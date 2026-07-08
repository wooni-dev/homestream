using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using static HomeStream.Mac.MacTheme;
using CoreTheme = HomeStream.Core.Theme;

namespace HomeStream.Mac;

internal sealed class FolderSelectWindow : Window
{
    public event Action<string>? PathSelected;
    public event Action? Cancelled;

    private bool _selected;

    public FolderSelectWindow()
    {
        Title = CoreTheme.S("홈 스트리밍", "Home Streaming");
        Background = BgBrush;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        SizeToContent = SizeToContent.WidthAndHeight;

        const int contentWidth = 440;

        var layout = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Avalonia.Thickness(36, 40, 36, 40),
        };

        layout.Children.Add(new TextBlock
        {
            Text = CoreTheme.S("스트리밍할 영상 폴더를 선택하세요", "Choose a folder to stream"),
            Foreground = TextMutedBrush,
            FontSize = 15,
            Width = contentWidth,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 0, 0, 20),
        });

        var selectBtn = MakeButton(CoreTheme.S("폴더 선택", "Select Folder"), contentWidth);
        selectBtn.Click += async (_, _) =>
        {
            string? path = await PickFolderAsync(this);
            if (path == null) return;

            _selected = true;
            PathSelected?.Invoke(path);
            Close();
        };
        layout.Children.Add(selectBtn);

        Content = layout;

        Closed += (_, _) =>
        {
            if (!_selected) Cancelled?.Invoke();
        };
    }
}
