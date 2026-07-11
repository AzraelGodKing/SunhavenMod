param(
    [Parameter(Mandatory = $true)]
    [string]$ReviewPath,
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\lib\RepoPaths.ps1')

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Get-RepoRoot -StartPath $PSScriptRoot
}

$modNameToDir = @{
    'Birthday Reminder' = 'BirthdayReminder'
    'Crop Optimizer' = 'CropOptimizer'
    'Haven Dev Tools' = 'HavenDevTools'
    "Haven's Almanac" = 'HavensAlmanac'
    "Haven's Respec" = 'HavensRespec'
    'S.M.U.T.' = 'SunHavenMuseumUtilityTracker'
    "Senpai's Chest" = 'SenpaisChest'
    'Sun Haven Todo' = 'SunhavenTodo'
    'The Vault' = 'TheVault'
}

function Unescape-Cell([string]$s) {
    if ($null -eq $s) { return '' }
    $s = $s.Trim()
    $s = $s -replace '\\\|', '|'
    $s = $s -replace '<br>', "`n"
    return $s
}

function ConvertTo-JsonString([string]$s) {
    if ($null -eq $s) { return '""' }
    # Iterate Unicode chars — never UTF-8 bytes-as-chars (that double-encodes and produces mojibake).
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append('"')
    foreach ($ch in $s.ToCharArray()) {
        switch ($ch) {
            '"' { [void]$sb.Append('\"') }
            '\' { [void]$sb.Append('\\') }
            "`n" { [void]$sb.Append('\n') }
            "`r" { [void]$sb.Append('\r') }
            "`t" { [void]$sb.Append('\t') }
            default {
                $code = [int]$ch
                if ($code -lt 32) {
                    [void]$sb.AppendFormat('\u{0:x4}', $code)
                }
                else {
                    [void]$sb.Append($ch)
                }
            }
        }
    }
    [void]$sb.Append('"')
    return $sb.ToString()
}

$content = Get-Content -Raw -Encoding UTF8 $ReviewPath
$lines = $content -split "`r?`n"

$currentMod = $null
$updates = @{}

foreach ($line in $lines) {
    if ($line -match '^## (.+?) \(\d+ strings\)\s*$') {
        $sectionName = $Matches[1].Trim()
        if ($modNameToDir.ContainsKey($sectionName)) {
            $currentMod = $modNameToDir[$sectionName]
            if (-not $updates.ContainsKey($currentMod)) {
                $updates[$currentMod] = @{}
            }
        }
        else {
            Write-Warning "Unknown mod section: $sectionName"
            $currentMod = $null
        }
        continue
    }

    if ($null -eq $currentMod) { continue }
    if ($line -notmatch '^\|') { continue }
    if ($line -match '^\| ---') { continue }
    if ($line -match '^\| Key \|') { continue }

    if ($line -match '^\|\s*`([^`]+)`\s*\|\s*(.+?)\s*\|\s*(.+?)\s*\|$') {
        $key = $Matches[1].Trim()
        $ptBr = Unescape-Cell $Matches[3]
        $updates[$currentMod][$key] = $ptBr
    }
}

function Apply-StringsFile([string]$StringsPath, [hashtable]$KeyUpdates) {
    $fileLines = [System.Collections.Generic.List[string]]::new()
    $fileLines.AddRange([string[]](Get-Content -Encoding UTF8 $StringsPath))

    $currentKey = $null
    $updated = 0
    $missing = 0
    $found = @{}

    for ($i = 0; $i -lt $fileLines.Count; $i++) {
        $line = $fileLines[$i]

        if ($line -match '^\s*"([^"]+)":\s*\{') {
            $currentKey = $Matches[1]
            continue
        }

        if ($null -ne $currentKey -and $line -match '"pt-BR"' -and $KeyUpdates.ContainsKey($currentKey)) {
            $indent = if ($line -match '^(\s*)') { $Matches[1] } else { '' }
            $escaped = ConvertTo-JsonString $KeyUpdates[$currentKey]
            $fileLines[$i] = "$indent`"pt-BR`":  $escaped,"
            $updated++
            $found[$currentKey] = $true
            continue
        }

        if ($null -ne $currentKey -and $line -match '^\s*\},?\s*$') {
            $currentKey = $null
        }
    }

    foreach ($key in $KeyUpdates.Keys) {
        if (-not $found.ContainsKey($key)) {
            Write-Warning "[$([IO.Path]::GetFileName([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($StringsPath))))] Key not found: $key"
            $missing++
        }
    }

    if ($updated -gt 0) {
        [IO.File]::WriteAllLines($StringsPath, $fileLines, [System.Text.UTF8Encoding]::new($false))
    }

    return @{ Updated = $updated; Missing = $missing }
}

$totalUpdated = 0
$totalMissing = 0

foreach ($modDir in $updates.Keys | Sort-Object) {
    $stringsPath = Join-Path $RepoRoot (Join-Path $modDir 'Localization\strings.json')
    if (-not (Test-Path $stringsPath)) {
        Write-Warning "Missing strings.json for $modDir"
        continue
    }

    $result = Apply-StringsFile $stringsPath $updates[$modDir]
    $totalUpdated += $result.Updated
    $totalMissing += $result.Missing
    if ($result.Updated -gt 0) {
        Write-Host "[$modDir] Updated $($result.Updated) pt-BR strings"
    }
}

Write-Host "Done. Updated $totalUpdated strings; $totalMissing keys not found."
