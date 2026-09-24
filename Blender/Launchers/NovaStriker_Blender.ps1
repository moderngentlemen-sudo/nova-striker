param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Create", "Open", "Repair", "Blockout", "BlockoutV2", "BlockoutV3", "Articulation", "ClearArticulation", "Validate", "Export")]
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
$RepairScript = Join-Path $RepoRoot "Blender\Tools\ns_repair_character_scene.py"
$NovaBlockoutScript = Join-Path $RepoRoot "Blender\Tools\ns_build_nova_blockout.py"
$NovaBlockoutV2Script = Join-Path $RepoRoot "Blender\Tools\ns_build_nova_blockout_v2.py"
$NovaBlockoutV3Script = Join-Path $RepoRoot "Blender\Tools\ns_build_nova_blockout_v3.py"
$NovaArticulationScript = Join-Path $RepoRoot "Blender\Tools\ns_test_nova_articulation.py"
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

if (!(Test-Path $RepairScript)) {
    throw "Missing Blender repair helper: $RepairScript"
}

if (!(Test-Path $NovaBlockoutScript)) {
    throw "Missing Nova blockout helper: $NovaBlockoutScript"
}

if (!(Test-Path $NovaBlockoutV2Script)) {
    throw "Missing Nova blockout V2 helper: $NovaBlockoutV2Script"
}

if (!(Test-Path $NovaBlockoutV3Script)) {
    throw "Missing Nova blockout V3 helper: $NovaBlockoutV3Script"
}

if (!(Test-Path $NovaArticulationScript)) {
    throw "Missing Nova articulation helper: $NovaArticulationScript"
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

            & $BlenderExe --background --factory-startup --python $SetupScript -- --character $Character --save $BlendPath --clean-default-scene

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

            & $BlenderExe --background --factory-startup --python $SetupScript -- --character $Character --save $BlendPath --clean-default-scene

            if ($LASTEXITCODE -ne 0) {
                throw "Blender source setup failed with exit code $LASTEXITCODE."
            }
        }

        Write-Step "Opening $Character master file"
        Start-Process -FilePath $BlenderExe -ArgumentList @($BlendPath)
    }

    "Repair" {
        if (!(Test-Path $BlendPath)) {
            throw ($Character + " master file does not exist. Run Create_" + $Character + "_Master.bat first.")
        }

        Write-Step "Repairing $Character source scene"

        & $BlenderExe --background $BlendPath --python $RepairScript -- --character $Character

        if ($LASTEXITCODE -ne 0) {
            throw "$Character repair failed with exit code $LASTEXITCODE."
        }

        Write-Host ""
        Write-Host "$Character source scene repaired." -ForegroundColor Green
        Write-Step "Opening repaired $Character master file"
        Start-Process -FilePath $BlenderExe -ArgumentList @($BlendPath)
    }

    "Blockout" {
        if ($Character -ne "Nova") {
            throw "The automated silhouette blockout is currently available for Nova only."
        }

        if (!(Test-Path $BlendPath)) {
            throw "Nova master file does not exist. Run Create_Nova_Master.bat first."
        }

        Write-Step "Generating Nova production silhouette blockout"

        & $BlenderExe --background $BlendPath --python $NovaBlockoutScript

        if ($LASTEXITCODE -ne 0) {
            throw "Nova blockout generation failed with exit code $LASTEXITCODE."
        }

        Write-Host ""
        Write-Host "Nova blockout generated and saved." -ForegroundColor Green
        Write-Step "Opening Nova blockout in Blender"
        Start-Process -FilePath $BlenderExe -ArgumentList @($BlendPath)
    }

    "BlockoutV2" {
        if ($Character -ne "Nova") {
            throw "The automated silhouette blockout V2 is currently available for Nova only."
        }

        if (!(Test-Path $BlendPath)) {
            throw "Nova master file does not exist. Run Create_Nova_Master.bat first."
        }

        Write-Step "Generating Nova production silhouette blockout V2"

        & $BlenderExe --background $BlendPath --python $NovaBlockoutV2Script

        if ($LASTEXITCODE -ne 0) {
            throw "Nova blockout V2 generation failed with exit code $LASTEXITCODE."
        }

        Write-Host ""
        Write-Host "Nova blockout V2 generated and saved." -ForegroundColor Green
        Write-Step "Opening Nova blockout V2 in Blender"
        Start-Process -FilePath $BlenderExe -ArgumentList @($BlendPath)
    }

    "BlockoutV3" {
        if ($Character -ne "Nova") {
            throw "The automated silhouette blockout V3 is currently available for Nova only."
        }

        if (!(Test-Path $BlendPath)) {
            throw "Nova master file does not exist. Run Create_Nova_Master.bat first."
        }

        Write-Step "Generating Nova production silhouette blockout V3"

        & $BlenderExe --background $BlendPath --python $NovaBlockoutV3Script

        if ($LASTEXITCODE -ne 0) {
            throw "Nova blockout V3 generation failed with exit code $LASTEXITCODE."
        }

        Write-Host ""
        Write-Host "Nova blockout V3 generated and saved." -ForegroundColor Green
        Write-Step "Opening Nova blockout V3 in Blender"
        Start-Process -FilePath $BlenderExe -ArgumentList @($BlendPath)
    }

    "Articulation" {
        if ($Character -ne "Nova") {
            throw "The automated articulation review is currently available for Nova only."
        }

        if (!(Test-Path $BlendPath)) {
            throw "Nova master file does not exist. Run Create_Nova_Master.bat first."
        }

        Write-Step "Creating Nova articulation and armor-clearance test"

        & $BlenderExe --background $BlendPath --python $NovaArticulationScript

        if ($LASTEXITCODE -ne 0) {
            throw "Nova articulation test generation failed with exit code $LASTEXITCODE."
        }

        Write-Host ""
        Write-Host "Nova articulation test created and saved." -ForegroundColor Green
        Write-Step "Opening Nova articulation review in Blender"
        Start-Process -FilePath $BlenderExe -ArgumentList @($BlendPath)
    }

    "ClearArticulation" {
        if ($Character -ne "Nova") {
            throw "The automated articulation cleanup is currently available for Nova only."
        }

        if (!(Test-Path $BlendPath)) {
            throw "Nova master file does not exist."
        }

        Write-Step "Removing Nova articulation test data"

        & $BlenderExe --background $BlendPath --python $NovaArticulationScript -- --clear

        if ($LASTEXITCODE -ne 0) {
            throw "Nova articulation test cleanup failed with exit code $LASTEXITCODE."
        }

        Write-Host ""
        Write-Host "Nova articulation test removed." -ForegroundColor Green
        Write-Step "Opening Nova in neutral pose"
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
