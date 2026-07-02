<#
.SYNOPSIS
    Verifies required Sun Haven reference DLLs exist at SUNHAVEN_PATH.

.DESCRIPTION
    This script is intended for local/self-hosted CI preflight checks.
    It does not modify files and exits non-zero when references are missing.
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$SunHavenPath = $env:SUNHAVEN_PATH
)

$ErrorActionPreference = "Stop"

if (-not $SunHavenPath) {
    Write-Error "SUNHAVEN_PATH is not set. Pass -SunHavenPath or set env var."
    exit 1
}

$managedPath = Join-Path $SunHavenPath "Sun Haven_Data\Managed"
if (-not (Test-Path $managedPath)) {
    Write-Error "Managed path not found: $managedPath"
    exit 1
}

# Baseline references commonly required across Sun Haven mods.
$requiredDlls = @(
    "Assembly-CSharp.dll",
    "BepInEx.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.dll",
    "Unity.TextMeshPro.dll",
    "mscorlib.dll",
    "System.dll",
    "System.Core.dll"
)

$missing = @()
foreach ($dll in $requiredDlls) {
    $dllPath = Join-Path $managedPath $dll
    if (-not (Test-Path $dllPath)) {
        $missing += $dll
    }
}

if ($missing.Count -gt 0) {
    Write-Host "Missing required references in $managedPath:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "Reference preflight passed: all required DLLs present in $managedPath" -ForegroundColor Green
