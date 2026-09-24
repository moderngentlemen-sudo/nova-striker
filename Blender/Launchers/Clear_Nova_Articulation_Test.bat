@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0NovaStriker_Blender.ps1" -Action ClearArticulation -Character Nova
if errorlevel 1 (
  echo.
  echo Nova articulation test cleanup failed.
  pause
)
