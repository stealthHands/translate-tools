using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenTranslate.Core;

namespace ScreenTranslate.UI;

public sealed class RegionHighlightWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    public RegionHighlightWindow(ScreenRegion region)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        IsHitTestVisible = false;
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        var canvas = new System.Windows.Controls.Canvas { Background = Brushes.Transparent };
        Content = canvas;

        Loaded += (_, _) => DrawFrame(canvas, region);
        SourceInitialized += (_, _) => MakeClickThrough();
    }

    private void DrawFrame(System.Windows.Controls.Canvas canvas, ScreenRegion region)
    {
        // PointFromScreen needs a presentation source, which exists only after the
        // window has loaded. The selected region is stored in screen coordinates.
        var first = PointFromScreen(new Point(region.X, region.Y));
        var last = PointFromScreen(new Point(region.X + region.Width, region.Y + region.Height));
        var frame = new Rectangle
        {
            Width = Math.Abs(last.X - first.X),
            Height = Math.Abs(last.Y - first.Y),
            Stroke = Brushes.DeepSkyBlue,
            StrokeThickness = 2,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };
        System.Windows.Controls.Canvas.SetLeft(frame, Math.Min(first.X, last.X));
        System.Windows.Controls.Canvas.SetTop(frame, Math.Min(first.Y, last.Y));
        canvas.Children.Add(frame);
    }

    private void MakeClickThrough()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | WsExTransparent | WsExToolWindow | WsExNoActivate));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr handle, int index, IntPtr value);
}
