namespace ScreenTranslate.Core;

public sealed class TranslationSession : IAsyncDisposable
{
    private readonly IScreenCapture _capture;
    private readonly TesseractOcrService _ocr;
    private readonly ITranslator _translator;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private volatile bool _paused;
    private volatile bool _externalTranslation;
    private ScreenRegion _region;

    public event EventHandler<string>? TranslationReady;
    public event EventHandler<string>? ExternalTextReady;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? Failed;

    public TranslationSession(IScreenCapture capture, TesseractOcrService ocr, ITranslator translator)
    { _capture = capture; _ocr = ocr; _translator = translator; }

    public async Task StartAsync(ScreenRegion region)
    {
        await _lifecycle.WaitAsync();
        try
        {
            await StopCoreAsync();
            _region = region;
            _paused = false;
            _runCts = new CancellationTokenSource();
            _runTask = Task.Run(() => RunLoopAsync(_runCts.Token));
        }
        finally { _lifecycle.Release(); }
    }

    public void SetPaused(bool paused) => _paused = paused;
    public void SetExternalTranslation(bool enabled) => _externalTranslation = enabled;

    public async Task StopAsync()
    {
        await _lifecycle.WaitAsync();
        try { await StopCoreAsync(); }
        finally { _lifecycle.Release(); }
    }

    private async Task StopCoreAsync()
    {
        if (_runCts is null) return;
        _runCts.Cancel();
        try { if (_runTask is not null) await _runTask; } catch (OperationCanceledException) { }
        _runCts.Dispose(); _runCts = null; _runTask = null;
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        var lastFullText = string.Empty;
        var recent = new Queue<string>();
        StatusChanged?.Invoke(this, "正在识别并翻译…");
        while (!token.IsCancellationRequested)
        {
            if (_paused) { await Task.Delay(180, token); continue; }
            try
            {
                var image = await Task.Run(() => _capture.CapturePng(_region), token);
                var current = await Task.Run(() => _ocr.ReadText(image), token);
                var additions = ExtractNewText(lastFullText, current);
                lastFullText = current;
                foreach (var line in additions)
                {
                    var normalized = NormalizeForDedup(line);
                    if (normalized.Length < 3 || recent.Contains(normalized)) continue;
                    recent.Enqueue(normalized);
                    while (recent.Count > 24) recent.Dequeue();
                    if (_externalTranslation)
                    {
                        ExternalTextReady?.Invoke(this, line);
                        continue;
                    }
                    var translated = await _translator.TranslateAsync(line, token);
                    TranslationReady?.Invoke(this, translated);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                Failed?.Invoke(this, ex.Message);
                StatusChanged?.Invoke(this, "识别或翻译失败");
                break;
            }
            await Task.Delay(650, token);
        }
    }

    internal static IReadOnlyList<string> ExtractNewText(string previous, string current)
    {
        var oldLines = Lines(previous);
        var newLines = Lines(current);
        if (newLines.Length == 0) return Array.Empty<string>();
        if (oldLines.Length == 0) return newLines;
        var overlap = 0;
        for (var count = Math.Min(oldLines.Length, newLines.Length); count > 0; count--)
        {
            if (oldLines.Skip(oldLines.Length - count).SequenceEqual(newLines.Take(count), StringComparer.OrdinalIgnoreCase)) { overlap = count; break; }
        }
        if (overlap > 0) return newLines.Skip(overlap).ToArray();
        var oldSet = oldLines.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return newLines.Where(line => !oldSet.Contains(line)).ToArray();
    }

    private static string[] Lines(string value) => value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static string NormalizeForDedup(string line) => string.Concat(line.Where(char.IsLetterOrDigit)).ToLowerInvariant();

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _ocr.Dispose();
        (_translator as IDisposable)?.Dispose();
        _lifecycle.Dispose();
    }
}
