
@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_nova_reference_pack.ps1"
if errorlevel 1 (
  echo.
  echo Nova reference pack generation failed.
  pause
)
