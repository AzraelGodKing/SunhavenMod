# Runs fill-localization-languages.ps1 once per mod, once per target language.
# Saves after each language pass; pauses between runs to reduce MyMemory 429s.
param(
    [switch]$ForceRetranslate,
    [int]$DelayMs = 5000,
    [int]$PauseBetweenRunsSeconds = 30,
    [string]$StartMod,
    [string]$StartLanguage
)

$ErrorActionPreference = "Stop"
$fillScript = Join-Path $PSScriptRoot "fill-localization-languages.ps1"

$mods = @(
    "SunhavenTodo", "BirthdayReminder", "CropOptimizer", "SenpaisChest", "TheVault",
    "HavensAlmanac", "SunHavenMuseumUtilityTracker", "HavenDevTools", "HavensRespec"
)

$langs = @("da", "de", "es", "fr", "it", "ja", "ko", "nl", "pt", "pt-BR", "ru", "sv", "zh-CN", "zh-TW", "uk")

$started = [string]::IsNullOrWhiteSpace($StartMod) -and [string]::IsNullOrWhiteSpace($StartLanguage)
$run = 0
$total = $mods.Count * $langs.Count

foreach ($mod in $mods) {
    foreach ($lang in $langs) {
        if (-not $started) {
            if ($mod -eq $StartMod -and $lang -eq $StartLanguage) { $started = $true }
            else { continue }
        }

        $run++
        Write-Host ""
        Write-Host "========== [$run/$total] $mod / $lang ==========" -ForegroundColor Magenta

        # -ForceRetranslate only when explicitly passed; otherwise English placeholders are still filled.
        & $fillScript -Translate -ForceRetranslate:$ForceRetranslate.IsPresent -Mod $mod -Language $lang -DelayMs $DelayMs

        if ($run -lt $total -and $PauseBetweenRunsSeconds -gt 0) {
            Write-Host "Pausing ${PauseBetweenRunsSeconds}s before next mod/language..." -ForegroundColor DarkGray
            Start-Sleep -Seconds $PauseBetweenRunsSeconds
        }
    }
}

Write-Host ""
Write-Host "All mod/language passes finished." -ForegroundColor Green
