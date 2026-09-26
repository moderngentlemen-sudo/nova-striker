@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup_blender_mcp.ps1" -LaunchAfterSetup $true
if errorlevel 1 (
  echo.
  echo Blender MCP setup failed.
  pause
)
