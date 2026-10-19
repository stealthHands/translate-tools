using System.Windows;
using System.Windows.Media;
using ScreenTranslate.Core;
using ScreenTranslate.UI;

namespace ScreenTranslate;

public partial class MainWindow : Window
{
    private readonly TranslationSession _session;
    private readonly TranslationOverlay _overlay = new();
    private GoogleTranslateWindow? _googleWindow;
    private ScreenRegion? _region;
    private bool _paused;

    public MainWindow()
    {
        InitializeComponent();
        _session = new TranslationSession(new GdiScreenCapture(), new TesseractOcrService(), new LocalOnnxTranslator(LocalModelBrowser));
        _session.TranslationReady += (_, text) => Dispatcher.Invoke(() => _overlay.ShowTranslation(text));
        _session.ExternalTextReady += (_, text) => Dispatcher.Invoke(() =>
        {
            _overlay.ShowTranslation("请查看 Google 翻译备用窗口中的译文。\n\n" + text);
            EnsureGoogleWindow().ShowSource(text);
        });
        _session.StatusChanged += (_, text) => Dispatcher.Invoke(() => SetStatus(text));
        _session.Failed += (_, message) => Dispatcher.Invoke(() => MessageBox.Show(this, message, "屏幕字幕翻译", MessageBoxButton.OK, MessageBoxImage.Warning));
    }

    private async void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        await _session.StopAsync();
        _region = new AreaSelectionWindow().SelectRegion();
        if (_region is null) { SetStatus("已取消区域选择"); return; }
        StartButton.IsEnabled = true;
        SetStatus($"已选择区域 {_region.Value.Width} × {_region.Value.Height}");
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_region is null) return;
        try
        {
            _overlay.Show();
            _paused = false;
            await _session.StartAsync(_region.Value);
            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            StopButton.IsEnabled = true;
            SelectButton.IsEnabled = false;
        }
        catch (Exception ex) { SetStatus("启动失败"); MessageBox.Show(this, ex.Message, "无法开始翻译", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        _paused = !_paused;
        _session.SetPaused(_paused);
        PauseButton.Content = _paused ? "继续" : "暂停";
        SetStatus(_paused ? "已暂停" : "正在识别并翻译…");
    }

    private void TranslationMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_session is null || TranslationMode is null) return;
        var useGoogle = TranslationMode.SelectedIndex == 1;
        _session.SetExternalTranslation(useGoogle);
        if (useGoogle) EnsureGoogleWindow().Show();
        else _googleWindow?.Hide();
    }

    private GoogleTranslateWindow EnsureGoogleWindow()
    {
        if (_googleWindow is not null) return _googleWindow;
        _googleWindow = new GoogleTranslateWindow();
        _googleWindow.Closed += (_, _) => _googleWindow = null;
        return _googleWindow;
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        await _session.StopAsync();
        StartButton.IsEnabled = _region is not null;
        PauseButton.IsEnabled = false;
        PauseButton.Content = "暂停";
        StopButton.IsEnabled = false;
        SelectButton.IsEnabled = true;
        SetStatus("已停止");
    }

    private void FontSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FontValue is null) return;
        FontValue.Text = ((int)FontSlider.Value).ToString();
        if (_overlay is not null) _overlay.SetFontSize(FontSlider.Value);
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValue is null) return;
        OpacityValue.Text = $"{(int)OpacitySlider.Value}%";
        if (_overlay is not null) _overlay.SetSurfaceOpacity(OpacitySlider.Value / 100.0);
    }

    private void SetStatus(string value)
    {
        StatusText.Text = value;
        StatusDot.Fill = value.Contains("失败") ? Brushes.IndianRed : value.Contains("正在") ? Brushes.MediumSeaGreen : Brushes.SlateGray;
    }

    protected override async void OnClosed(EventArgs e)
    {
        await _session.DisposeAsync();
        _googleWindow?.Close();
        _overlay.Close();
        base.OnClosed(e);
    }
}
