$ErrorActionPreference = "Stop"
$root = Join-Path $PSScriptRoot ".."
$mods = @(
    "SunhavenTodo", "BirthdayReminder", "CropOptimizer", "SenpaisChest", "TheVault",
    "HavensAlmanac", "SunHavenMuseumUtilityTracker", "HavenDevTools", "HavensRespec"
)

function Normalize-FormatTokens([string]$text) {
    $out = [regex]::Replace($text, 'XPH\s*(\d+)\s*X', '{$1}', 'IgnoreCase')
    $out = [regex]::Replace($out, 'XRT\s*(\d+)\s*X', '{$1}', 'IgnoreCase')
    $out = [regex]::Replace($out, '\{(\s*\d[^}]*)\}', {
        param($m)
        $inner = $m.Groups[1].Value -replace '[\s\u00a0\u2007\u202f]+', ''
        "{${inner}}"
    })
    return $out
}

$filesFixed = 0
foreach ($mod in $mods) {
    $path = Join-Path $root (Join-Path $mod "Localization\strings.json")
    if (-not (Test-Path $path)) { continue }

    $utf8 = [Text.UTF8Encoding]::new($false)
    $raw = [IO.File]::ReadAllText($path, $utf8)
    if (-not $raw.TrimStart().StartsWith('{')) {
        throw "Refusing to edit $path - missing opening brace."
    }

    $before = $raw
    $raw = Normalize-FormatTokens $raw

    if ($raw -ne $before) {
        [IO.File]::WriteAllText($path, $raw, [Text.UTF8Encoding]::new($false))
        try { $null = $raw | ConvertFrom-Json } catch {
            throw "JSON validation failed after editing ${mod}: $($_.Exception.Message)"
        }
        Write-Host "Fixed $mod"
        $filesFixed++
    }
}
Write-Host "Updated $filesFixed file(s)."
