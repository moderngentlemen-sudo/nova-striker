param(
    [string]$McpRepository = "https://github.com/anhez/blender-mcp.git",
    [switch]$LaunchAfterSetup
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Resolve-BlenderExecutable {
    if ($env:NS_BLENDER_EXE -and (Test-Path $env:NS_BLENDER_EXE)) {
        return (Resolve-Path $env:NS_BLENDER_EXE).Path
    }

    if ($env:BLENDER_EXE -and (Test-Path $env:BLENDER_EXE)) {
        return (Resolve-Path $env:BLENDER_EXE).Path
    }

    foreach ($commandName in @("blender.exe", "blender")) {
        $command = Get-Command $commandName -ErrorAction SilentlyContinue
        if ($command) {
            return $command.Source
        }
    }

    $candidates = @()

    if ($env:ProgramFiles) {
        $foundation = Join-Path $env:ProgramFiles "Blender Foundation"
        if (Test-Path $foundation) {
            $candidates += Get-ChildItem $foundation -Directory -ErrorAction SilentlyContinue |
                Sort-Object Name -Descending |
                ForEach-Object { Join-Path $_.FullName "blender.exe" }
        }
    }

    $programFilesX86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    if ($programFilesX86) {
        $candidates += Join-Path $programFilesX86 "Steam\steamapps\common\Blender\blender.exe"
    }

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path $candidate)) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "Blender could not be found. Install Blender or set BLENDER_EXE/NS_BLENDER_EXE."
}

function Resolve-SystemPython {
    $py = Get-Command py.exe -ErrorAction SilentlyContinue
    if ($py) {
        $version = & $py.Source -3 -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')"
        if ($LASTEXITCODE -eq 0) {
            return @{
                Executable = $py.Source
                PrefixArgs = @("-3")
                Version = $version.Trim()
            }
        }
    }

    $python = Get-Command python.exe -ErrorAction SilentlyContinue
    if (!$python) {
        $python = Get-Command python -ErrorAction SilentlyContinue
    }

    if ($python) {
        $version = & $python.Source -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')"
        if ($LASTEXITCODE -eq 0) {
            return @{
                Executable = $python.Source
                PrefixArgs = @()
                Version = $version.Trim()
            }
        }
    }

    throw "Python 3.10+ could not be found. Install Python, then rerun Setup_Blender_MCP.bat."
}

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
$BlenderExe = Resolve-BlenderExecutable
$Python = Resolve-SystemPython

$versionParts = $Python.Version.Split(".")
if ([int]$versionParts[0] -lt 3 -or ([int]$versionParts[0] -eq 3 -and [int]$versionParts[1] -lt 10)) {
    throw "Python 3.10+ is required. Found $($Python.Version)."
}

$ToolsRoot = Join-Path $env:LOCALAPPDATA "NovaStriker\Tools"
$McpRoot = Join-Path $ToolsRoot "blender-mcp"
$VenvDir = Join-Path $McpRoot ".venv"
$VenvPython = Join-Path $VenvDir "Scripts\python.exe"
$LocalDir = Join-Path $ScriptDir ".local"
$EnableScript = Join-Path $ScriptDir "ns_enable_mcp_addon.py"
$StartScript = Join-Path $ScriptDir "start_blender_mcp.ps1"
$TestScript = Join-Path $ScriptDir "test_blender_mcp.ps1"

New-Item -ItemType Directory -Force -Path $ToolsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $LocalDir | Out-Null

Write-Host "Nova Striker Blender MCP Setup" -ForegroundColor White
Write-Host "Blender: $BlenderExe"
Write-Host "Python:  $($Python.Executable) $($Python.Version)"
Write-Host "MCP dir: $McpRoot"

Write-Step "Installing/updating local Blender MCP source"
$git = Get-Command git.exe -ErrorAction SilentlyContinue
if (!$git) {
    $git = Get-Command git -ErrorAction SilentlyContinue
}
if (!$git) {
    throw "Git is required for Blender MCP setup."
}

if (Test-Path (Join-Path $McpRoot ".git")) {
    & $git.Source -C $McpRoot pull --ff-only
    if ($LASTEXITCODE -ne 0) {
        throw "Could not update Blender MCP with a fast-forward pull."
    }
}
elseif (Test-Path $McpRoot) {
    throw "$McpRoot exists but is not a Git checkout. Move/remove it and rerun setup."
}
else {
    & $git.Source clone --depth 1 $McpRepository $McpRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Could not clone Blender MCP."
    }
}

