# Copies zh-CN into zh-TW when zh-TW still matches English.
param([string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path)

$mods = @(
    "SunhavenTodo", "BirthdayReminder", "CropOptimizer", "SenpaisChest", "TheVault",
    "HavensAlmanac", "SunHavenMuseumUtilityTracker", "HavenDevTools", "HavensRespec"
)

foreach ($mod in $mods) {
    $path = Join-Path (Join-Path $RepoRoot $mod) "Localization\strings.json"
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
        $tw = [string]$entry["zh-TW"]
        $cn = [string]$entry["zh-CN"]
        if ($tw.Trim() -eq $en.Trim() -and $cn.Trim() -ne $en.Trim()) {
            $entry["zh-TW"] = $cn
            $fixed++
        }
        $out[$prop.Name] = $entry
    }
    if ($fixed -gt 0) {
        ($out | ConvertTo-Json -Depth 6) | Set-Content -Path $path -Encoding UTF8
        Write-Host "$mod : $fixed zh-TW cells from zh-CN"
    }
}
