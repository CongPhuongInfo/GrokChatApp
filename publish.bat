@echo off
setlocal

echo ==========================================
echo   Grok Chat App - Publish (single .exe)
echo ==========================================

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [LOI] Khong tim thay dotnet CLI trong PATH.
    pause
    exit /b 1
)

echo.
echo Dang publish ban self-contained, single-file cho win-x64...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if errorlevel 1 (
    echo [LOI] Publish that bai.
    pause
    exit /b 1
)

echo.
echo ==========================================
echo   PUBLISH THANH CONG!
echo   File .exe nam trong: bin\Release\net9.0-windows\win-x64\publish\
echo ==========================================
pause
