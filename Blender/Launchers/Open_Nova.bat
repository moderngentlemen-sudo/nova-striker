@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0NovaStriker_Blender.ps1" -Action Open -Character Nova
if errorlevel 1 (
  echo.
  echo Nova open failed.
  pause
)
