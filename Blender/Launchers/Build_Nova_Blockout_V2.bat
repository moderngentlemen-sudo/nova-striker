@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0NovaStriker_Blender.ps1" -Action BlockoutV2 -Character Nova
if errorlevel 1 (
  echo.
  echo Nova blockout V2 generation failed.
  pause
)
