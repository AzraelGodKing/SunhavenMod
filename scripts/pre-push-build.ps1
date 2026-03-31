<#
.SYNOPSIS
    Pre-push bump and build for Sun Haven mods.

.DESCRIPTION
    Bumps or syncs version in every tracked location for one mod (or -All):
    - docs/versions.json
    - <Mod>/PluginInfo.cs or Plugin.cs (PLUGIN_VERSION)
    - <Mod>/thunderstore/manifest.json (version_number)
    - <Mod>/NexusMods-BBCode.txt (first [i](vX.Y.Z)[/i] line)
    - <Mod>/thunderstore/README.md (**Version X.Y.Z** line)
    - README.md mods table (pipe-separated version column for that mod)
    - docs/<mod-page>.html (version-badge span), when mapped
    - docs/index.html (mod card mod-version for data-name)
    - HavenDevTools/UI/DebugWindow.cs (_knownMods tuple for this GUID if present)
    - Optional: TheVault + TheVault.Abstractions .csproj; FasterRaces .csproj; any <Mod>.csproj
      that already contains a <Version> element
    Then dotnet build for the mod's main .csproj.

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
        "havendevtools", "havensalmanac", "fasterraces", "trinketfortune",
        "justiceforharold"
    )]
    [string]$Mod,

    [Parameter(Mandatory = $false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$Bump,

    [Parameter(Mandatory = $false)]
    [switch]$All
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
if (-not $ScriptRoot) { $ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $ScriptRoot ".."))
$VersionsPath = Join-Path $RepoRoot "docs\versions.json"
$ReadmePath = Join-Path $RepoRoot "README.md"
$IndexHtmlPath = Join-Path $RepoRoot "docs\index.html"
$DebugWindowPath = Join-Path $RepoRoot "HavenDevTools\UI\DebugWindow.cs"

# Per-mod: JsonKey, ModDir, UsePluginInfo (else Plugin.cs), ReadmePath segment e.g. SenpaisChest,
# IndexDataName (docs/index.html data-name), DocsHtml relative to docs\, ExtraCsproj paths relative repo root (optional)
$ModDefs = [ordered]@{
    "senpaischest"                 = @{ JsonKey = "com.azraelgodking.senpaischest"; ModDir = "SenpaisChest"; UsePluginInfo = $true; ReadmePath = "SenpaisChest"; IndexDataName = "Senpai's Chest"; DocsHtml = "SenpaisChest\SenpaisChest.html"; ExtraCsproj = @() }
    "havensbirthright"             = @{ JsonKey = "com.azraelgodking.havensbirthright"; ModDir = "HavensBirthright"; UsePluginInfo = $false; ReadmePath = "HavensBirthright"; IndexDataName = "Haven's Birthright"; DocsHtml = $null; ExtraCsproj = @() }
    "sunhavenmuseumutilitytracker" = @{ JsonKey = "com.azraelgodking.sunhavenmuseumutilitytracker"; ModDir = "SunHavenMuseumUtilityTracker"; UsePluginInfo = $false; ReadmePath = "SunHavenMuseumUtilityTracker"; IndexDataName = "S.M.U.T."; DocsHtml = "SMUT\SMUT.html"; ExtraCsproj = @() }
    "squirrelsbirthdayreminder"    = @{ JsonKey = "com.azraelgodking.squirrelsbirthdayreminder"; ModDir = "BirthdayReminder"; UsePluginInfo = $true; ReadmePath = "BirthdayReminder"; IndexDataName = "A Squirrel's Birthday Reminder"; DocsHtml = "BirthdayReminder\BirthdayReminder.html"; ExtraCsproj = @() }
    "sunhaventodo"                 = @{ JsonKey = "com.azraelgodking.sunhaventodo"; ModDir = "SunhavenTodo"; UsePluginInfo = $true; ReadmePath = "SunhavenTodo"; IndexDataName = "Sun Haven Todo"; DocsHtml = "Todo\todo.html"; ExtraCsproj = @() }
    "thevault"                     = @{ JsonKey = "com.azraelgodking.thevault"; ModDir = "TheVault"; UsePluginInfo = $false; ReadmePath = "TheVault"; IndexDataName = "The Vault"; DocsHtml = "TheVault\TheVault.html"; ExtraCsproj = @("TheVault\TheVault.csproj", "TheVault.Abstractions\TheVault.Abstractions.csproj") }
    "havendevtools"                = @{ JsonKey = "com.azraelgodking.havendevtools"; ModDir = "HavenDevTools"; UsePluginInfo = $false; ReadmePath = "HavenDevTools"; IndexDataName = "HavenDevTools"; DocsHtml = "HavenDevTools\HavenDevTools.html"; ExtraCsproj = @() }
    "havensalmanac"                = @{ JsonKey = "com.azraelgodking.havensalmanac"; ModDir = "HavensAlmanac"; UsePluginInfo = $true; ReadmePath = "HavensAlmanac"; IndexDataName = "Haven's Almanac"; DocsHtml = "HavensAlmanac\HavensAlmanac.html"; ExtraCsproj = @() }
    "fasterraces"                  = @{ JsonKey = "com.azraelgodking.fasterraces"; ModDir = "FasterRaces"; UsePluginInfo = $false; ReadmePath = "FasterRaces"; IndexDataName = "Faster Races"; DocsHtml = "FasterRaces\FasterRaces.html"; ExtraCsproj = @("FasterRaces\FasterRaces.csproj") }
    "trinketfortune"               = @{ JsonKey = "com.azraelgodking.trinketfortune"; ModDir = "TrinketFortune"; UsePluginInfo = $false; ReadmePath = "TrinketFortune"; IndexDataName = "Trinket Fortune"; DocsHtml = "TrinketFortune\TrinketFortune.html"; ExtraCsproj = @() }
    "justiceforharold"             = @{ JsonKey = "com.azraelgodking.justiceforharold"; ModDir = "JusticeForHarold"; UsePluginInfo = $true; ReadmePath = $null; IndexDataName = $null; DocsHtml = $null; ExtraCsproj = @() }
}

function Get-VersionParts {
    param([string]$Version)
    $parts = $Version -split '\.'
    $major = [int]($parts[0])
    $minor = if ($parts.Count -ge 2) { [int]$parts[1] } else { 0 }
    $patch = if ($parts.Count -ge 3) { [int]$parts[2] } else { 0 }
    return @{ Major = $major; Minor = $minor; Patch = $patch }
}

function Update-Version {
    param([string]$Current, [string]$BumpType)
    $p = Get-VersionParts $Current
    switch ($BumpType) {
        "major" { return "$($p.Major + 1).0.0" }
        "minor" { return "$($p.Major).$($p.Minor + 1).0" }
        "patch" { return "$($p.Major).$($p.Minor).$($p.Patch + 1)" }
        default { throw "Unknown bump type: $BumpType" }
    }
}

function Get-AssemblyFileVersion {
    param([string]$SemanticVersion)
    # Three-part X.Y.Z -> X.Y.Z.0 for AssemblyVersion / FileVersion
    if ($SemanticVersion -match '^(\d+)\.(\d+)\.(\d+)$') {
        return "$SemanticVersion.0"
    }
    if ($SemanticVersion -match '^(\d+\.\d+\.\d+\.\d+)$') {
        return $SemanticVersion
    }
    return $SemanticVersion
}

function Update-VersionsJson {
    param([string]$JsonKey, [string]$Version)
    $content = Get-Content $VersionsPath -Raw -Encoding utf8
    $pattern = "(`"$([regex]::Escape($JsonKey))`":\s*\{\s*`"version`":\s*)`"[^`"]*`""
    $replacement = "`${1}`"$Version`""
    if ($content -match $pattern) {
        $content = $content -replace $pattern, $replacement
        Set-Content $VersionsPath -Value $content -NoNewline -Encoding utf8
        Write-Host "  Updated versions.json -> $Version"
    }
    else {
        Write-Warning "  Could not find $JsonKey in versions.json"
    }
}

function Update-PluginVersionConst {
    param([string]$FilePath, [string]$Version)
    $FilePath = [System.IO.Path]::GetFullPath($FilePath)
    if (-not (Test-Path $FilePath)) {
        Write-Warning "Version file not found: $FilePath"
        return
    }
    $content = [System.IO.File]::ReadAllText($FilePath)
    if ($content -notmatch 'PLUGIN_VERSION\s*=\s*"[^"]*"') {
        Write-Warning "  PLUGIN_VERSION pattern not found in $(Split-Path $FilePath -Leaf)"
        return
    }
    $newContent = $content -replace 'PLUGIN_VERSION\s*=\s*"[^"]*"', "PLUGIN_VERSION = `"$Version`""
    if ($newContent -ne $content) {
        [System.IO.File]::WriteAllText($FilePath, $newContent)
        Write-Host "  Updated $(Split-Path $FilePath -Leaf) PLUGIN_VERSION -> $Version"
    }
    else {
        Write-Host "  $(Split-Path $FilePath -Leaf) PLUGIN_VERSION already $Version"
    }
}

