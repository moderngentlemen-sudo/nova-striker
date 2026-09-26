@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_nova_mcp_review.ps1"
if errorlevel 1 (
  echo.
  echo MCP-driven Nova review failed.
  pause
)
