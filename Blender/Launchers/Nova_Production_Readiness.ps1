$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..\..")).Path
$ManifestPath = Join-Path $RepoRoot "Production\asset-manifest.json"

if (!(Test-Path $ManifestPath)) {
    throw "Missing production manifest: $ManifestPath"
}

$manifest = Get-Content -Raw $ManifestPath | ConvertFrom-Json
$nova = $manifest.assets | Where-Object { $_.id -eq "CHR-NOVA" }

if (!$nova) {
    throw "CHR-NOVA was not found in the production manifest."
}

Write-Host "Nova Production Modeling Readiness" -ForegroundColor White
Write-Host ""
Write-Host ("Concept:          " + $nova.concept)
Write-Host ("Reference sheet:  " + $nova.sheet)
Write-Host ("Blender blockout: " + $nova.blender_blockout)
Write-Host ("Model final:      " + $nova.model_final)

$requirements = @(
    @{ Name = "Concept approved"; Passed = ($nova.concept -eq "approved") },
    @{ Name = "Reference sheet approved"; Passed = ($nova.sheet -eq "approved") },
    @{ Name = "Blender blockout approved"; Passed = ($nova.blender_blockout -eq "approved") }
)

Write-Host ""
foreach ($requirement in $requirements) {
    if ($requirement.Passed) {
        Write-Host ("[PASS] " + $requirement.Name) -ForegroundColor Green
    }
    else {
        Write-Host ("[BLOCKED] " + $requirement.Name) -ForegroundColor Yellow
    }
}

$ready = ($requirements | Where-Object { -not $_.Passed }).Count -eq 0

Write-Host ""
if ($ready) {
    Write-Host "Nova is READY to begin final production modeling." -ForegroundColor Green
    Write-Host "Use Docs\nova-production-modeling-plan.md as the production sequence."
    exit 0
}

Write-Host "Nova final production modeling is NOT YET UNLOCKED." -ForegroundColor Yellow

if ($nova.sheet -ne "approved") {
    Write-Host ""
    Write-Host "Current blocker: the Nova turnaround/reference sheet is still '$($nova.sheet)'." -ForegroundColor Yellow
    Write-Host "Complete and approve the reference sheet before changing model_final to in_progress."
}

Write-Host ""
Write-Host "The approved V3 blockout remains the authoritative proportion/articulation reference."
exit 2
