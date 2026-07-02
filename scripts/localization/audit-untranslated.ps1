# Reports how many non-English values still match English (likely untranslated).
param(
    [string]$RepoRoot,
    [string]$Mod,
    [switch]$ShowPerLanguage
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\lib\RepoPaths.ps1')

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Get-RepoRoot -StartPath $PSScriptRoot
}

$langs = Get-NonEnglishLangs
$mods = if ([string]::IsNullOrWhiteSpace($Mod)) {
    Get-LocalizedModDirs -StartPath $PSScriptRoot -RepoRoot $RepoRoot
} else {
    @(Resolve-ModDir -Mod $Mod -StartPath $PSScriptRoot)
}

$grandTotal = 0
$grandSame = 0

foreach ($modName in $mods) {
    $path = Join-Path (Join-Path $RepoRoot $modName) 'Localization\strings.json'
    if (-not (Test-Path $path)) {
        Write-Warning "Skipping $modName (missing strings.json)"
        continue
    }

    $json = Get-Content -Raw -Encoding UTF8 $path | ConvertFrom-Json
    $keys = @($json.PSObject.Properties.Name)
    $modCells = $keys.Count * $langs.Count
    $modSame = 0
    $byLang = @{}

    foreach ($lang in $langs) {
        $same = 0
        foreach ($k in $keys) {
            $en = [string]$json.$k.en
            $val = [string]$json.$k.$lang
            if ($val.Trim() -eq $en.Trim()) { $same++ }
        }
        $byLang[$lang] = $same
        $modSame += $same
    }

    $grandTotal += $modCells
    $grandSame += $modSame
    $pct = if ($modCells -gt 0) { [math]::Round(100 * ($modCells - $modSame) / $modCells, 1) } else { 0 }
    Write-Host ("{0,-32} {1,4} keys  {2,5}/{3,-5} translated ({4}%)" -f $modName, $keys.Count, ($modCells - $modSame), $modCells, $pct)

    if ($ShowPerLanguage) {
        foreach ($lang in $langs) {
            $same = $byLang[$lang]
            $langPct = if ($keys.Count -gt 0) { [math]::Round(100 * ($keys.Count - $same) / $keys.Count, 1) } else { 0 }
            Write-Host ("  {0,-8} {1,4}/{2,-4} still English ({3}%)" -f $lang, $same, $keys.Count, $langPct)
        }
    }
}

Write-Host ''
$pctAll = if ($grandTotal -gt 0) { [math]::Round(100 * ($grandTotal - $grandSame) / $grandTotal, 1) } else { 0 }
Write-Host ("TOTAL: {0}/{1} cells translated ({2}%); {3} still match English" -f ($grandTotal - $grandSame), $grandTotal, $pctAll, $grandSame)
