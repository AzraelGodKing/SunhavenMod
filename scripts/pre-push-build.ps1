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
    Then dotnet build for the mod's main .csproj (skipped when -SyncOnly).
    Use -BuildOnly with -Mod or -All to run builds only (no version edits).
    Use -SyncOnly with -Mod or -All to propagate versions without dotnet (for CI).

.PARAMETER Mod
    Mod key (e.g. senpaischest, havensbirthright). Required unless -All is used.

.PARAMETER Bump
    Bump type: major, minor, or patch. Omit for sync-only (single mod or -All). Incompatible with -BuildOnly.

.PARAMETER All
    Process all mods. With -Bump: bump semver then sync everywhere and build each.
    Without -Bump: sync every mod from current docs/versions.json (no JSON change) and build each.

.PARAMETER BuildOnly
    Skip all version file updates; only dotnet build. Use with -Mod or -All.

.PARAMETER SyncOnly
    Update plugin files, manifests, README, and docs from docs/versions.json but do not run dotnet build.

.EXAMPLE
    .\pre-push-build.ps1 -Mod senpaischest -Bump patch

.EXAMPLE
    .\pre-push-build.ps1 -Mod senpaischest
    (sync from versions.json, no bump - use after pulling a version-bump merge)

.EXAMPLE
    .\pre-push-build.ps1 -All -Bump patch
    (bump every mod in versions.json, sync all tracked files, build all)

.EXAMPLE
    .\pre-push-build.ps1 -All
    (sync all mods from versions.json, no bump, build all)

.EXAMPLE
    .\pre-push-build.ps1 -Mod senpaischest -BuildOnly
    .\pre-push-build.ps1 -All -BuildOnly
    (build only; does not read or require versions.json)

.EXAMPLE
    .\pre-push-build.ps1 -All -SyncOnly
    (GitHub Actions: align all version strings with versions.json; no game assemblies required)
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$Mod,

    [Parameter(Mandatory = $false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$Bump,

    [Parameter(Mandatory = $false)]
    [switch]$All,

    [Parameter(Mandatory = $false)]
    [switch]$BuildOnly,

    [Parameter(Mandatory = $false)]
    [switch]$SyncOnly
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
if (-not $ScriptRoot) { $ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $ScriptRoot ".."))
$ModMatrixPath = Join-Path $ScriptRoot "mod-matrix.json"
$VersionsPath = Join-Path $RepoRoot "docs\versions.json"
$ReadmePath = Join-Path $RepoRoot "README.md"
$IndexHtmlPath = Join-Path $RepoRoot "docs\index.html"
$DebugWindowPath = Join-Path $RepoRoot "HavenDevTools\UI\DebugWindow.cs"

function Get-ModDefinitions {
    param([string]$MatrixPath)
    if (-not (Test-Path $MatrixPath)) {
        throw "mod-matrix.json not found: $MatrixPath"
    }
    $rows = Get-Content $MatrixPath -Raw -Encoding utf8 | ConvertFrom-Json
    $defs = @{}
    foreach ($row in $rows) {
        if (-not $row.modKey) { continue }
        $defs[$row.modKey] = @{
            JsonKey = $row.jsonKey
            ModDir = $row.modDir
            PluginFile = $row.pluginFile
            ReadmePath = $row.readmePath
            IndexDataName = $row.indexDataName
            DocsHtml = if ($row.docsPagePath) { [string]$row.docsPagePath -replace '/', '\' } else { $null }
            ExtraCsproj = @($row.extraCsprojPaths)
        }
    }
    return $defs
}

$ModDefs = Get-ModDefinitions -MatrixPath $ModMatrixPath

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
    $content = $content -replace '"version_number"\s*:\s*"[^"]*"', "`"version_number`": `"$Version`""
    Set-Content $ManifestPath -Value $content -NoNewline -Encoding utf8
    Write-Host "  Updated thunderstore/manifest.json -> $Version"
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
        [object]$Def,
        [string]$Version,
        [bool]$UpdateVersionsJson,
        [bool]$SkipBuild
    )

    $jsonKey = $Def.JsonKey
    $modDir = $Def.ModDir
    $modPath = Join-Path $RepoRoot $modDir

    if ($UpdateVersionsJson) {
        Update-VersionsJson -JsonKey $jsonKey -Version $Version
    }

    $versionFile = if ($Def.PluginFile) { [string]$Def.PluginFile } else { "Plugin.cs" }
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
    if ($SkipBuild) {
        Write-Host "  Skipping dotnet build (-SyncOnly)."
    }
    else {
        Invoke-ModProjectBuild -Def $Def -ModPath $modPath -MainCsproj $mainCsproj
    }
}

function Invoke-ModProjectBuild {
    param(
        [object]$Def,
        [string]$ModPath,
        [string]$MainCsproj
    )
    $modDir = $Def.ModDir
    if (-not $MainCsproj) {
        $MainCsproj = Join-Path $ModPath "$modDir.csproj"
    }
    if (-not (Test-Path $MainCsproj)) {
        throw "Project not found: $MainCsproj"
    }
    Write-Host "  Building $modDir..."
    $buildResult = & dotnet build $MainCsproj --verbosity minimal 2>&1
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

if ($Mod -and -not $ModDefs.ContainsKey($Mod)) {
    $available = ($ModDefs.Keys | Sort-Object) -join ", "
    Write-Error "Unknown -Mod value '$Mod'. Available: $available"
    exit 1
}

if ($BuildOnly -and $Bump) {
    Write-Error "-BuildOnly cannot be used with -Bump"
    exit 1
}

if ($SyncOnly -and $BuildOnly) {
    Write-Error "-SyncOnly cannot be used with -BuildOnly"
    exit 1
}

if ($SyncOnly -and $Bump) {
    Write-Error "-SyncOnly cannot be used with -Bump (sync reads versions.json only)"
    exit 1
}

$modsToProcess = if ($All) { @($ModDefs.Keys | Sort-Object) } else { @($Mod) }

if ($BuildOnly) {
    foreach ($modKey in $modsToProcess) {
        $def = $ModDefs[$modKey]
        if (-not $def) { throw "Unknown mod: $modKey" }
        $modPath = Join-Path $RepoRoot $def.ModDir
        Write-Host "Build-only: $($def.ModDir)"
        Invoke-ModProjectBuild -Def $def -ModPath $modPath
    }
    Write-Host "Done. Build-only complete."
    exit 0
}

if (-not (Test-Path $VersionsPath)) {
    Write-Error "versions.json not found: $VersionsPath"
    exit 1
}
$versions = Get-Content $VersionsPath -Raw -Encoding utf8 | ConvertFrom-Json

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
        Bump-Version -Current $currentVersion -BumpType $Bump
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

    Sync-ModVersionEverywhere -Def $def -Version $targetVersion -UpdateVersionsJson ([bool]$Bump) -SkipBuild ([bool]$SyncOnly)
}

if ($SyncOnly) {
    Write-Host "Done. Version strings aligned; build skipped (-SyncOnly)."
}
else {
    Write-Host "Done. Version strings aligned; DLL(s) built."
}
