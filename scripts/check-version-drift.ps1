<#
.SYNOPSIS
    Detects version drift between docs/versions.json, plugin constants, and thunderstore manifests.
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$versionsPath = Join-Path $repoRoot "docs\versions.json"

if (-not (Test-Path $versionsPath)) {
    Write-Error "versions.json not found: $versionsPath"
    exit 1
}

$modDefs = @(
    @{ key = "com.azraelgodking.senpaischest"; dir = "SenpaisChest"; plugin = "PluginInfo.cs" },
    @{ key = "com.azraelgodking.havensbirthright"; dir = "HavensBirthright"; plugin = "Plugin.cs" },
    @{ key = "com.azraelgodking.sunhavenmuseumutilitytracker"; dir = "SunHavenMuseumUtilityTracker"; plugin = "Plugin.cs" },
    @{ key = "com.azraelgodking.squirrelsbirthdayreminder"; dir = "BirthdayReminder"; plugin = "PluginInfo.cs" },
    @{ key = "com.azraelgodking.sunhaventodo"; dir = "SunhavenTodo"; plugin = "PluginInfo.cs" },
    @{ key = "com.azraelgodking.thevault"; dir = "TheVault"; plugin = "Plugin.cs" },
    @{ key = "com.azraelgodking.havendevtools"; dir = "HavenDevTools"; plugin = "Plugin.cs" },
    @{ key = "com.azraelgodking.havensalmanac"; dir = "HavensAlmanac"; plugin = "PluginInfo.cs" },
    @{ key = "com.azraelgodking.fasterraces"; dir = "FasterRaces"; plugin = "Plugin.cs" },
    @{ key = "com.azraelgodking.trinketfortune"; dir = "TrinketFortune"; plugin = "Plugin.cs" }
)

$versions = Get-Content $versionsPath -Raw -Encoding utf8 | ConvertFrom-Json
$errors = New-Object System.Collections.Generic.List[string]

foreach ($m in $modDefs) {
    $jsonEntry = $versions.PSObject.Properties[$m.key]
    if (-not $jsonEntry) {
        $errors.Add("Missing key in versions.json: $($m.key)")
        continue
    }

    $expectedVersion = [string]$jsonEntry.Value.version
    $pluginPath = Join-Path $repoRoot (Join-Path $m.dir $m.plugin)
    $manifestPath = Join-Path $repoRoot (Join-Path $m.dir "thunderstore\manifest.json")

    if (-not (Test-Path $pluginPath)) {
        $errors.Add("Missing plugin file: $pluginPath")
        continue
    }
    if (-not (Test-Path $manifestPath)) {
        $errors.Add("Missing thunderstore manifest: $manifestPath")
        continue
    }

    $pluginContent = Get-Content $pluginPath -Raw -Encoding utf8
    $pluginMatch = [regex]::Match($pluginContent, 'PLUGIN_VERSION\s*=\s*"([^"]+)"')
    if (-not $pluginMatch.Success) {
        $errors.Add("PLUGIN_VERSION not found in $pluginPath")
        continue
    }
    $pluginVersion = $pluginMatch.Groups[1].Value

    $manifestContent = Get-Content $manifestPath -Raw -Encoding utf8
    $manifestMatch = [regex]::Match($manifestContent, '"version_number"\s*:\s*"([^"]+)"')
    if (-not $manifestMatch.Success) {
        $errors.Add("version_number not found in $manifestPath")
        continue
    }
    $manifestVersion = $manifestMatch.Groups[1].Value

    if ($expectedVersion -ne $pluginVersion -or $expectedVersion -ne $manifestVersion) {
        $errors.Add(
            "$($m.dir): versions.json=$expectedVersion, plugin=$pluginVersion, manifest=$manifestVersion"
        )
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Version drift detected:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "Version drift check passed: versions are aligned." -ForegroundColor Green