function Update-ThunderstoreManifest {
    param([string]$ManifestPath, [string]$Version)
    if (-not (Test-Path $ManifestPath)) {
        Write-Warning "  manifest.json not found: $ManifestPath"
        return
    }
    $content = Get-Content $ManifestPath -Raw -Encoding utf8
    $updated = $content -replace '"version_number"\s*:\s*"[^"]*"', "`"version_number`": `"$Version`""
    if ($updated -ne $content) {
        Set-Content $ManifestPath -Value $updated -NoNewline -Encoding utf8
        Write-Host "  Updated thunderstore/manifest.json -> $Version"
    }
    else {
        Write-Host "  thunderstore/manifest.json already $Version"
    }
}

function Update-RootReadmeTable {
    param([string]$ReadmeSegment, [string]$Version)
    if (-not $ReadmeSegment) { return }
    if (-not (Test-Path $ReadmePath)) {
        Write-Warning "README.md not found"
        return
    }
    $needle = "]($ReadmeSegment/)"
    $lines = Get-Content $ReadmePath -Encoding utf8
    $foundRow = $false
    $wrote = $false
    $verEnd = '\|\s*' + [regex]::Escape($Version) + '\s*\|\s*$'
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Contains($needle)) {
            $foundRow = $true
            $oldLine = $lines[$i]
            $lines[$i] = $oldLine -replace '\|\s*[\d\.]+\s*\|\s*$', "| $Version |"
            if ($lines[$i] -ne $oldLine) { $wrote = $true }
            elseif ($oldLine -match $verEnd) {
                Write-Host "  Root README.md table already $Version"
            }
        }
    }
    if ($wrote) {
        Set-Content $ReadmePath -Value ($lines -join "`r`n") -NoNewline -Encoding utf8
        Write-Host "  Updated root README.md table -> $Version"
    }
    elseif (-not $foundRow) {
        Write-Warning "  README row not found for $needle"
    }
}

