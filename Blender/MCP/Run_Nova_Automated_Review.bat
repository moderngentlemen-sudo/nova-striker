@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_nova_mcp_review.ps1"
if errorlevel 1 (
  echo.
  echo MCP-driven Nova automated review failed.
  echo You can use Run_Nova_Headless_Review.bat as the deterministic fallback.
  pause
)
