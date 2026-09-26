@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Nova_Production_Readiness.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
pause
exit /b %EXITCODE%
