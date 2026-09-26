@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_nova_automated_review.ps1"
if errorlevel 1 (
  echo.
  echo Nova automated review failed.
  pause
)
