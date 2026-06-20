$modDirs = @('BirthdayReminder','CropOptimizer','HavenDevTools','HavensAlmanac','HavensRespec','SenpaisChest','SunHavenMuseumUtilityTracker','SunhavenTodo','TheVault')
$files = @()
foreach ($mod in $modDirs) {
    $p = Join-Path $mod 'Localization/strings.json'
    if (Test-Path $p) { $files += Get-Item $p }
}

$langNames = [ordered]@{
    'da' = 'Danish'; 'de' = 'German'; 'es' = 'Spanish'; 'fr' = 'French'
    'it' = 'Italian'; 'ja' = 'Japanese'; 'ko' = 'Korean'; 'nl' = 'Dutch'; 'pt' = 'Portuguese'
    'pt-BR' = 'Portuguese (BR)'; 'ru' = 'Russian'; 'sv' = 'Swedish'; 'zh-CN' = 'Chinese (Simp)'
    'zh-TW' = 'Chinese (Trad)'; 'uk' = 'Ukrainian'
}

$gaps = @{}
$totalGaps = 0

foreach ($f in $files) {
    $mod = (Split-Path (Split-Path $f.FullName -Parent) -Parent | Split-Path -Leaf)
    $content = [System.IO.File]::ReadAllText($f.FullName)
    $data = $content | ConvertFrom-Json
    
    foreach ($key in $data.PSObject.Properties.Name) {
        $translations = $data.$key
        $enText = $translations.en
        
        foreach ($lang in $langNames.Keys) {
            $val = $translations.$lang
            if ($val -eq $enText) {
                $totalGaps++
                if (-not $gaps[$mod]) { $gaps[$mod] = @{} }
                if (-not $gaps[$mod][$lang]) { $gaps[$mod][$lang] = @() }
                $gaps[$mod][$lang] += $key
            }
        }
    }
}

Write-Host "TOTAL MISSING TRANSLATIONS (English copies): $totalGaps"
Write-Host ""

foreach ($mod in ($gaps.Keys | Sort-Object)) {
    $modTotal = 0
    foreach ($lang in $gaps[$mod].Keys) {
        $modTotal += $gaps[$mod][$lang].Count
    }
    Write-Host "## $mod ($modTotal gaps)"
    foreach ($lang in ($gaps[$mod].Keys | Sort-Object)) {
        $count = $gaps[$mod][$lang].Count
        Write-Host "  $($langNames[$lang]): $count strings"
    }
    Write-Host ""
}

# Export a sample of the most common untranslated keys across all mods
Write-Host "="*60
Write-Host "TOP 20 MOST COMMON UNTRANSLATED KEYS (across all mods)"
Write-Host "="*60
$keyCounts = @{}
foreach ($mod in $gaps.Keys) {
    foreach ($lang in $gaps[$mod].Keys) {
        foreach ($key in $gaps[$mod][$lang]) {
            if (-not $keyCounts[$key]) { $keyCounts[$key] = 0 }
            $keyCounts[$key]++
        }
    }
}

$topKeys = $keyCounts.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 20
foreach ($k in $topKeys) {
    Write-Host "$($k.Value.ToString().PadLeft(3)) languages missing: $($k.Key)"
}
