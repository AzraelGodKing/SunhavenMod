$files = @(
    'HavensAlmanac/Localization/strings.json',
    'HavenDevTools/Localization/strings.json',
    'CropOptimizer/Localization/strings.json',
    'HavensRespec/Localization/strings.json',
    'SenpaisChest/Localization/strings.json',
    'SunHavenMuseumUtilityTracker/Localization/strings.json',
    'SunhavenTodo/Localization/strings.json'
)
foreach ($file in $files) {
    Write-Output "=== $file ==="
    $json = Get-Content $file -Raw -Encoding UTF8 | ConvertFrom-Json
    $totalKeys = ($json | Get-Member -MemberType NoteProperty).Count
    $englishCopyCount = 0
    $json.PSObject.Properties | ForEach-Object {
        $key = $_.Name
        $entry = $_.Value
        $en = $entry.en
        $entry.PSObject.Properties | Where-Object { $_.Name -ne 'en' } | ForEach-Object {
            if ($_.Value -eq $en) {
                $englishCopyCount++
            }
        }
    }
    Write-Output "Keys: $totalKeys, English copies: $englishCopyCount"
}