function Update-NexusBbcodeHeader {
    param([string]$NexusPath, [string]$Version)
    if (-not (Test-Path $NexusPath)) {
        Write-Warning "  NexusMods-BBCode.txt not found: $NexusPath"
        return
    }
    $c = [System.IO.File]::ReadAllText($NexusPath)
    if ($c -notmatch '\[i\]\(v[\d\.]+\)\[/i\]') {
        Write-Warning "  NexusMods-BBCode.txt: no [i](v...) header to update"
        return
    }
    $once = [regex]::Replace($c, '\[i\]\(v[\d\.]+\)\[/i\]', "[i](v$Version)[/i]", 1)
    if ($once -ne $c) {
        [System.IO.File]::WriteAllText($NexusPath, $once)
        Write-Host "  Updated NexusMods-BBCode.txt header -> v$Version"
    }
    else {
        Write-Host "  NexusMods-BBCode.txt header already v$Version"
    }
}

function Update-ThunderstoreReadmeVersionLine {
    param([string]$TsReadmePath, [string]$Version)
    if (-not (Test-Path $TsReadmePath)) {
        Write-Warning "  thunderstore/README.md not found: $TsReadmePath"
        return
    }
    $c = [System.IO.File]::ReadAllText($TsReadmePath)
    if ($c -notmatch '\*\*Version [\d\.]+\*\*') {
        Write-Warning "  thunderstore/README.md: no **Version X** line found"
        return
    }
    $new = [regex]::Replace($c, '\*\*Version [\d\.]+\*\*', "**Version $Version**", 1)
    if ($new -ne $c) {
        [System.IO.File]::WriteAllText($TsReadmePath, $new)
        Write-Host "  Updated thunderstore/README.md Version line -> $Version"
    }
    else {
        Write-Host "  thunderstore/README.md Version line already $Version"
    }
}

