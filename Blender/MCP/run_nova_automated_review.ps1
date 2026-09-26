$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..\..")).Path
$ReviewScript = Join-Path $ScriptDir "ns_nova_automated_review.py"
$NovaBlend = Join-Path $RepoRoot "Blender\Characters\Nova\Nova_master.blend"
$OutputDir = Join-Path $RepoRoot "Blender\Reviews\Nova\Articulation\latest"

$BlenderExe = $env:NS_BLENDER_EXE
if (!$BlenderExe) {
    $BlenderExe = $env:BLENDER_EXE
}
if (!$BlenderExe -or !(Test-Path $BlenderExe)) {
    throw "Blender is not configured. Run Setup_Blender_MCP.bat first."
}
if (!(Test-Path $NovaBlend)) {
    throw "Nova_master.blend does not exist."
}

$TempRoot = Join-Path $env:TEMP "NovaStriker\BlenderReview"
New-Item -ItemType Directory -Force -Path $TempRoot | Out-Null
$TempBlend = Join-Path $TempRoot "Nova_review.blend"
Copy-Item -Force $NovaBlend $TempBlend

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
Get-ChildItem $OutputDir -File -ErrorAction SilentlyContinue | Remove-Item -Force

# The deterministic review uses a temporary .blend copy and does not need a
# second MCP listener. Disable MCP autostart for this headless Blender process.
$previousAutostart = $env:BLENDER_MCP_AUTOSTART
$env:BLENDER_MCP_AUTOSTART = "0"

& $BlenderExe --background $TempBlend --python $ReviewScript -- --output $OutputDir
$reviewExit = $LASTEXITCODE

$env:BLENDER_MCP_AUTOSTART = $previousAutostart

if ($reviewExit -ne 0) {
    throw "Nova automated articulation review failed."
}

Write-Host ""
Write-Host "Nova automated review complete:" -ForegroundColor Green
Write-Host $OutputDir

Start-Process explorer.exe -ArgumentList $OutputDir
