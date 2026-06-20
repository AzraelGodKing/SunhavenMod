$ErrorActionPreference = "Stop"
$root = Join-Path $PSScriptRoot ".."
$mods = @(
    "SunhavenTodo", "BirthdayReminder", "CropOptimizer", "SenpaisChest", "TheVault",
    "HavensAlmanac", "SunHavenMuseumUtilityTracker", "HavenDevTools", "HavensRespec"
)

function Fix-FormatTokensInString([string]$s) {
    if ([string]::IsNullOrEmpty($s)) { return $s }
    $out = [regex]::Replace($s, 'XPH\s*(\d+)\s*X', '{$1}', 'IgnoreCase')
    $out = [regex]::Replace($out, 'XRT\s*(\d+)\s*X', '{$1}', 'IgnoreCase')
    # Only normalize brace groups that look like numeric format placeholders.
    $out = [regex]::Replace($out, '\{(\s*\d[^}]*)\}', {
        param($m)
        $inner = $m.Groups[1].Value -replace '[\s\u00a0\u2007\u202f]+', ''
        "{${inner}}"
    })
    return $out
}

function Fix-LocalizationLine([string]$line) {
    if ($line -notmatch ':\s*"') { return $line }
    return [regex]::Replace($line, ':\s*"((?:\\.|[^"\\])*)"\s*(,?\s*)$', {
        param($m)
        $value = $m.Groups[1].Value
        $suffix = $m.Groups[2].Value
        $fixed = Fix-FormatTokensInString $value
        if ($fixed -eq $value) { return $m.Value }
        ': "' + $fixed + '"' + $suffix
    })
}

$filesFixed = 0
foreach ($mod in $mods) {
    $path = Join-Path $root (Join-Path $mod "Localization\strings.json")
    if (-not (Test-Path $path)) { continue }

    $raw = [IO.File]::ReadAllText($path)
    if ($raw.StartsWith([char]0xFEFF)) { $raw = $raw.Substring(1) }
    $lines = $raw -split "`r?`n", -1
    $changed = $false
    $out = foreach ($line in $lines) {
        $newLine = Fix-LocalizationLine $line
        if ($newLine -ne $line) { $changed = $true }
        $newLine
    }

    if ($changed) {
        [IO.File]::WriteAllText($path, ($out -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
        Write-Host "Fixed $mod"
        $filesFixed++
    }
}
Write-Host "Updated $filesFixed file(s)."
