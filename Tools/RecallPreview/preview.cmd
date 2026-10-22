@echo off
rem Recall preview: run run.ps1 bypassing the PowerShell execution policy, then open the player.
rem Keep this file ASCII-only; cmd misparses UTF-8 text. Extra arguments go to run.ps1.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run.ps1" %*
if errorlevel 1 (
    echo.
    echo Preview failed, see the error above.
    pause
    exit /b 1
)
start "" "%~dp0..\..\.vissandbox\recall-preview\recall-player.html"
