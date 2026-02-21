<#
.SYNOPSIS
    Pre-push bump and build for Sun Haven mods.

.DESCRIPTION
    Bumps all version locations and builds: versions.json, PluginInfo.cs/Plugin.cs,
    thunderstore/manifest.json. Run before push — version-bump-on-merge workflow has been dropped.

.PARAMETER Mod
    Mod key (e.g. senpaischest, havensbirthright). Required unless -All is used.

.PARAMETER Bump
    Bump type: major, minor, or patch. Required for pre-push; omit only for post-merge sync.

.PARAMETER All
    Process all mods. Use with -Bump.

.EXAMPLE
    .\pre-push-build.ps1 -Mod senpaischest -Bump patch

.EXAMPLE
    .\pre-push-build.ps1 -Mod senpaischest
    (sync from versions.json, no bump - use after pulling a version-bump merge)
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet(
        "senpaischest", "havensbirthright", "sunhavenmuseumutilitytracker",
        "squirrelsbirthdayreminder", "sunhaventodo", "thevault",
        "havendevtools", "havensalmanac"
    )]
    [string]$Mod,

    [Parameter(Mandatory = $false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$Bump,

    [Parameter(Mandatory = $false)]
    [switch]$All
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptRoot
$VersionsPath = Join-Path $RepoRoot "docs\versions.json"

# Mod mapping: modKey -> [jsonKey, modDir, versionInPluginInfo]
$ModMap = @{
    "senpaischest"                 = @("com.azraelgodking.senpaischest", "SenpaisChest", $true)
    "havensbirthright"             = @("com.azraelgodking.havensbirthright", "HavensBirthright", $false)
    "sunhavenmuseumutilitytracker" = @("com.azraelgodking.sunhavenmuseumutilitytracker", "SunHavenMuseumUtilityTracker", $false)
    "squirrelsbirthdayreminder"    = @("com.azraelgodking.squirrelsbirthdayreminder", "BirthdayReminder", $true)
    "sunhaventodo"                 = @("com.azraelgodking.sunhaventodo", "SunhavenTodo", $true)
    "thevault"                     = @("com.azraelgodking.thevault", "TheVault", $false)
    "havendevtools"                = @("com.azraelgodking.havendevtools", "HavenDevTools", $false)
    "havensalmanac"                = @("com.azraelgodking.havensalmanac", "HavensAlmanac", $true)
}

function Get-VersionParts {
    param([string]$Version)
    $parts = $Version -split '\.'
    $major = [int]($parts[0])
    $minor = if ($parts.Count -ge 2) { [int]$parts[1] } else { 0 }
    $patch = if ($parts.Count -ge 3) { [int]$parts[2] } else { 0 }
    return @{ Major = $major; Minor = $minor; Patch = $patch }
}

function Bump-Version {
    param([string]$Current, [string]$BumpType)
    $p = Get-VersionParts $Current
    switch ($BumpType) {
        "major" { return "$($p.Major + 1).0.0" }
        "minor" { return "$($p.Major).$($p.Minor + 1).0" }
        "patch" { return "$($p.Major).$($p.Minor).$($p.Patch + 1)" }
        default { throw "Unknown bump type: $BumpType" }
    }
}

function Update-VersionsJson {
    param([string]$JsonKey, [string]$Version)
    $content = Get-Content $VersionsPath -Raw
    $pattern = "(`"$([regex]::Escape($JsonKey))`":\s*\{\s*`"version`":\s*)`"[^`"]*`""
    $replacement = "`${1}`"$Version`""
    if ($content -match $pattern) {
        $content = $content -replace $pattern, $replacement
        Set-Content $VersionsPath -Value $content -NoNewline
        Write-Host "  Updated versions.json -> $Version"
    } else {
        Write-Warning "  Could not find $JsonKey in versions.json"
    }
}

function Sync-AndBuild {
    param(
        [string]$ModKey,
        [string]$Version,
        [bool]$UpdateVersionsJson
    )
    $info = $ModMap[$ModKey]
    if (-not $info) { throw "Unknown mod: $ModKey" }
    $jsonKey = $info[0]
    $modDir = $info[1]
    $usePluginInfo = $info[2]

    $modPath = Join-Path $RepoRoot $modDir

    if ($UpdateVersionsJson) {
        Update-VersionsJson -JsonKey $jsonKey -Version $Version
    }

    # Update PluginInfo.cs or Plugin.cs
    $versionFile = if ($usePluginInfo) { "PluginInfo.cs" } else { "Plugin.cs" }
    $versionFilePath = Join-Path $modPath $versionFile
    if (-not (Test-Path $versionFilePath)) {
        Write-Warning "Version file not found: $versionFilePath"
    } else {
        $content = Get-Content $versionFilePath -Raw
        $content = $content -replace 'PLUGIN_VERSION = "[^"]*"', "PLUGIN_VERSION = `"$Version`""
        Set-Content $versionFilePath -Value $content -NoNewline
        Write-Host "  Updated $versionFile -> $Version"
    }

    # Update thunderstore/manifest.json
    $manifestPath = Join-Path $modPath "thunderstore\manifest.json"
    if (Test-Path $manifestPath) {
        $content = Get-Content $manifestPath -Raw
        $content = $content -replace '"version_number":\s*"[^"]*"', "`"version_number`": `"$Version`""
        Set-Content $manifestPath -Value $content -NoNewline
        Write-Host "  Updated thunderstore/manifest.json -> $Version"
    } else {
        Write-Warning "  manifest.json not found: $manifestPath"
    }

    # Build (DLL gets version from PluginInfo at compile time)
    $csproj = Join-Path $modPath "$modDir.csproj"
    if (-not (Test-Path $csproj)) {
        throw "Project not found: $csproj"
    }
    Write-Host "  Building $modDir..."
    $buildResult = & dotnet build $csproj --verbosity minimal 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host $buildResult
        throw "Build failed for $modDir"
    }
    Write-Host "  Build succeeded."
}

# Validate args
if (-not $All -and -not $Mod) {
    Write-Error "Specify -Mod <modkey> or -All"
    exit 1
}
if ($All -and -not $Bump) {
    Write-Error "-All requires -Bump (major, minor, or patch)"
    exit 1
}

# Resolve mods to process
$modsToProcess = if ($All) { $ModMap.Keys } else { @($Mod) }

# Load versions.json
if (-not (Test-Path $VersionsPath)) {
    Write-Error "versions.json not found: $VersionsPath"
    exit 1
}
$versions = Get-Content $VersionsPath -Raw | ConvertFrom-Json

foreach ($modKey in $modsToProcess) {
    $info = $ModMap[$modKey]
    $jsonKey = $info[0]
    $modDir = $info[1]

    $currentVersion = $versions.PSObject.Properties[$jsonKey].value.version
    if (-not $currentVersion) {
        Write-Warning "No version for $modKey in versions.json, skipping"
        continue
    }

    $targetVersion = if ($Bump) {
        Bump-Version -Current $currentVersion -BumpType $Bump
    } else {
        $currentVersion
    }

    if ($Bump) {
        Write-Host "Bumping ${modDir}: $currentVersion -> $targetVersion ($Bump)"
    } else {
        Write-Host "Syncing ${modDir} to $targetVersion (no bump)"
    }

    Sync-AndBuild -ModKey $modKey -Version $targetVersion -UpdateVersionsJson ([bool]$Bump)
}

Write-Host "Done. DLL(s) in builds/ have correct version."
