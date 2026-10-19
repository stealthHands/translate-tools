using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ScreenTranslate.Core;

/// <summary>Captures pixels in virtual-desktop coordinates, including regions on secondary monitors.</summary>
public sealed class GdiScreenCapture : IScreenCapture
{
    public byte[] CapturePng(ScreenRegion region)
    {
        if (region.Width < 1 || region.Height < 1) throw new ArgumentOutOfRangeException(nameof(region));
        using var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
            graphics.CopyFromScreen(region.X, region.Y, 0, 0, new Size(region.Width, region.Height), CopyPixelOperation.SourceCopy);
        using var output = new MemoryStream();
        bitmap.Save(output, ImageFormat.Png);
        return output.ToArray();
    }
}
