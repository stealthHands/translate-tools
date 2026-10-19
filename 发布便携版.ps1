$ErrorActionPreference = 'Stop'

$projectRoot = $PSScriptRoot
$dotnet = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }
$ocrModel = Join-Path $projectRoot 'Models\tessdata\eng.traineddata'
if (-not (Test-Path $ocrModel)) {
    throw '缺少 Models\tessdata\eng.traineddata。请先准备英文 OCR 数据文件，再运行此脚本。'
}
$webViewInstallerParts = 1..3 | ForEach-Object { Join-Path $projectRoot "Dependencies\WebView2Installer.part$_" }
if ($webViewInstallerParts | Where-Object { -not (Test-Path $_) }) {
    throw '缺少 Dependencies\WebView2Installer.part1 到 part3。便携版需要包含 WebView2 离线安装器分卷。'
}

$output = Join-Path $projectRoot 'portable'
& $dotnet publish (Join-Path $projectRoot 'ScreenTranslate.csproj') `
    --configuration Release --runtime win-x64 --self-contained true `
    -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=false `
    --output $output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败，退出码：$LASTEXITCODE" }

$bundledInstaller = Join-Path $output 'Dependencies\MicrosoftEdgeWebView2RuntimeInstallerX64.exe'
if (Test-Path $bundledInstaller) { Remove-Item -LiteralPath $bundledInstaller -Force }

Write-Host "便携版已生成：$output"
Write-Host '在项目目录双击“启动屏幕翻译.bat”即可运行。'
