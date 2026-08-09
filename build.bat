@echo off
setlocal

echo ==========================================
echo   Grok Chat App - Batch Build (.NET 9)
echo ==========================================

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [LOI] Khong tim thay dotnet CLI trong PATH.
    echo Hay cai dat .NET 9 SDK tai: https://dotnet.microsoft.com/download/dotnet/9.0
    pause
    exit /b 1
)

echo.
echo [1/2] Restore packages...
dotnet restore
if errorlevel 1 (
    echo [LOI] Restore that bai.
    pause
    exit /b 1
)

echo.
echo [2/2] Build (Release)...
dotnet build -c Release
if errorlevel 1 (
    echo [LOI] Build that bai.
    pause
    exit /b 1
)

echo.
echo ==========================================
echo   BUILD THANH CONG!
echo   File .exe nam trong: bin\Release\net9.0-windows\
echo ==========================================
pause
