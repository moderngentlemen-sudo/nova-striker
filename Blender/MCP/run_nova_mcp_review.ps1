$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..\..")).Path
$StartScript = Join-Path $ScriptDir "start_blender_mcp.ps1"
$ClientScript = Join-Path $ScriptDir "ns_run_nova_review_via_mcp.py"
$OutputDir = Join-Path $RepoRoot "Blender\Reviews\Nova\Articulation\latest"

$McpRoot = $env:NS_BLENDER_MCP_ROOT
$McpPython = $env:NS_BLENDER_MCP_PYTHON

if (!$McpRoot -or !(Test-Path $McpRoot)) {
    throw "Blender MCP source is not configured. Run Setup_Blender_MCP.bat first."
}
if (!$McpPython -or !(Test-Path $McpPython)) {
    throw "Blender MCP Python environment is not configured. Run Setup_Blender_MCP.bat first."
}
if (!(Test-Path $ClientScript)) {
    throw "Missing Nova MCP review client: $ClientScript"
}

Write-Host "Nova Striker MCP-Driven Review" -ForegroundColor White
Write-Host "Transport: standard MCP stdio -> execute_blender_code -> live Blender"

& $StartScript -WaitForServer

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$env:BLENDER_MCP_HOST = "127.0.0.1"
$env:BLENDER_MCP_PORT = "9877"

& $McpPython $ClientScript `
    --mcp-root $McpRoot `
    --repo-root $RepoRoot `
    --output $OutputDir

if ($LASTEXITCODE -ne 0) {
    throw "MCP-driven Nova review failed."
}

$Proof = Join-Path $OutputDir "mcp-run.json"
$Report = Join-Path $OutputDir "articulation-report.json"
$Index = Join-Path $OutputDir "review-index.html"

foreach ($required in @($Proof, $Report, $Index)) {
    if (!(Test-Path $required)) {
        throw "MCP review did not produce required output: $required"
    }
}

Write-Host ""
Write-Host "MCP-driven Nova review complete." -ForegroundColor Green
Write-Host "Proof:  $Proof"
Write-Host "Report: $Report"
Write-Host "Index:  $Index"

Start-Process $Index
