@echo off
setlocal
set "APP=%~dp0portable\ScreenTranslate.exe"
if not exist "%APP%" (
  echo Portable app is missing. The portable folder must be beside this launcher.
  pause
  exit /b 1
)
set "WEBVIEW=%ProgramFiles(x86)%\Microsoft\EdgeWebView\Application\msedgewebview2.exe"
for /d %%V in ("%ProgramFiles(x86)%\Microsoft\EdgeWebView\Application\*") do if exist "%%~fV\msedgewebview2.exe" set "WEBVIEW=%%~fV\msedgewebview2.exe"
for /d %%V in ("%ProgramFiles%\Microsoft\EdgeWebView\Application\*") do if exist "%%~fV\msedgewebview2.exe" set "WEBVIEW=%%~fV\msedgewebview2.exe"
if exist "%WEBVIEW%" goto startApp
set "PART1=%~dp0Dependencies\WebView2Installer.part1"
set "PART2=%~dp0Dependencies\WebView2Installer.part2"
set "PART3=%~dp0Dependencies\WebView2Installer.part3"
if not exist "%PART1%" goto missingInstaller
if not exist "%PART2%" goto missingInstaller
if not exist "%PART3%" goto missingInstaller
set "INSTALLER=%TEMP%\ScreenTranslate-WebView2-%RANDOM%-%RANDOM%.exe"
copy /b "%PART1%"+"%PART2%"+"%PART3%" "%INSTALLER%" >nul
if errorlevel 1 goto missingInstaller
echo Installing the bundled WebView2 runtime offline. Please wait...
"%INSTALLER%" /silent /install
set "INSTALL_RESULT=%ERRORLEVEL%"
del "%INSTALLER%" >nul 2>nul
if not "%INSTALL_RESULT%"=="0" goto installFailed
:startApp
start "Screen Translate" "%APP%"
exit /b 0
:missingInstaller
if defined INSTALLER del "%INSTALLER%" >nul 2>nul
echo The bundled offline WebView2 installer parts are missing or could not be combined.
pause
exit /b 1
:installFailed
echo WebView2 runtime installation failed.
pause
exit /b 1
