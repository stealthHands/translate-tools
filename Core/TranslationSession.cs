using System.Diagnostics;

namespace ScreenTranslate.Core;

public sealed class TranslationSession : IAsyncDisposable
{
    private readonly IScreenCapture _capture;
    private readonly TesseractOcrService _ocr;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private volatile bool _paused;
    private ScreenRegion _region;

    public event EventHandler<string>? SourceTextReady;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? Failed;

    public TranslationSession(IScreenCapture capture, TesseractOcrService ocr)
    { _capture = capture; _ocr = ocr; }

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
        var lastSubmittedText = string.Empty;
        StatusChanged?.Invoke(this, "正在识别并翻译…");
        while (!token.IsCancellationRequested)
        {
            if (_paused) { await Task.Delay(180, token); continue; }
            var cycleStarted = Stopwatch.GetTimestamp();
            try
            {
                var image = await Task.Run(() => _capture.CapturePng(_region), token);
                var current = await Task.Run(() => _ocr.ReadText(image), token);
                if (!string.IsNullOrWhiteSpace(current) && !StringComparer.Ordinal.Equals(current, lastSubmittedText))
                {
                    // Each OCR pass represents the full selected region. Keep its
                    // line breaks and send the complete snapshot as one translation.
                    SourceTextReady?.Invoke(this, current);
                }
                lastSubmittedText = current;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                Failed?.Invoke(this, ex.Message);
                StatusChanged?.Invoke(this, "识别或翻译失败");
                break;
            }
            var remaining = 1000 - Stopwatch.GetElapsedTime(cycleStarted).TotalMilliseconds;
            if (remaining > 0)
                await Task.Delay(TimeSpan.FromMilliseconds(remaining), token);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _ocr.Dispose();
        _lifecycle.Dispose();
    }
}