function Update-DocsHtmlBadge {
    param([string]$RelativeUnderDocs, [string]$Version)
    if (-not $RelativeUnderDocs) { return }
    $htmlPath = Join-Path (Join-Path $RepoRoot "docs") $RelativeUnderDocs
    if (-not (Test-Path $htmlPath)) {
        Write-Warning "  docs HTML not found: $htmlPath"
        return
    }
    $c = [System.IO.File]::ReadAllText($htmlPath)
    if ($c -notmatch '<span class="version-badge">v[\d\.]+</span>') {
        Write-Warning "  docs/${RelativeUnderDocs}: no version-badge span"
        return
    }
    $new = [regex]::Replace($c, '<span class="version-badge">v[\d\.]+</span>', "<span class=`"version-badge`">v$Version</span>", 1)
    if ($new -ne $c) {
        [System.IO.File]::WriteAllText($htmlPath, $new)
        Write-Host "  Updated docs/${RelativeUnderDocs} badge -> v$Version"
    }
    else {
        Write-Host "  docs/${RelativeUnderDocs} badge already v$Version"
    }
}

function Update-IndexHtmlModCard {
    param([string]$DataName, [string]$Version)
    if (-not $DataName) { return }
    if (-not (Test-Path $IndexHtmlPath)) {
        Write-Warning "  docs/index.html not found"
        return
    }
    $c = [System.IO.File]::ReadAllText($IndexHtmlPath)
    $esc = [regex]::Escape($DataName)
    $pattern = "(?s)(data-name=`"$esc`".*?<span class=`"mod-version`">)v[\d\.]+(</span>)"
    if ($c -notmatch $pattern) {
        Write-Warning "  docs/index.html: no mod-version for data-name=`"$DataName`""
        return
    }
    $new = [regex]::Replace($c, $pattern, "`${1}v$Version`${2}", 1)
    if ($new -ne $c) {
        [System.IO.File]::WriteAllText($IndexHtmlPath, $new)
        Write-Host "  Updated docs/index.html ($DataName) -> v$Version"
    }
    else {
        Write-Host "  docs/index.html ($DataName) already v$Version"
    }
}

function Update-DebugWindowKnownMod {
    param([string]$JsonKey, [string]$Version)
    if (-not (Test-Path $DebugWindowPath)) {
        Write-Warning "  DebugWindow.cs not found"
        return
    }
    $c = [System.IO.File]::ReadAllText($DebugWindowPath)
    $keyEsc = [regex]::Escape($JsonKey)
    $pattern = "(\(\s*`"$keyEsc`"\s*,\s*`"[^`"]+`"\s*,\s*)`"[\d\.]+`"\s*\)\s*,"
    if ($c -notmatch $pattern) {
        Write-Warning "  DebugWindow.cs: no _knownMods tuple for $JsonKey (add manually if needed)"
        return
    }
    $new = [regex]::Replace($c, $pattern, "`${1}`"$Version`"),", 1)
    if ($new -ne $c) {
        [System.IO.File]::WriteAllText($DebugWindowPath, $new)
        Write-Host "  Updated HavenDevTools DebugWindow.cs ($JsonKey) -> $Version"
    }
    else {
        Write-Host "  HavenDevTools DebugWindow.cs ($JsonKey) already $Version"
    }
}

function Update-CsprojVersions {
    param([string]$CsprojPath, [string]$Version)
    if (-not (Test-Path $CsprojPath)) {
        Write-Warning "  csproj not found: $CsprojPath"
        return
    }
    $c = Get-Content $CsprojPath -Raw -Encoding utf8
    $orig = $c
    $four = Get-AssemblyFileVersion $Version
    if ($c -match '<AssemblyVersion>') {
        $c = $c -replace '<Version>[^<]*</Version>', "<Version>$Version</Version>"
        $c = $c -replace '<AssemblyVersion>[^<]*</AssemblyVersion>', "<AssemblyVersion>$four</AssemblyVersion>"
        $c = $c -replace '<FileVersion>[^<]*</FileVersion>', "<FileVersion>$four</FileVersion>"
        $c = $c -replace '<InformationalVersion>[^<]*</InformationalVersion>', "<InformationalVersion>$Version</InformationalVersion>"
    }
    else {
        $c = $c -replace '<Version>[^<]*</Version>', "<Version>$Version</Version>"
    }
    if ($c -ne $orig) {
        Set-Content $CsprojPath -Value $c -NoNewline -Encoding utf8
        Write-Host "  Updated $(Split-Path $CsprojPath -Leaf) -> $Version"
    }
}

function Sync-ModVersionEverywhere {
    param(
        [hashtable]$Def,
        [string]$Version,
        [bool]$UpdateVersionsJson
    )

    $jsonKey = $Def.JsonKey
    $modDir = $Def.ModDir
    $usePluginInfo = $Def.UsePluginInfo
    $modPath = Join-Path $RepoRoot $modDir

    if ($UpdateVersionsJson) {
        Update-VersionsJson -JsonKey $jsonKey -Version $Version
    }

    $versionFile = if ($usePluginInfo) { "PluginInfo.cs" } else { "Plugin.cs" }
    Update-PluginVersionConst -FilePath (Join-Path $modPath $versionFile) -Version $Version

    Update-ThunderstoreManifest -ManifestPath (Join-Path $modPath "thunderstore\manifest.json") -Version $Version

    Update-RootReadmeTable -ReadmeSegment $Def.ReadmePath -Version $Version

    Update-NexusBbcodeHeader -NexusPath (Join-Path $modPath "NexusMods-BBCode.txt") -Version $Version

    Update-ThunderstoreReadmeVersionLine -TsReadmePath (Join-Path $modPath "thunderstore\README.md") -Version $Version

    Update-DocsHtmlBadge -RelativeUnderDocs $Def.DocsHtml -Version $Version

    Update-IndexHtmlModCard -DataName $Def.IndexDataName -Version $Version

    Update-DebugWindowKnownMod -JsonKey $jsonKey -Version $Version

    foreach ($rel in $Def.ExtraCsproj) {
        Update-CsprojVersions -CsprojPath (Join-Path $RepoRoot $rel) -Version $Version
    }

    # Main mod csproj (when it carries Version / AssemblyVersion, e.g. TheVault already in ExtraCsproj;
    # FasterRaces uses ExtraCsproj; others often omit — still try ModDir.csproj if file contains <Version>)
    $mainCsproj = Join-Path $modPath "$modDir.csproj"
    if ((Test-Path $mainCsproj) -and ($Def.ExtraCsproj -notcontains "$modDir\$modDir.csproj")) {
        $raw = Get-Content $mainCsproj -Raw -Encoding utf8
        if ($raw -match '<Version>\s*[\d\.]+\s*</Version>') {
            Update-CsprojVersions -CsprojPath $mainCsproj -Version $Version
        }
    }

    if (-not (Test-Path $mainCsproj)) {
        throw "Project not found: $mainCsproj"
    }
    Write-Host "  Building $modDir..."
    $buildResult = & dotnet build $mainCsproj --verbosity minimal 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host $buildResult
        throw "Build failed for $modDir"
    }
    Write-Host "  Build succeeded."
}

# --- main ---

if (-not $All -and -not $Mod) {
    Write-Error "Specify -Mod <modkey> or -All"
    exit 1
}
if ($All -and -not $Bump) {
    Write-Error "-All requires -Bump (major, minor, or patch)"
    exit 1
}

if (-not (Test-Path $VersionsPath)) {
    Write-Error "versions.json not found: $VersionsPath"
    exit 1
}
$versions = Get-Content $VersionsPath -Raw -Encoding utf8 | ConvertFrom-Json

$modsToProcess = if ($All) { @($ModDefs.Keys) } else { @($Mod) }

foreach ($modKey in $modsToProcess) {
    $def = $ModDefs[$modKey]
    if (-not $def) { throw "Unknown mod: $modKey" }

    $jsonKey = $def.JsonKey
    $modDir = $def.ModDir
    $prop = $versions.PSObject.Properties[$jsonKey]
    if (-not $prop -or -not $prop.Value.version) {
        Write-Warning "No version for $modKey ($jsonKey) in versions.json, skipping"
        continue
    }

    $currentVersion = [string]$prop.Value.version
    $targetVersion = if ($Bump) {
        Update-Version -Current $currentVersion -BumpType $Bump
    }
    else {
        $currentVersion
    }

    if ($Bump) {
        Write-Host "Bumping ${modDir}: $currentVersion -> $targetVersion ($Bump)"
    }
    else {
        Write-Host "Syncing ${modDir} to $targetVersion (no bump)"
    }

    Sync-ModVersionEverywhere -Def $def -Version $targetVersion -UpdateVersionsJson ([bool]$Bump)
}

Write-Host "Done. Version strings aligned; DLL(s) built."
