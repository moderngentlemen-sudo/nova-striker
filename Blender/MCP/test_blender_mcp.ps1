$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SmokeScript = Join-Path $ScriptDir "ns_blender_mcp_smoke.py"

$McpRoot = $env:NS_BLENDER_MCP_ROOT
$McpPython = $env:NS_BLENDER_MCP_PYTHON

if (!$McpRoot -or !(Test-Path $McpRoot)) {
    throw "Blender MCP source is not configured. Run Setup_Blender_MCP.bat first."
}
if (!$McpPython -or !(Test-Path $McpPython)) {
    throw "Blender MCP Python environment is not configured. Run Setup_Blender_MCP.bat first."
}

$env:BLENDER_MCP_HOST = "127.0.0.1"
$env:BLENDER_MCP_PORT = "9877"

& $McpPython $SmokeScript --mcp-root $McpRoot
if ($LASTEXITCODE -ne 0) {
    throw "Blender MCP smoke test failed."
}

Write-Host ""
Write-Host "Blender MCP read-only smoke test PASSED." -ForegroundColor Green
