$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\lib\RepoPaths.ps1')

$RepoRoot = Get-RepoRoot -StartPath $PSScriptRoot
$mods = Get-LocalizedModDirs -StartPath $PSScriptRoot -RepoRoot $RepoRoot

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
    $path = Join-Path (Join-Path $RepoRoot $mod) 'Localization\strings.json'
    if (-not (Test-Path $path)) { continue }

    $utf8 = [Text.UTF8Encoding]::new($false)
    $raw = [IO.File]::ReadAllText($path, $utf8)
    if (-not $raw.TrimStart().StartsWith('{')) {
        throw "Refusing to edit $path - missing opening brace."
    }

    $before = $raw
    $json = $raw | ConvertFrom-Json
    foreach ($keyProp in $json.PSObject.Properties) {
        foreach ($langProp in $keyProp.Value.PSObject.Properties) {
            $fixed = Normalize-FormatTokens ([string]$langProp.Value)
            if ($fixed -ne [string]$langProp.Value) {
                $keyProp.Value.$($langProp.Name) = $fixed
            }
        }
    }
    $after = ($json | ConvertTo-Json -Depth 8) + "`n"
    if ($after -ne $before) {
        [IO.File]::WriteAllText($path, $after, $utf8)
        Write-Host "Fixed format tokens: $mod"
        $filesFixed++
    }
}

Write-Host "Done. Updated $filesFixed file(s)."
