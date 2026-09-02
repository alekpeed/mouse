@echo off
rem Launches the portable anti-idle utility in a visible console window.
rem Double-click this file, or run it from the USB drive.
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Jiggle.ps1" %*
endlocal
