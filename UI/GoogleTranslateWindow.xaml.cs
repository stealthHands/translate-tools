using System.Windows;

namespace ScreenTranslate.UI;

public partial class GoogleTranslateWindow : Window
{
    private string? _pendingText;
    private bool _browserReady;

    public GoogleTranslateWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitializeBrowserAsync();
    }

    public void ShowSource(string source)
    {
        _pendingText = source;
        if (!IsVisible) Show();
        Activate();
        if (_browserReady) NavigateToText(source);
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            await Browser.EnsureCoreWebView2Async();
            _browserReady = true;
            Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            if (_pendingText is null) Browser.CoreWebView2.Navigate("https://translate.google.com/?sl=en&tl=zh-CN&op=translate");
            else NavigateToText(_pendingText);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"无法打开 Google 翻译网页。请确认此电脑可以访问 translate.google.com。\n\n{ex.Message}", "备用翻译不可用", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void NavigateToText(string source)
    {
        var url = "https://translate.google.com/?sl=en&tl=zh-CN&text=" + Uri.EscapeDataString(source) + "&op=translate";
        Browser.CoreWebView2.Navigate(url);
    }
}
