# Reports how many non-English values still match English (likely untranslated).
param([string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path)

$mods = @(
    "SunhavenTodo", "BirthdayReminder", "CropOptimizer", "SenpaisChest", "TheVault",
    "HavensAlmanac", "SunHavenMuseumUtilityTracker", "HavenDevTools", "HavensRespec"
)
$langs = @("da", "de", "es", "fr", "it", "ja", "ko", "nl", "pt", "pt-BR", "ru", "sv", "zh-CN", "zh-TW", "uk")

$grandTotal = 0
$grandSame = 0

foreach ($mod in $mods) {
    $path = Join-Path (Join-Path $RepoRoot $mod) "Localization\strings.json"
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
    Write-Host ("{0,-32} {1,4} keys  {2,5}/{3,-5} translated ({4}%)" -f $mod, $keys.Count, ($modCells - $modSame), $modCells, $pct)
}

Write-Host ""
$pctAll = if ($grandTotal -gt 0) { [math]::Round(100 * ($grandTotal - $grandSame) / $grandTotal, 1) } else { 0 }
Write-Host ("TOTAL: {0}/{1} cells translated ({2}%); {3} still match English" -f ($grandTotal - $grandSame), $grandTotal, $pctAll, $grandSame)
