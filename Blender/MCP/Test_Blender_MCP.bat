@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0test_blender_mcp.ps1"
echo.
pause
