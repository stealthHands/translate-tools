using Tesseract;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

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
        using var processedImage = PrepareImage(image);
        using var stream = new MemoryStream();
        processedImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        var png = stream.ToArray();
        using var pix = Pix.LoadFromMemory(png);

        // A selected region may contain several lines even when it is very wide.
        // Avoid SingleLine, which can silently discard all but one row.
        var modes = new[] { PageSegMode.Auto, PageSegMode.SingleBlock, PageSegMode.SparseText };

        string bestText = string.Empty;
        double bestScore = double.NegativeInfinity;
        foreach (var mode in modes)
        {
            using var page = _engine.Process(pix, mode);
            var text = Normalize(page.GetText());
            if (text.Length == 0) continue;

            // GetMeanConfidence is normalized to 0..1. Rank completeness first:
            // confidence-only ranking can prefer one crisp row over the full
            // selection whose lines have slightly lower confidence.
            var confidence = page.GetMeanConfidence();
            var lineCount = text.Count(character => character == '\n') + 1;
            var score = lineCount * 200 + Math.Min(text.Length, 199) + confidence;
            if (score > bestScore)
            {
                bestText = text;
                bestScore = score;
            }
        }
        return bestText;
    }

    private static Bitmap PrepareImage(byte[] image)
    {
        using var input = new MemoryStream(image);
        using var source = new Bitmap(input);

        // Enlarge small on-screen text, but cap the output size to avoid excessive work.
        var scale = Math.Min(3.0, Math.Min(2800.0 / source.Width, 2800.0 / source.Height));
        scale = Math.Max(1.0, scale);
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        const int border = 12;
        var result = new Bitmap(width + border * 2, height + border * 2, PixelFormat.Format32bppArgb);
        result.SetResolution(300, 300);

        using (var graphics = Graphics.FromImage(result))
        using (var attributes = new ImageAttributes())
        {
            graphics.Clear(Color.White);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            attributes.SetColorMatrix(new ColorMatrix(new[]
            {
                new[] { 0.299f, 0.299f, 0.299f, 0f, 0f },
                new[] { 0.587f, 0.587f, 0.587f, 0f, 0f },
                new[] { 0.114f, 0.114f, 0.114f, 0f, 0f },
                new[] { 0f, 0f, 0f, 1f, 0f },
                new[] { 0f, 0f, 0f, 0f, 1f }
            }));
            graphics.DrawImage(source, new Rectangle(border, border, width, height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
        }
        return result;
    }

    private static string Normalize(string text) => string.Join('\n', text.Replace("\r", "").Split('\n').Select(x => string.Join(' ', x.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))).Where(x => x.Length > 0));
    private static TesseractEngine CreateEngine()
    {
        var modelRoot = Path.Combine(AppContext.BaseDirectory, "Models");
        var dataPath = Path.Combine(modelRoot, "tessdata");
        if (!File.Exists(Path.Combine(dataPath, "eng.traineddata")))
            throw new FileNotFoundException("缺少内置英文 OCR 模型 Models/tessdata/eng.traineddata。请重新安装完整模型包。");
        var engine = new TesseractEngine(dataPath, "eng", EngineMode.LstmOnly);
        engine.DefaultPageSegMode = PageSegMode.Auto;
        return engine;
    }
    public void Dispose() => _engine?.Dispose();
}
