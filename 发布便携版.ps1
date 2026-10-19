$ErrorActionPreference = 'Stop'

$projectRoot = $PSScriptRoot
$dotnet = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }
$ocrModel = Join-Path $projectRoot 'Models\tessdata\eng.traineddata'
if (-not (Test-Path $ocrModel)) {
    throw '缺少 Models\tessdata\eng.traineddata。请先准备英文 OCR 数据文件，再运行此脚本。'
}
$webViewInstaller = Join-Path $projectRoot 'Dependencies\MicrosoftEdgeWebView2RuntimeInstallerX64.exe'
if (-not (Test-Path $webViewInstaller)) {
    throw '缺少 Dependencies\MicrosoftEdgeWebView2RuntimeInstallerX64.exe。便携版需要包含 WebView2 离线安装器。'
}

$output = Join-Path $projectRoot 'publish'
& $dotnet publish (Join-Path $projectRoot 'ScreenTranslate.csproj') `
    --configuration Release --runtime win-x64 --self-contained true `
    -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=false `
    --output $output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败，退出码：$LASTEXITCODE" }

Write-Host "便携版已生成：$output"
Write-Host '在项目目录双击“启动屏幕翻译.bat”即可运行。'
