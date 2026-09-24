@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0NovaStriker_Blender.ps1" -Action Repair -Character Nova
if errorlevel 1 (
  echo.
  echo Nova repair failed.
  pause
)
