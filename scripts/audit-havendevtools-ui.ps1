$json = Get-Content 'HavenDevTools/Localization/strings.json' -Raw -Encoding UTF8 | ConvertFrom-Json

# Categorize keys based on content analysis and usage context
$userFacing = @()
$technical = @()
$properNouns = @()

foreach ($prop in $json.PSObject.Properties) {
    $key = $prop.Name
    $en = $prop.Value.en
    
    # Check for technical/config path hints
    if ($en -match '\.cfg' -or $en -match 'com\.azraelgodking' -or $en -match '\[.*section\]') {
        $technical += [PSCustomObject]@{ Key = $key; English = $en }
    }
    # Check for command syntax hints (contain English command names)
    elseif ($en -match 'spawn.*tp.*time\.set.*reload\.config' -or $en -match 'spawn, tp, time\.set, reload\.config') {
        $technical += [PSCustomObject]@{ Key = $key; English = $en }
    }
    # Check for mod names / proper nouns that typically aren't translated
    elseif ($key -match 'azrael\.(almanac|birthday|birthright|smut|senpai|vault|todo|trinket)\.title' -or 
            $key -match 'azrael\.tab\.(the_vault|smut|senpais_chest|trinket_fortune)' -or
            $key -eq 'azrael.none_detected') {
        $properNouns += [PSCustomObject]@{ Key = $key; English = $en }
    }
    else {
        $userFacing += [PSCustomObject]@{ Key = $key; English = $en }
    }
}

Write-Host 'HavenDevTools UI Text Audit'
Write-Host '==========================='
Write-Host ''
Write-Host "Total keys: $($json.PSObject.Properties.Count)"
Write-Host "User-facing UI text: $($userFacing.Count)"
Write-Host "Technical hints (contain config paths/command names): $($technical.Count)"
Write-Host "Mod name / proper noun labels: $($properNouns.Count)"
Write-Host ''

Write-Host 'USER-FACING UI TEXT (needs translation)'
Write-Host '----------------------------------------'
foreach ($item in $userFacing | Sort-Object Key) {
    Write-Host "$($item.Key)"
    Write-Host "    EN: $($item.English)"
}

Write-Host ''
Write-Host 'TECHNICAL HINTS (contain English terms that should stay as-is)'
Write-Host '---------------------------------------------------------------'
foreach ($item in $technical | Sort-Object Key) {
    Write-Host "$($item.Key)"
    Write-Host "    EN: $($item.English)"
}

Write-Host ''
Write-Host 'MOD NAME / PROPER NOUN LABELS (may not need translation)'
Write-Host '---------------------------------------------------------'
foreach ($item in $properNouns | Sort-Object Key) {
    Write-Host "$($item.Key)"
    Write-Host "    EN: $($item.English)"
}
