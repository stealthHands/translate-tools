using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ScreenTranslate.Core;

/// <summary>Runs Transformers.js and its ONNX Runtime Web backend inside the app's WebView2.</summary>
public sealed class LocalOnnxTranslator : ITranslator, IDisposable
{
    private readonly WebView2 _browser;
    private readonly TaskCompletionSource _workerReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pending = new();
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private bool _disposed;

    public LocalOnnxTranslator(WebView2 browser)
    {
        _browser = browser;
        _browser.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
        _browser.EnsureCoreWebView2Async();
    }

    public async Task<string> TranslateAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        await EnsureWorkerAsync(cancellationToken);

        var id = Guid.NewGuid().ToString("N");
        var response = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(id, response)) throw new InvalidOperationException("无法创建翻译请求。");
        using var registration = cancellationToken.Register(() => response.TrySetCanceled(cancellationToken));
        try
        {
            await _browser.Dispatcher.InvokeAsync(() => _browser.CoreWebView2.ExecuteScriptAsync($"window.translateLine({JsonSerializer.Serialize(id)}, {JsonSerializer.Serialize(text)});"));
            return await response.Task;
        }
        finally { _pending.TryRemove(id, out _); }
    }

    private async Task EnsureWorkerAsync(CancellationToken cancellationToken)
    {
        await _initializeGate.WaitAsync(cancellationToken);
        try
        {
            if (_workerReady.Task.IsCompleted) { await _workerReady.Task; return; }
            var basePath = AppContext.BaseDirectory;
            var required = new[]
            {
                Path.Combine(basePath, "TranslationWorker", "index.html"),
                Path.Combine(basePath, "TranslationWorker", "vendor", "transformers.min.js"),
                Path.Combine(basePath, "TranslationWorker", "vendor", "ort-wasm-simd-threaded.wasm"),
                Path.Combine(basePath, "Models", "opus-mt-en-zh", "config.json"),
                Path.Combine(basePath, "Models", "opus-mt-en-zh", "tokenizer.json"),
                Path.Combine(basePath, "Models", "opus-mt-en-zh", "onnx", "encoder_model_quantized.onnx"),
                Path.Combine(basePath, "Models", "opus-mt-en-zh", "onnx", "decoder_model_merged_quantized.onnx")
            };
            var missing = required.Where(path => !File.Exists(path)).Select(path => Path.GetRelativePath(basePath, path)).ToArray();
            if (missing.Length > 0)
                throw new FileNotFoundException("本地翻译资源不完整，请按 Models/README.md 下载并放置以下文件：\n• " + string.Join("\n• ", missing));
            await _browser.Dispatcher.InvokeAsync(async () =>
            {
                await _browser.EnsureCoreWebView2Async();
                _browser.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                _browser.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                _browser.CoreWebView2.SetVirtualHostNameToFolderMapping("screentranslate.local", basePath, CoreWebView2HostResourceAccessKind.Allow);
                _browser.CoreWebView2.Navigate("https://screentranslate.local/TranslationWorker/index.html");
            });
            await _workerReady.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        }
        finally { _initializeGate.Release(); }
    }

    private void OnCoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess) _workerReady.TrySetException(e.InitializationException ?? new InvalidOperationException("WebView2 初始化失败。"));
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var root = document.RootElement;
            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (type == "ready") { _workerReady.TrySetResult(); return; }
            if (type == "init-error") { _workerReady.TrySetException(new InvalidOperationException(root.GetProperty("message").GetString())); return; }
            if (type != "translation") return;
            var id = root.GetProperty("id").GetString();
            if (id is null || !_pending.TryGetValue(id, out var completion)) return;
            if (root.GetProperty("ok").GetBoolean()) completion.TrySetResult(root.GetProperty("text").GetString() ?? string.Empty);
            else completion.TrySetException(new InvalidOperationException(root.GetProperty("message").GetString() ?? "本地翻译失败。"));
        }
        catch (Exception ex) { _workerReady.TrySetException(ex); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_browser.CoreWebView2 is not null) _browser.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
        foreach (var request in _pending.Values) request.TrySetCanceled();
        _initializeGate.Dispose();
    }
}
