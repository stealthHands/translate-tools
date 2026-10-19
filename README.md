# Screen Translate

A Windows 10 22H2 / Windows 11 desktop tool for selecting a screen region, recognizing English subtitles locally, and translating them to Simplified Chinese in a floating window.

## Features

- Drag a rectangle over a transcript or subtitle region; capture supports desktop coordinates across monitors.
- Local English OCR with Tesseract and local English-to-Chinese translation with OPUS-MT ONNX via Transformers.js and WebView2.
- Optional Google Translate website mode for computers that can reach the website but cannot use the offline translation model. Recognized caption text is sent to Google in this mode.
- Pause, resume, stop, choose another region, and adjust the translation overlay's font size and opacity.

## Build

Install the .NET 8 SDK and restore/build `ScreenTranslate.csproj` on Windows x64. The computer also needs the Microsoft Edge WebView2 Runtime (present on current Windows 11 systems and installable on Windows 10).

The embedded browser runtime is staged from the pinned npm dependency:

```powershell
cd TranslationWorker
npm install
npm run stage
cd ..
dotnet build ScreenTranslate.csproj -c Release
```

The application uses WebView2 only as a local ONNX runtime host in offline mode; it disables remote model loading. `TranslationWorker/vendor/` contains the Transformers.js and ONNX Runtime Web browser assets required for a distributable build. Publish a self-contained x64 build so the target computer does not need the .NET runtime:

```powershell
dotnet publish ScreenTranslate.csproj -c Release -r win-x64 --self-contained true
```

Distribute the generated publish folder together with the `Models` and `TranslationWorker` content copied by the build.

## Models

Model weights are not committed to this Git repository. Download and place Tesseract `eng.traineddata` and the OPUS-MT ONNX/tokenizer files as described in [Models/README.md](Models/README.md). After copying them to `Models/`, rebuild or copy the `Models` folder beside the app executable. The installed app can then run OCR and local translation without internet access.

The Google Translate mode is an online fallback, not the Google Cloud Translation API. It requires access to `translate.google.com`; translation contents are sent to Google, and its webpage behavior depends on Google's current site UI.

## License note

Review the licenses for the OPUS-MT model, Tesseract OCR data, Transformers.js, ONNX Runtime Web, and WebView2 before redistributing a packaged build.
