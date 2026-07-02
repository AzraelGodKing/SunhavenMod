<#
.SYNOPSIS
    Delegates to scripts/version/verify-version-consistency.py (canonical checks).

.DESCRIPTION
    The Python verifier is the single source of truth (stray manifest detection, versions.json, PLUGIN_VERSION, thunderstore manifests).
    Uses scripts/lib/Resolve-Python.ps1 to find a working Python 3 locally and on runners.

    Override: $env:PYTHON = 'C:\Path\To\python.exe'
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pyScript = Join-Path $scriptDir 'verify-version-consistency.py'

if (-not (Test-Path $pyScript)) {
    Write-Error "Missing $pyScript"
    exit 1
}

. (Join-Path $scriptDir '..\lib\Resolve-Python.ps1')
$exitCode = Invoke-PythonScript -ScriptPath $pyScript
exit $exitCode
