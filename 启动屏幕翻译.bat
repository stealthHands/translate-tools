@echo off
set "APP=%~dp0publish\ScreenTranslate.exe"
if not exist "%APP%" (
  echo Portable app is missing. Run the publish script first.
  pause
  exit /b 1
)
set "WEBVIEW=%ProgramFiles(x86)%\Microsoft\EdgeWebView\Application\msedgewebview2.exe"
for /d %%V in ("%ProgramFiles(x86)%\Microsoft\EdgeWebView\Application\*") do if exist "%%~fV\msedgewebview2.exe" set "WEBVIEW=%%~fV\msedgewebview2.exe"
for /d %%V in ("%ProgramFiles%\Microsoft\EdgeWebView\Application\*") do if exist "%%~fV\msedgewebview2.exe" set "WEBVIEW=%%~fV\msedgewebview2.exe"
if exist "%WEBVIEW%" goto startApp

set "INSTALLER=%~dp0publish\Dependencies\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"
if not exist "%INSTALLER%" goto missingInstaller
echo Installing the bundled WebView2 runtime. Please wait...
"%INSTALLER%" /silent /install
if errorlevel 1 goto installFailed

:startApp
start "Screen Translate" "%APP%"
exit /b 0

:missingInstaller
echo Bundled WebView2 installer is missing. Run the publish script again.
pause
exit /b 1

:installFailed
echo WebView2 runtime installation failed.
pause
exit /b 1
