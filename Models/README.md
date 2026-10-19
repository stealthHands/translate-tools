# Offline model files

Download these files on a computer with internet access, then copy them into the paths shown below. The program does not download model files at runtime.

## English OCR

Save [eng.traineddata](https://github.com/tesseract-ocr/tessdata_fast/raw/main/eng.traineddata) as `Models/tessdata/eng.traineddata`.

## English to Simplified Chinese translation

Use the ONNX Runtime Web quantized files from [Xenova/opus-mt-en-zh](https://huggingface.co/Xenova/opus-mt-en-zh/tree/main). Save the repository files into `Models/opus-mt-en-zh/` and preserve the `onnx` subfolder:

```text
Models/opus-mt-en-zh/
  config.json
  generation_config.json
  tokenizer.json
  tokenizer_config.json
  onnx/
    encoder_model_quantized.onnx
    decoder_model_merged_quantized.onnx
```

Direct download links:

- [config.json](https://huggingface.co/Xenova/opus-mt-en-zh/resolve/main/config.json)
- [generation_config.json](https://huggingface.co/Xenova/opus-mt-en-zh/resolve/main/generation_config.json)
- [tokenizer.json](https://huggingface.co/Xenova/opus-mt-en-zh/resolve/main/tokenizer.json)
- [tokenizer_config.json](https://huggingface.co/Xenova/opus-mt-en-zh/resolve/main/tokenizer_config.json)
- [encoder_model_quantized.onnx](https://huggingface.co/Xenova/opus-mt-en-zh/resolve/main/onnx/encoder_model_quantized.onnx)
- [decoder_model_merged_quantized.onnx](https://huggingface.co/Xenova/opus-mt-en-zh/resolve/main/onnx/decoder_model_merged_quantized.onnx)

The ONNX weights total roughly 113 MB before transfer overhead; the exact size can change. If Hugging Face is inaccessible on the target machine, download on an unrestricted machine and transfer the files through approved removable media or a local package share.
