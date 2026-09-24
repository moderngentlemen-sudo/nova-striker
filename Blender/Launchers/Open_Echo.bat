@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0NovaStriker_Blender.ps1" -Action Open -Character Echo
if errorlevel 1 (
  echo.
  echo Echo open failed.
  pause
)
