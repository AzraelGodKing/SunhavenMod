param(
    [string]$RepoRoot,
    [string]$OutPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\lib\RepoPaths.ps1')

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Get-RepoRoot -StartPath $PSScriptRoot
}
if ([string]::IsNullOrWhiteSpace($OutPath)) {
    $OutPath = Join-Path $RepoRoot 'pt-BR-translation-review.md'
}

$mods = foreach ($row in Get-ModMatrix -StartPath $PSScriptRoot) {
    $strings = Join-Path (Join-Path $RepoRoot $row.modDir) 'Localization\strings.json'
    if (-not (Test-Path $strings)) { continue }
    @{
        Name = [string]$row.indexDataName
        Path = "$($row.modDir)/Localization/strings.json"
    }
}

function Format-Cell([string]$s) {
    if ($null -eq $s) { return '' }
    $s = $s -replace "`r`n", '<br>'
    $s = $s -replace "`n", '<br>'
    return ($s -replace '\|', '\|')
}

$lines = @('# pt-BR translation review', '', '| Mod | Key | en | pt-BR |', '| --- | --- | --- | --- |')

foreach ($mod in $mods) {
    $path = Join-Path $RepoRoot ($mod.Path -replace '/', '\')
    $json = Get-Content -Raw -Encoding UTF8 $path | ConvertFrom-Json
    foreach ($prop in $json.PSObject.Properties) {
        $en = Format-Cell ([string]$prop.Value.en)
        $pt = Format-Cell ([string]$prop.Value.'pt-BR')
        $lines += "| $($mod.Name) | $($prop.Name) | $en | $pt |"
    }
    $lines += ''
}

$lines | Set-Content -Path $OutPath -Encoding UTF8
Write-Host "Wrote $OutPath"
