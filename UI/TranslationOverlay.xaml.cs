using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenTranslate.UI;

public partial class TranslationOverlay : Window
{
    private double _expandedHeight = 150;
    private bool _isCollapsed;

    public TranslationOverlay() => InitializeComponent();
    public void ShowTranslation(string text) { TranslationText.Text = text; }
    public void SetFontSize(double size) => TranslationText.FontSize = size;
    public void SetSurfaceOpacity(double opacity) => Surface.Background = new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(opacity * 255, 0, 255), 16, 23, 34));
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }

    private void CollapseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isCollapsed)
        {
            TranslationContent.Visibility = Visibility.Visible;
            MinHeight = 90;
            MaxHeight = double.PositiveInfinity;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Height = Math.Max(_expandedHeight, MinHeight);
            CollapseButton.Content = "−";
            CollapseButton.ToolTip = "最小化";
            _isCollapsed = false;
            return;
        }

        _expandedHeight = Height;
        TranslationContent.Visibility = Visibility.Collapsed;
        MinHeight = 48;
        MaxHeight = 48;
        ResizeMode = ResizeMode.NoResize;
        Height = 48;
        CollapseButton.Content = "□";
        CollapseButton.ToolTip = "还原";
        _isCollapsed = true;
    }
}
