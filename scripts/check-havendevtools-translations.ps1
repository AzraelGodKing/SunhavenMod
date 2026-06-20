$json = Get-Content 'HavenDevTools/Localization/strings.json' -Raw -Encoding UTF8 | ConvertFrom-Json
$languages = @('en','da','de','es','fr','it','ja','ko','nl','pt','pt-BR','ru','sv','zh-CN','zh-TW','uk')
$stats = @{}
foreach ($lang in $languages) { $stats[$lang] = @{ translated=0; same=0; total=0 } }

foreach ($prop in $json.PSObject.Properties) {
    $key = $prop.Name
    $translations = $prop.Value
    $enVal = $translations.en
    foreach ($lang in $languages) {
        $stats[$lang].total++
        if ($translations.$lang -ne $enVal) {
            $stats[$lang].translated++
        } else {
            $stats[$lang].same++
        }
    }
}

Write-Host 'HavenDevTools Translation Report'
Write-Host '===================================='
Write-Host ('Total keys: ' + $stats['en'].total)
Write-Host ''
Write-Host ('{0,-10} {1,10} {2,12} {3,10}' -f 'Language','Translated','Untranslated','Progress')
Write-Host '--------------------------------------------------'
foreach ($lang in $languages) {
    $t = $stats[$lang].translated
    $u = $stats[$lang].same
    $tot = $stats[$lang].total
    $pct = [math]::Round(($t / $tot) * 100, 1)
    Write-Host ('{0,-10} {1,10} {2,12} {3,9}%' -f $lang,$t,$u,$pct)
}

Write-Host ''
Write-Host 'Translated keys per language (values differ from en):'
Write-Host '--------------------------------------------------'
foreach ($lang in $languages) {
    if ($lang -eq 'en') { continue }
    $keys = @()
    foreach ($prop in $json.PSObject.Properties) {
        if ($prop.Value.$lang -ne $prop.Value.en) {
            $keys += $prop.Name
        }
    }
    Write-Host ('{0}: {1} keys' -f $lang, $keys.Count)
    foreach ($k in $keys | Sort-Object) {
        Write-Host ('  - {0}' -f $k)
    }
}
