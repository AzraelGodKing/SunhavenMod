# Copies zh-CN into zh-TW when zh-TW still matches English.
param([string]$RepoRoot)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\lib\RepoPaths.ps1')

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Get-RepoRoot -StartPath $PSScriptRoot
}

$mods = Get-LocalizedModDirs -StartPath $PSScriptRoot -RepoRoot $RepoRoot

foreach ($mod in $mods) {
    $path = Join-Path (Join-Path $RepoRoot $mod) 'Localization\strings.json'
    if (-not (Test-Path $path)) { continue }
    $json = Get-Content -Raw -Encoding UTF8 $path | ConvertFrom-Json
    $out = [ordered]@{}
    $fixed = 0
    foreach ($prop in $json.PSObject.Properties) {
        $entry = [ordered]@{}
        foreach ($lang in $prop.Value.PSObject.Properties) {
            $entry[$lang.Name] = [string]$lang.Value
        }
        $en = [string]$entry.en
        $tw = [string]$entry.'zh-TW'
        if ($tw.Trim() -eq $en.Trim() -and -not [string]::IsNullOrWhiteSpace([string]$entry.'zh-CN')) {
            $entry.'zh-TW' = [string]$entry.'zh-CN'
            $fixed++
        }
        $out[$prop.Name] = $entry
    }
    if ($fixed -gt 0) {
        ($out | ConvertTo-Json -Depth 6) | Set-Content -Path $path -Encoding UTF8
        Write-Host "$mod : updated $fixed zh-TW entries from zh-CN"
    }
}
