@echo off
rem ============================================================
rem QuickTidy one-key build script (run from repo root or anywhere)
rem Requires: .NET 8 SDK (dotnet in PATH); optional Inno Setup (iscc in PATH)
rem Outputs:
rem   packaging\payload\                          portable payload
rem   packaging\QuickTidy-1.0.0-portable-win-x64.zip
rem   packaging\out\QuickTidy-1.0.0-setup.exe     (if ISCC found)
rem ============================================================
setlocal
cd /d "%~dp0.."

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] dotnet not found in PATH. Install .NET 8 SDK first.
  exit /b 1
)

echo [1/4] Publishing QuickTidy.MenuHost (self-contained, single file)...
dotnet publish src\QuickTidy.MenuHost -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
if errorlevel 1 exit /b 1

echo [2/4] Publishing QuickTidy.App (self-contained, single file)...
dotnet publish src\QuickTidy.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
if errorlevel 1 exit /b 1

echo [3/4] Assembling payload...
set PAYLOAD=packaging\payload
if exist %PAYLOAD% rmdir /s /q %PAYLOAD%
mkdir %PAYLOAD%\Assets
copy /y src\QuickTidy.App\bin\Release\net8.0-windows\win-x64\publish\QuickTidy.exe %PAYLOAD%\ >nul
copy /y src\QuickTidy.MenuHost\bin\Release\net8.0-windows\win-x64\publish\QuickTidy.MenuHost.exe %PAYLOAD%\ >nul
copy /y assets\qt.ico %PAYLOAD%\ >nul
copy /y packaging\AppxManifest.xml %PAYLOAD%\ >nul
copy /y packaging\Assets\Logo.png %PAYLOAD%\Assets\ >nul
copy /y packaging\Register-SparsePackage.ps1 %PAYLOAD%\ >nul
copy /y packaging\Unregister-SparsePackage.ps1 %PAYLOAD%\ >nul
copy /y README.md %PAYLOAD%\ >nul

echo [4/4] Portable zip...
powershell -NoProfile -Command "Compress-Archive -Force -Path 'packaging\payload\*' -DestinationPath 'packaging\QuickTidy-1.0.0-portable-win-x64.zip'"

where iscc >nul 2>nul
if errorlevel 1 (
  echo [INFO] Inno Setup ^(ISCC^) not found, skipped setup-exe. Install from https://jrsoftware.org/isdl.php then rerun to build installer.
) else (
  echo Building installer...
  iscc packaging\installer.iss
)

echo.
echo DONE. Artifacts in packaging\payload , packaging\*.zip , packaging\out\*.exe
endlocal
