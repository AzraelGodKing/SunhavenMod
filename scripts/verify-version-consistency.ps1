<#
.SYNOPSIS
    Verify version consistency without requiring Python.

.DESCRIPTION
    Ensures docs/versions.json `version` matches:
    - PLUGIN_VERSION in each mod's Plugin.cs or PluginInfo.cs
    - thunderstore/manifest.json `version_number` when manifest exists

    This mirrors scripts/verify-version-consistency.py so CI/local checks can run
    on machines that do not have Python installed.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDir ".."))
$versionsPath = Join-Path $repoRoot "docs\versions.json"
$readEncoding = [System.Text.Encoding]::UTF8

$modPluginFiles = [ordered]@{
    "com.azraelgodking.squirrelsbirthdayreminder" = @{ ModDir = "BirthdayReminder"; PluginFile = "PluginInfo.cs" }
    "com.azraelgodking.havensbirthright" = @{ ModDir = "HavensBirthright"; PluginFile = "Plugin.cs" }
    "com.azraelgodking.sunhavenmuseumutilitytracker" = @{ ModDir = "SunHavenMuseumUtilityTracker"; PluginFile = "Plugin.cs" }
    "com.azraelgodking.sunhaventodo" = @{ ModDir = "SunhavenTodo"; PluginFile = "PluginInfo.cs" }
    "com.azraelgodking.thevault" = @{ ModDir = "TheVault"; PluginFile = "Plugin.cs" }
    "com.azraelgodking.havendevtools" = @{ ModDir = "HavenDevTools"; PluginFile = "Plugin.cs" }
    "com.azraelgodking.senpaischest" = @{ ModDir = "SenpaisChest"; PluginFile = "PluginInfo.cs" }
    "com.azraelgodking.havensalmanac" = @{ ModDir = "HavensAlmanac"; PluginFile = "PluginInfo.cs" }
    "com.azraelgodking.fasterraces" = @{ ModDir = "FasterRaces"; PluginFile = "PluginInfo.cs" }
    "com.azraelgodking.trinketfortune" = @{ ModDir = "TrinketFortune"; PluginFile = "Plugin.cs" }
    "com.azraelgodking.cropoptimizer" = @{ ModDir = "CropOptimizer"; PluginFile = "PluginInfo.cs" }
    "com.azraelgodking.havensrespec" = @{ ModDir = "HavensRespec"; PluginFile = "PluginInfo.cs" }
}

$pluginVersionRegex = 'PLUGIN_VERSION\s*=\s*"([^"]*)"'
$manifestVersionRegex = '"version_number"\s*:\s*"([^"]*)"'

function Read-TextWithBomSupport {
    param([Parameter(Mandatory = $true)][string]$Path)
    # UTF8Encoding($true) accepts BOM and non-BOM UTF-8 consistently.
    $utf8BomAware = New-Object System.Text.UTF8Encoding($true)
    return [System.IO.File]::ReadAllText($Path, $utf8BomAware)
}

function Read-ManifestVersion {
    param([Parameter(Mandatory = $true)][string]$ManifestPath)
    if (-not (Test-Path $ManifestPath)) {
        return $null
    }

    $text = Read-TextWithBomSupport -Path $ManifestPath
    $m = [regex]::Match($text, $manifestVersionRegex)
    if (-not $m.Success) {
        return $null
    }
    return $m.Groups[1].Value
}

function Read-PluginVersions {
    param([Parameter(Mandatory = $true)][string]$PluginPath)
    $text = Read-TextWithBomSupport -Path $PluginPath
    $matches = [regex]::Matches($text, $pluginVersionRegex)
    $versions = New-Object System.Collections.Generic.List[string]
    foreach ($m in $matches) {
        $versions.Add($m.Groups[1].Value)
    }
    return $versions
}

if (-not (Test-Path $versionsPath)) {
    Write-Error "versions.json not found: $versionsPath"
    exit 1
}

$versionsRaw = Read-TextWithBomSupport -Path $versionsPath
$data = $versionsRaw | ConvertFrom-Json
$errors = New-Object System.Collections.Generic.List[string]

foreach ($prop in $data.PSObject.Properties) {
    $key = [string]$prop.Name
    $entry = $prop.Value

    if (-not $modPluginFiles.Contains($key)) {
        $errors.Add("Unknown mod key in versions.json (add to MOD_PLUGIN_FILES): $key")
        continue
    }

    $want = [string]$entry.version
    if ([string]::IsNullOrWhiteSpace($want)) {
        $errors.Add("${key}: missing version in versions.json")
        continue
    }

    $def = $modPluginFiles[$key]
    $modDir = [string]$def.ModDir
    $pluginFile = [string]$def.PluginFile
    $base = Join-Path $repoRoot $modDir
    $pluginPath = Join-Path $base $pluginFile
    $manifestPath = Join-Path $base "thunderstore\manifest.json"

    if (-not (Test-Path $pluginPath)) {
        $rel = [System.IO.Path]::GetRelativePath($repoRoot, $pluginPath)
        $errors.Add("${key}: missing $rel")
        continue
    }

    $found = Read-PluginVersions -PluginPath $pluginPath
    if ($found.Count -eq 0) {
        $rel = [System.IO.Path]::GetRelativePath($repoRoot, $pluginPath)
        $errors.Add("${key}: no PLUGIN_VERSION in $rel")
        continue
    }

    $uniq = @($found | Sort-Object -Unique)
    if ($uniq.Count -ne 1) {
        $errors.Add("${key}: PLUGIN_VERSION lines disagree in $pluginFile : {$($uniq -join ', ')}")
        continue
    }

    if ($uniq[0] -ne $want) {
        $errors.Add("${key}: PLUGIN_VERSION is '$($uniq[0])' but versions.json says '$want' - run: .\scripts\pre-push-build.ps1 -Mod `<modkey`> (sync), or -Mod `<modkey`> -Bump patch|minor|major")
    }

    if (Test-Path $manifestPath) {
        $mv = Read-ManifestVersion -ManifestPath $manifestPath
        if ($null -eq $mv) {
            $errors.Add("${key}: could not parse version_number in thunderstore/manifest.json")
        }
        elseif ($mv -ne $want) {
            $errors.Add("${key}: manifest version_number is '$mv' but versions.json says '$want'")
        }
    }
}

if ($errors.Count -gt 0) {
    [Console]::Error.WriteLine("Version consistency check failed:")
    [Console]::Error.WriteLine("")
    foreach ($e in $errors) {
        [Console]::Error.WriteLine("  - $e")
    }
    exit 1
}

$modCount = @($data.PSObject.Properties).Count
Write-Host "OK: $modCount mod(s) match docs/versions.json, PLUGIN_VERSION, and manifest."
exit 0
