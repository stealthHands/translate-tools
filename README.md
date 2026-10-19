# Screen Translate

A Windows 10 22H2 / Windows 11 x64 desktop tool for selecting a screen region, recognizing English subtitles locally, and translating them to Simplified Chinese through the Google Translate website.

## Features

- Drag a rectangle over a transcript or subtitle region; capture supports desktop coordinates across monitors.
- English OCR with the bundled Tesseract engine and English model.
- Google Translate website is the only translation method. Recognized caption text is sent to Google, and the website is shown in a separate window.
- Pause, resume, stop, choose another region, and adjust the translation overlay's font size and opacity.

## Build

Before building, place Microsoft's WebView2 x64 offline installer at `Dependencies/MicrosoftEdgeWebView2RuntimeInstallerX64.exe`. Run `发布便携版.ps1` on a Windows x64 development computer to create the self-contained `publish` folder. It contains the application, .NET runtime, OCR engine, OCR language data, and WebView2 offline installer. Double-click `启动屏幕翻译.bat`; it installs WebView2 from the included installer if needed, then opens the app. No .NET SDK, Node.js, npm, or ONNX model is needed on the target computer.

Google Translate requires access to `translate.google.com`; translation contents are sent to Google, and the site may change independently.

## License note

Review the licenses for Tesseract, its English OCR data, and WebView2 before redistributing a packaged build.
