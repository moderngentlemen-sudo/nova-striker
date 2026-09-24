param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Create", "Open", "Validate", "Export")]
    [string]$Action,

    [Parameter(Mandatory = $true)]
    [ValidateSet("Nova", "Echo")]
    [string]$Character
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Resolve-BlenderExecutable {
    if ($env:BLENDER_EXE -and (Test-Path $env:BLENDER_EXE)) {
        return (Resolve-Path $env:BLENDER_EXE).Path
    }

    foreach ($commandName in @("blender.exe", "blender")) {
        $command = Get-Command $commandName -ErrorAction SilentlyContinue
        if ($command) {
            return $command.Source
        }
    }

    $candidates = New-Object System.Collections.Generic.List[string]

    if ($env:ProgramFiles) {
        $foundation = Join-Path $env:ProgramFiles "Blender Foundation"
        if (Test-Path $foundation) {
            Get-ChildItem $foundation -Directory -ErrorAction SilentlyContinue |
                Sort-Object Name -Descending |
                ForEach-Object {
                    $candidates.Add((Join-Path $_.FullName "blender.exe"))
                }
        }
    }

    $programFilesX86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    if ($programFilesX86) {
        $steamBlender = Join-Path $programFilesX86 "Steam\steamapps\common\Blender\blender.exe"
        $candidates.Add($steamBlender)
    }

    if ($env:LOCALAPPDATA) {
        $localFoundation = Join-Path $env:LOCALAPPDATA "Programs\Blender Foundation"
        if (Test-Path $localFoundation) {
            Get-ChildItem $localFoundation -Directory -ErrorAction SilentlyContinue |
                Sort-Object Name -Descending |
                ForEach-Object {
                    $candidates.Add((Join-Path $_.FullName "blender.exe"))
                }
        }
    }

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path $candidate)) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw @"
Blender could not be found automatically.

Install Blender from blender.org or set an environment variable named BLENDER_EXE
to the full path of blender.exe, for example:

  C:\Program Files\Blender Foundation\Blender 4.5\blender.exe
"@
}

$LauncherDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $LauncherDir "..\..")).Path
$BlenderExe = Resolve-BlenderExecutable

$SetupScript = Join-Path $RepoRoot "Blender\Tools\ns_character_source_setup.py"
$ExportScript = Join-Path $RepoRoot "Blender\Tools\ns_export_character_fbx.py"
$CharacterDir = Join-Path $RepoRoot ("Blender\Characters\" + $Character)
$BlendPath = Join-Path $CharacterDir ($Character + "_master.blend")
$UnityExportDir = Join-Path $RepoRoot ("UnityProject\Assets\Art\Models\Characters\" + $Character)
$FbxPath = Join-Path $UnityExportDir ($Character + ".fbx")

if (!(Test-Path $SetupScript)) {
    throw "Missing Blender setup helper: $SetupScript"
}

if (!(Test-Path $ExportScript)) {
    throw "Missing Blender export helper: $ExportScript"
}

New-Item -ItemType Directory -Force -Path $CharacterDir | Out-Null

Write-Host "Nova Striker Blender Workflow" -ForegroundColor White
Write-Host "Character: $Character"
Write-Host "Action:    $Action"
Write-Host "Blender:   $BlenderExe"

switch ($Action) {
    "Create" {
        if (Test-Path $BlendPath) {
            Write-Step "$Character master file already exists; opening it without overwriting."
        }
        else {
            Write-Step "Creating $Character master scene"

            & $BlenderExe --background --factory-startup --python $SetupScript -- --character $Character --save $BlendPath

            if ($LASTEXITCODE -ne 0) {
                throw "Blender source setup failed with exit code $LASTEXITCODE."
            }
        }

        Write-Step "Opening $Character in Blender"
        Start-Process -FilePath $BlenderExe -ArgumentList @($BlendPath)
    }

    "Open" {
        if (!(Test-Path $BlendPath)) {
            Write-Step "$Character master file does not exist yet; creating it first."

            & $BlenderExe --background --factory-startup --python $SetupScript -- --character $Character --save $BlendPath

            if ($LASTEXITCODE -ne 0) {
                throw "Blender source setup failed with exit code $LASTEXITCODE."
            }
        }

        Write-Step "Opening $Character master file"
        Start-Process -FilePath $BlenderExe -ArgumentList @($BlendPath)
    }

    "Validate" {
        if (!(Test-Path $BlendPath)) {
            throw ($Character + " master file does not exist. Run Create_" + $Character + "_Master.bat first.")
        }

        Write-Step "Validating $Character Blender -> Unity contract"

        & $BlenderExe --background $BlendPath --python $ExportScript -- --character $Character --validate-only --include-blockout

        if ($LASTEXITCODE -ne 0) {
            throw "$Character validation failed with exit code $LASTEXITCODE."
        }

        Write-Host ""
        Write-Host "$Character validation PASSED." -ForegroundColor Green
    }

    "Export" {
        if (!(Test-Path $BlendPath)) {
            throw ($Character + " master file does not exist. Run Create_" + $Character + "_Master.bat first.")
        }

        New-Item -ItemType Directory -Force -Path $UnityExportDir | Out-Null

        Write-Step "Validating and exporting $Character to Unity"

        & $BlenderExe --background $BlendPath --python $ExportScript -- --character $Character --out $FbxPath --include-blockout

        if ($LASTEXITCODE -ne 0) {
            throw "$Character FBX export failed with exit code $LASTEXITCODE."
        }

        Write-Host ""
        Write-Host "Export complete:" -ForegroundColor Green
        Write-Host $FbxPath

        if (Test-Path $FbxPath) {
            Start-Process explorer.exe -ArgumentList ("/select,`"" + $FbxPath + "`"")
        }
    }
}
