$files = @{
    'CropOptimizer' = 'CropOptimizer/Localization/strings.json'
    'HavensRespec' = 'HavensRespec/Localization/strings.json'
    'SenpaisChest' = 'SenpaisChest/Localization/strings.json'
    'SMUT' = 'SunHavenMuseumUtilityTracker/Localization/strings.json'
    'SunhavenTodo' = 'SunhavenTodo/Localization/strings.json'
}
foreach ($mod in $files.Keys) {
    $file = $files[$mod]
    Write-Output "=== $mod ==="
    $json = Get-Content $file -Raw -Encoding UTF8 | ConvertFrom-Json
    $json.PSObject.Properties | ForEach-Object {
        $key = $_.Name
        $entry = $_.Value
        $en = $entry.en
        $entry.PSObject.Properties | Where-Object { $_.Name -ne 'en' } | ForEach-Object {
            if ($_.Value -eq $en) {
                Write-Output "$key : $($_.Name) = '$en'"
            }
        }
    }
    Write-Output ""
}
