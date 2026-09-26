param(
    [switch]$WaitForServer
)

$ErrorActionPreference = "Stop"

function Test-Port([int]$Port) {
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $async = $client.BeginConnect("127.0.0.1", $Port, $null, $null)
        $ok = $async.AsyncWaitHandle.WaitOne(600)
        if (!$ok) {
            $client.Close()
            return $false
        }
        $client.EndConnect($async)
        $client.Close()
        return $true
    }
    catch {
        return $false
    }
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..\..")).Path
$NovaBlend = Join-Path $RepoRoot "Blender\Characters\Nova\Nova_master.blend"

$BlenderExe = $env:NS_BLENDER_EXE
if (!$BlenderExe) {
    $BlenderExe = $env:BLENDER_EXE
}
if (!$BlenderExe -or !(Test-Path $BlenderExe)) {
    throw "Blender MCP is not configured. Run Setup_Blender_MCP.bat first."
}
if (!(Test-Path $NovaBlend)) {
    throw "Nova_master.blend does not exist. Create Nova's master file first."
}

$env:BLENDER_MCP_HOST = "127.0.0.1"
$env:BLENDER_MCP_PORT = "9877"
$env:BLENDER_MCP_ADDON_PORT = "9877"
$env:BLENDER_MCP_AUTOSTART = "1"

if (Test-Port 9877) {
    Write-Host "Blender MCP is already listening on 127.0.0.1:9877." -ForegroundColor Green
    exit 0
}

Write-Host "Starting Blender with Nova_master.blend and MCP autostart..."
Start-Process -FilePath $BlenderExe -ArgumentList @("`"$NovaBlend`"")

if ($WaitForServer) {
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Milliseconds 500
        if (Test-Port 9877) {
            $ready = $true
            break
        }
    }

    if (!$ready) {
        throw "Blender opened, but MCP did not begin listening on 127.0.0.1:9877 within 15 seconds."
    }

    Write-Host "Blender MCP is ready on 127.0.0.1:9877." -ForegroundColor Green
}
