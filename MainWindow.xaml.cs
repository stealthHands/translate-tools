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
    private RegionHighlightWindow? _selectionHighlight;
    private ScreenRegion? _region;
    private bool _paused;

    public MainWindow()
    {
        InitializeComponent();
        _session = new TranslationSession(new GdiScreenCapture(), new TesseractOcrService());
        _session.SourceTextReady += (_, text) => Dispatcher.Invoke(() =>
        {
            _overlay.ShowTranslation("字幕已发送到 Google 翻译，请查看翻译窗口。\n\n" + text);
            EnsureGoogleWindow().ShowSource(text);
        });
        _session.StatusChanged += (_, text) => Dispatcher.Invoke(() => SetStatus(text));
        _session.Failed += (_, message) => Dispatcher.Invoke(() => MessageBox.Show(this, message, "屏幕字幕翻译", MessageBoxButton.OK, MessageBoxImage.Warning));
    }

    private async void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        await _session.StopAsync();
        _selectionHighlight?.Hide();
        var selectedRegion = new AreaSelectionWindow().SelectRegion();
        if (selectedRegion is null)
        {
            _selectionHighlight?.Show();
            SetStatus("已取消区域选择");
            return;
        }
        _region = selectedRegion;
        _selectionHighlight?.Close();
        _selectionHighlight = new RegionHighlightWindow(selectedRegion.Value);
        _selectionHighlight.Show();
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
        _selectionHighlight?.Close();
        _overlay.Close();
        base.OnClosed(e);
    }
}
