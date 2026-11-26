@echo off
setlocal
title Shardbind Preview
set "SHARDBIND_PWSH=pwsh.exe"
where pwsh.exe >nul 2>&1
if errorlevel 1 set "SHARDBIND_PWSH=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe"
"%SHARDBIND_PWSH%" -NoProfile -File "%~dp0rebuild.ps1" -Interactive
if errorlevel 1 (
    echo.
    echo Preview build failed. See the error above.
    pause
    exit /b 1
)
endlocal
