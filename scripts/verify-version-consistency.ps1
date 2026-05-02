<#
.SYNOPSIS
    Delegates to scripts/verify-version-consistency.py (canonical checks).

.DESCRIPTION
    The Python verifier is the single source of truth (stray manifest detection, versions.json, PLUGIN_VERSION, thunderstore manifests).
    Install Python 3 or use `py -3 scripts\verify-version-consistency.py`.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pyScript = Join-Path $scriptDir "verify-version-consistency.py"

if (-not (Test-Path $pyScript)) {
    Write-Error "Missing $pyScript"
    exit 1
}

$python = Get-Command python -ErrorAction SilentlyContinue
if (-not $python) {
    $python = Get-Command py -ErrorAction SilentlyContinue
}
if (-not $python) {
    Write-Error "Python 3 is required. Install from python.org or run: py -3 `"$pyScript`""
    exit 1
}

& $python.Source $pyScript
exit $LASTEXITCODE