if (!(Test-Path $VenvPython)) {
    Write-Step "Creating isolated Python environment"
    $venvArgs = @()
    $venvArgs += $Python.PrefixArgs
    $venvArgs += @("-m", "venv", $VenvDir)
    & $Python.Executable @venvArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Could not create Blender MCP Python environment."
    }
}

Write-Step "Installing Blender MCP server dependencies"
& $VenvPython -m pip install --disable-pip-version-check -e $McpRoot
if ($LASTEXITCODE -ne 0) {
    throw "Blender MCP Python installation failed."
}

$versionLine = (& $BlenderExe --version | Select-Object -First 1)
if ($versionLine -notmatch "Blender\s+(\d+\.\d+)") {
    throw "Could not determine Blender major/minor version from: $versionLine"
}
$BlenderVersion = $Matches[1]

$AddonSource = Join-Path $McpRoot "addon.py"
if (!(Test-Path $AddonSource)) {
    throw "Blender MCP addon.py was not found at $AddonSource"
}

$AddonDir = Join-Path $env:APPDATA "Blender Foundation\Blender\$BlenderVersion\scripts\addons"
$AddonTarget = Join-Path $AddonDir "maket_blender_mcp.py"
New-Item -ItemType Directory -Force -Path $AddonDir | Out-Null

Write-Step "Installing Blender MCP add-on for Blender $BlenderVersion"
Copy-Item -Force $AddonSource $AddonTarget

$settings = @{
    "BLENDER_MCP_HOST" = "127.0.0.1"
    "BLENDER_MCP_PORT" = "9877"
    "BLENDER_MCP_ADDON_PORT" = "9877"
    "BLENDER_MCP_AUTOSTART" = "0"
    "NS_BLENDER_EXE" = $BlenderExe
    "NS_BLENDER_MCP_ROOT" = $McpRoot
    "NS_BLENDER_MCP_PYTHON" = $VenvPython
}

Write-Step "Writing local-only MCP environment settings"
foreach ($entry in $settings.GetEnumerator()) {
    [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, "User")
    Set-Item -Path ("Env:" + $entry.Key) -Value $entry.Value
}

# Never allow the bridge to be configured to a public bind from this bootstrap.
if ($env:BLENDER_MCP_HOST -ne "127.0.0.1") {
    throw "Security check failed: BLENDER_MCP_HOST must remain 127.0.0.1."
}

Write-Step "Enabling Blender MCP add-on"
$env:NS_BLENDER_MCP_ADDON_MODULE = "maket_blender_mcp"
$previousAutostart = $env:BLENDER_MCP_AUTOSTART
$env:BLENDER_MCP_AUTOSTART = "0"

& $BlenderExe --background --factory-startup --python $EnableScript
$enableExit = $LASTEXITCODE

$env:BLENDER_MCP_AUTOSTART = $previousAutostart

if ($enableExit -ne 0) {
    throw "Blender could not enable the MCP add-on automatically."
}

$ClientConfig = @{
    mcpServers = @{
        "maket-blender" = @{
            command = $VenvPython
            args = @((Join-Path $McpRoot "server.py"))
            env = @{
                BLENDER_MCP_HOST = "127.0.0.1"
                BLENDER_MCP_PORT = "9877"
            }
        }
    }
}
$ClientConfig | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 (Join-Path $LocalDir "mcp-client.local.json")

$State = @{
    blender_exe = $BlenderExe
    blender_version = $BlenderVersion
    mcp_root = $McpRoot
    mcp_python = $VenvPython
    addon_path = $AddonTarget
    host = "127.0.0.1"
    port = 9877
}
$State | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 (Join-Path $LocalDir "setup-state.json")

Write-Host ""
Write-Host "Blender MCP setup complete." -ForegroundColor Green
Write-Host "Local client config: $(Join-Path $LocalDir "mcp-client.local.json")"
Write-Host "The Blender socket is restricted to 127.0.0.1:9877." -ForegroundColor Green
Write-Host "Nova Striker uses its deterministic app-timer bootstrap instead of the upstream UI autostart path." -ForegroundColor Green

if ($LaunchAfterSetup) {
    Write-Step "Launching Nova with Blender MCP"
    & $StartScript -WaitForServer

    Write-Step "Running read-only Blender MCP smoke test"
    & $TestScript
}
