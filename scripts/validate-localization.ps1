# Validates Localization/strings.json under each mod: all keys have en + all 16 game languages.
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$WarnLongStrings
)

$ErrorActionPreference = "Stop"

$RequiredLangs = @(
    "en", "da", "de", "es", "fr", "it", "ja", "ko", "nl", "pt", "pt-BR", "ru", "sv", "zh-CN", "zh-TW", "uk"
)

$ModPaths = @(
    "SunhavenTodo",
    "BirthdayReminder",
    "CropOptimizer",
    "SenpaisChest",
    "TheVault",
    "HavensAlmanac",
    "SunHavenMuseumUtilityTracker",
    "HavenDevTools",
    "HavensRespec"
)

$exitCode = 0
$issues = New-Object System.Collections.Generic.List[string]

function Test-StringsFile {
    param([string]$Path, [string]$ModName)

    if (-not (Test-Path $Path)) {
        $script:issues.Add("$ModName : missing $Path")
        $script:exitCode = 1
        return
    }

    try {
        $json = Get-Content -Raw -Encoding UTF8 $Path | ConvertFrom-Json
    }
    catch {
        $script:issues.Add("$ModName : invalid JSON - $($_.Exception.Message)")
        $script:exitCode = 1
        return
    }

    $keys = @($json.PSObject.Properties.Name)
    if ($keys.Count -eq 0) {
        $script:issues.Add("$ModName : no localization keys")
        $script:exitCode = 1
        return
    }

    $seen = @{}
    foreach ($key in $keys) {
        if ($seen.ContainsKey($key)) {
            $script:issues.Add("$ModName : duplicate key '$key'")
            $script:exitCode = 1
        }
        $seen[$key] = $true

        $entry = $json.$key
        if ($null -eq $entry) {
            $script:issues.Add("$ModName : key '$key' is null")
            $script:exitCode = 1
            continue
        }

        $en = $entry.en
        if ([string]::IsNullOrWhiteSpace($en)) {
            $script:issues.Add("$ModName : key '$key' missing or empty 'en'")
            $script:exitCode = 1
        }

        foreach ($lang in $RequiredLangs) {
            $val = $entry.$lang
            if ([string]::IsNullOrWhiteSpace($val)) {
                $script:issues.Add("$ModName : key '$key' missing language '$lang'")
                $script:exitCode = 1
            }
        }

        if ($WarnLongStrings -and -not [string]::IsNullOrWhiteSpace($en)) {
            foreach ($lang in $RequiredLangs) {
                if ($lang -eq "en") { continue }
                $val = [string]$entry.$lang
                if ($val.Length -gt ($en.Length * 2.5)) {
                    Write-Warning "$ModName : key '$key' lang '$lang' is much longer than English ($($val.Length) vs $($en.Length))"
                }
            }
        }
    }

    Write-Host "OK $ModName : $($keys.Count) keys" -ForegroundColor Green
}

Write-Host "Validating localization under $RepoRoot"
foreach ($mod in $ModPaths) {
    $file = Join-Path (Join-Path $RepoRoot $mod) "Localization\strings.json"
    Test-StringsFile -Path $file -ModName $mod
}

if ($issues.Count -gt 0) {
    Write-Host ""
    Write-Host "Failures:" -ForegroundColor Red
    foreach ($i in $issues) { Write-Host "  $i" -ForegroundColor Red }
}

exit $exitCode
