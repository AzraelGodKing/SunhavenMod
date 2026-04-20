<#
.SYNOPSIS
    Detects version drift between docs/versions.json, plugin constants, and thunderstore manifests.
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$versionsPath = Join-Path $repoRoot "docs\versions.json"
$modMatrixPath = Join-Path $PSScriptRoot "mod-matrix.json"

if (-not (Test-Path $versionsPath)) {
    Write-Error "versions.json not found: $versionsPath"
    exit 1
}

if (-not (Test-Path $modMatrixPath)) {
    Write-Error "mod-matrix.json not found: $modMatrixPath"
    exit 1
}

$modDefs = Get-Content $modMatrixPath -Raw -Encoding utf8 | ConvertFrom-Json

$versions = Get-Content $versionsPath -Raw -Encoding utf8 | ConvertFrom-Json
$errors = New-Object System.Collections.Generic.List[string]

foreach ($m in $modDefs) {
    $jsonEntry = $versions.PSObject.Properties[$m.jsonKey]
    if (-not $jsonEntry) {
        $errors.Add("Missing key in versions.json: $($m.jsonKey)")
        continue
    }

    $expectedVersion = [string]$jsonEntry.Value.version
    $pluginPath = Join-Path $repoRoot (Join-Path $m.modDir $m.pluginFile)
    $manifestPath = Join-Path $repoRoot (Join-Path $m.modDir "thunderstore\manifest.json")

    if (-not (Test-Path $pluginPath)) {
        $errors.Add("Missing plugin file: $pluginPath")
        continue
    }
    if (-not (Test-Path $manifestPath)) {
        $errors.Add("Missing thunderstore manifest: $manifestPath")
        continue
    }

    $pluginContent = Get-Content $pluginPath -Raw -Encoding utf8
    $pluginMatch = [regex]::Match($pluginContent, 'PLUGIN_VERSION\s*=\s*"([^"]+)"')
    if (-not $pluginMatch.Success) {
        $errors.Add("PLUGIN_VERSION not found in $pluginPath")
        continue
    }
    $pluginVersion = $pluginMatch.Groups[1].Value

    $manifestContent = Get-Content $manifestPath -Raw -Encoding utf8
    $manifestMatch = [regex]::Match($manifestContent, '"version_number"\s*:\s*"([^"]+)"')
    if (-not $manifestMatch.Success) {
        $errors.Add("version_number not found in $manifestPath")
        continue
    }
    $manifestVersion = $manifestMatch.Groups[1].Value

    if ($expectedVersion -ne $pluginVersion -or $expectedVersion -ne $manifestVersion) {
        $errors.Add(
            "$($m.modDir): versions.json=$expectedVersion, plugin=$pluginVersion, manifest=$manifestVersion"
        )
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Version drift detected:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "Version drift check passed: versions are aligned." -ForegroundColor Green
