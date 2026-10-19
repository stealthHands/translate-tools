using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenTranslate.UI;

public partial class TranslationOverlay : Window
{
    public TranslationOverlay() => InitializeComponent();
    public void ShowTranslation(string text) { TranslationText.Text = text; }
    public void SetFontSize(double size) => TranslationText.FontSize = size;
    public void SetSurfaceOpacity(double opacity) => Surface.Background = new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(opacity * 255, 0, 255), 16, 23, 34));
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
}
