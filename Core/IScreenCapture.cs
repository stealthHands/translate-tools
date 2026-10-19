namespace ScreenTranslate.Core;

public interface IScreenCapture
{
    byte[] CapturePng(ScreenRegion region);
}
