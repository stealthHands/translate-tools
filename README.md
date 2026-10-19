# Screen Translate

A Windows 10 22H2 / Windows 11 x64 desktop tool for selecting a screen region, recognizing English subtitles locally, and translating them to Simplified Chinese through the Google Translate website.

## Run from a GitHub checkout

Download or clone the complete repository, then double-click `启动屏幕翻译.bat`. The checked-in `portable` folder contains the self-contained app and .NET runtime; the English OCR model and native Tesseract runtime are included in the project. If WebView2 is not already installed, the launcher combines the three installer parts in `Dependencies` and installs WebView2 offline. No Git LFS client or installer download is needed.

The WebView2 offline installer exceeds GitHub's 100 MB per-file limit, so it is split into three regular repository files, each under that limit. This keeps the installer available in both a normal clone and a GitHub source ZIP.

The app itself has no .NET, SDK, Node.js, npm, or ONNX installation requirement on the target computer. Translation uses the Google Translate website, so translating text requires internet access to `translate.google.com`; OCR runs locally.

## Rebuild the portable app

On a Windows x64 development computer with the .NET 8 SDK, run `发布便携版.ps1`. This regenerates the checked-in `portable` directory. Building from source restores NuGet packages as usual; running the packaged app does not.

## License note

Review the licenses for Tesseract, its English OCR data, .NET, and WebView2 before redistributing this project.
