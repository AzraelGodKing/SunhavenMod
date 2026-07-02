<#
.SYNOPSIS
    Delegates to scripts/version/verify-version-consistency.ps1 (canonical version alignment check).
#>
[CmdletBinding()]
param()

& (Join-Path $PSScriptRoot 'verify-version-consistency.ps1')
exit $LASTEXITCODE
