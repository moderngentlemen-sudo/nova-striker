@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0NovaStriker_Blender.ps1" -Action Blockout -Character Nova
if errorlevel 1 (
  echo.
  echo Nova blockout generation failed.
  pause
)
