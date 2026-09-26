@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start_blender_mcp.ps1" -WaitForServer
if errorlevel 1 (
  echo.
  echo Blender MCP start failed.
  pause
)
