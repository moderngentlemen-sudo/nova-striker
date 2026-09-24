@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0NovaStriker_Blender.ps1" -Action Repair -Character Echo
if errorlevel 1 (
  echo.
  echo Echo repair failed.
  pause
)
