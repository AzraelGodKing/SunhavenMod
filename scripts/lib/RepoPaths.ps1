# Shared path and mod-matrix helpers for SunhavenMod scripts.
# Dot-source from any script under scripts/:  . (Join-Path $PSScriptRoot '..\lib\RepoPaths.ps1')

function Get-RepoRoot {
    param([string]$StartPath = $PSScriptRoot)

    $p = $StartPath
    while ($p) {
        if (Test-Path (Join-Path $p 'docs\versions.json')) {
            return (Resolve-Path $p).Path
        }
        $parent = Split-Path $p -Parent
        if ([string]::IsNullOrEmpty($parent) -or $parent -eq $p) { break }
        $p = $parent
    }
    throw "Could not locate repo root (docs/versions.json) from $StartPath"
}

function Get-ScriptsRoot {
    param([string]$StartPath = $PSScriptRoot)
    return (Resolve-Path (Join-Path (Get-RepoRoot -StartPath $StartPath) 'scripts')).Path
}

function Get-ModMatrixPath {
    param([string]$StartPath = $PSScriptRoot)
    return Join-Path (Get-ScriptsRoot -StartPath $StartPath) 'matrix\mod-matrix.json'
}

function Get-ModMatrix {
    param([string]$StartPath = $PSScriptRoot)

    $path = Get-ModMatrixPath -StartPath $StartPath
    if (-not (Test-Path $path)) {
        throw "mod-matrix.json not found: $path"
    }
    return Get-Content -Raw -Encoding UTF8 $path | ConvertFrom-Json
}

function Get-LocalizationLangs {
    return @(
        'en', 'da', 'de', 'es', 'fr', 'it', 'ja', 'ko', 'nl', 'pt', 'pt-BR', 'ru', 'sv', 'zh-CN', 'zh-TW', 'uk'
    )
}

function Get-NonEnglishLangs {
    return (Get-LocalizationLangs | Where-Object { $_ -ne 'en' })
}

function Get-LocalizedModDirs {
    param(
        [string]$StartPath = $PSScriptRoot,
        [string]$RepoRoot = (Get-RepoRoot -StartPath $StartPath)
    )

    $dirs = New-Object System.Collections.Generic.List[string]
    foreach ($row in Get-ModMatrix -StartPath $StartPath) {
        $modDir = [string]$row.modDir
        if ([string]::IsNullOrWhiteSpace($modDir)) { continue }
        $stringsPath = Join-Path (Join-Path $RepoRoot $modDir) 'Localization\strings.json'
        if (Test-Path $stringsPath) {
            $dirs.Add($modDir)
        }
    }
    return $dirs
}

function Resolve-ModDir {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Mod,
        [string]$StartPath = $PSScriptRoot
    )

    $needle = $Mod.Trim()
    foreach ($row in Get-ModMatrix -StartPath $StartPath) {
        if ($row.modDir -eq $needle -or $row.modKey -eq $needle) {
            return [string]$row.modDir
        }
    }
    throw "Unknown mod '$needle'. Use modDir or modKey from scripts/matrix/mod-matrix.json."
}

function Test-ModHasLocalization {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ModDir,
        [string]$RepoRoot = (Get-RepoRoot)
    )

    return Test-Path (Join-Path (Join-Path $RepoRoot $ModDir) 'Localization\strings.json')
}
