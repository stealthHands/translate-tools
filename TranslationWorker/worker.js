import { env, pipeline } from './vendor/transformers.min.js';

env.allowRemoteModels = false;
env.allowLocalModels = true;
env.localModelPath = '/Models/';
env.backends.onnx.wasm.wasmPaths = '/TranslationWorker/vendor/';
env.backends.onnx.wasm.numThreads = 2;

let translatorPromise;
function getTranslator() {
  if (!translatorPromise) translatorPromise = pipeline('translation', 'opus-mt-en-zh', { dtype: 'q8' });
  return translatorPromise;
}

chrome.webview.postMessage({ type: 'ready' });

window.translateLine = async (id, text) => {
  try {
    const translator = await getTranslator();
    const result = await translator(text, { max_new_tokens: 160 });
    const translated = Array.isArray(result) ? (result[0]?.translation_text ?? '') : '';
    chrome.webview.postMessage({ type: 'translation', id, ok: true, text: translated });
  } catch (error) {
    chrome.webview.postMessage({ type: 'translation', id, ok: false, message: String(error?.message ?? error) });
  }
};

window.addEventListener('error', event => {
  chrome.webview.postMessage({ type: 'init-error', message: event.message || '本地翻译运行时加载失败。' });
});
