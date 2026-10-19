using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScreenTranslate.Core;
using WpfPoint = System.Windows.Point;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ScreenTranslate.UI;

public partial class AreaSelectionWindow : Window
{
    private WpfPoint _anchor;
    private bool _dragging;
    private ScreenRegion? _selection;

    public AreaSelectionWindow()
    {
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    public ScreenRegion? SelectRegion() { ShowDialog(); return _selection; }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _anchor = e.GetPosition(this); _dragging = true; SelectionBox.Visibility = Visibility.Visible; CaptureMouse(); UpdateBox(e.GetPosition(this));
    }
    private void Window_MouseMove(object sender, WpfMouseEventArgs e) { if (_dragging) UpdateBox(e.GetPosition(this)); }
    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        UpdateBox(e.GetPosition(this)); _dragging = false; ReleaseMouseCapture();
        var p = e.GetPosition(this); var min = new WpfPoint(Math.Min(p.X, _anchor.X), Math.Min(p.Y, _anchor.Y)); var max = new WpfPoint(Math.Max(p.X, _anchor.X), Math.Max(p.Y, _anchor.Y));
        var screenMin = PointToScreen(min); var screenMax = PointToScreen(max);
        var x = (int)Math.Round(screenMin.X); var y = (int)Math.Round(screenMin.Y);
        var w = (int)Math.Round(screenMax.X - screenMin.X); var h = (int)Math.Round(screenMax.Y - screenMin.Y);
        if (w >= 24 && h >= 18 && IntersectsDisplay(x, y, w, h)) _selection = new ScreenRegion(x, y, w, h);
        DialogResult = _selection is not null;
    }
    private static bool IntersectsDisplay(int x, int y, int w, int h)
    {
        var left = GetSystemMetrics(76); var top = GetSystemMetrics(77);
        var right = left + GetSystemMetrics(78); var bottom = top + GetSystemMetrics(79);
        return x < right && x + w > left && y < bottom && y + h > top;
    }
    private void UpdateBox(WpfPoint p)
    {
        var left = Math.Min(p.X, _anchor.X); var top = Math.Min(p.Y, _anchor.Y);
        Canvas.SetLeft(SelectionBox, left); Canvas.SetTop(SelectionBox, top); SelectionBox.Width = Math.Abs(p.X - _anchor.X); SelectionBox.Height = Math.Abs(p.Y - _anchor.Y);
    }
    private void Window_KeyDown(object sender, WpfKeyEventArgs e) { if (e.Key == Key.Escape) { _selection = null; DialogResult = false; } }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
}
