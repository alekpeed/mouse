@echo off
rem ---------------------------------------------------------------------------
rem Builds Peripheral Companion as a single, self-contained, portable .exe.
rem Output: build\dist\PeripheralCompanion.exe  (copy this onto the USB stick)
rem Requires the .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
rem ---------------------------------------------------------------------------
setlocal
cd /d "%~dp0.."

set RID=win-x64
if not "%~1"=="" set RID=%~1

echo Publishing for %RID% ...
dotnet publish src\PeripheralCompanion\PeripheralCompanion.csproj ^
    -c Release ^
    -r %RID% ^
    --self-contained true ^
    -o build\dist

if errorlevel 1 (
    echo.
    echo Build FAILED. Ensure the .NET 8 SDK is installed and on PATH.
    exit /b 1
)

echo.
echo Done. Portable executable:
echo   %~dp0dist\PeripheralCompanion.exe
endlocal
