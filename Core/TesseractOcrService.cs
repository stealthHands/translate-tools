using Tesseract;
using System.IO;

namespace ScreenTranslate.Core;

public sealed class TesseractOcrService : IDisposable
{
    private TesseractEngine? _engine;

    public TesseractOcrService()
    {
    }

    public string ReadText(byte[] image)
    {
        _engine ??= CreateEngine();
        using var pix = Pix.LoadFromMemory(image);
        using var page = _engine.Process(pix);
        return Normalize(page.GetText());
    }

    private static string Normalize(string text) => string.Join('\n', text.Replace("\r", "").Split('\n').Select(x => string.Join(' ', x.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))).Where(x => x.Length > 0));
    private static TesseractEngine CreateEngine()
    {
        var modelRoot = Path.Combine(AppContext.BaseDirectory, "Models");
        var dataPath = Path.Combine(modelRoot, "tessdata");
        if (!File.Exists(Path.Combine(dataPath, "eng.traineddata")))
            throw new FileNotFoundException("缺少内置英文 OCR 模型 Models/tessdata/eng.traineddata。请重新安装完整模型包。");
        var engine = new TesseractEngine(modelRoot, "eng", EngineMode.LstmOnly);
        engine.DefaultPageSegMode = PageSegMode.Auto;
        return engine;
    }
    public void Dispose() => _engine?.Dispose();
}
