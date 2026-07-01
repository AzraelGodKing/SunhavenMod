# Runs fill-localization-languages.ps1 once per mod, once per target language.
# Saves after each language pass; pauses between runs to reduce MyMemory 429s.
param(
    [switch]$ForceRetranslate,
    [int]$DelayMs = 5000,
    [int]$PauseBetweenRunsSeconds = 30,
    [string]$StartMod,
    [string]$StartLanguage,
    [string]$LogPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\lib\RepoPaths.ps1')

$fillScript = Join-Path $PSScriptRoot 'fill-localization-languages.ps1'
$mods = Get-LocalizedModDirs -StartPath $PSScriptRoot
$langs = Get-NonEnglishLangs

$started = [string]::IsNullOrWhiteSpace($StartMod) -and [string]::IsNullOrWhiteSpace($StartLanguage)
$run = 0
$total = $mods.Count * $langs.Count

function Write-LogLine {
    param([string]$Message, [ConsoleColor]$Color = [ConsoleColor]::Gray)
    Write-Host $Message -ForegroundColor $Color
    if (-not [string]::IsNullOrWhiteSpace($LogPath)) {
        Add-Content -Path $LogPath -Value $Message -Encoding UTF8
    }
}

if (-not [string]::IsNullOrWhiteSpace($LogPath)) {
    Write-LogLine "=== Translation started $(Get-Date -Format u) ==="
}

foreach ($mod in $mods) {
    foreach ($lang in $langs) {
        if (-not $started) {
            if ($mod -eq $StartMod -and $lang -eq $StartLanguage) { $started = $true }
            else { continue }
        }

        $run++
        Write-LogLine '' 
        Write-LogLine "========== [$run/$total] $mod / $lang ==========" ([ConsoleColor]::Magenta)

        & $fillScript -Translate -ForceRetranslate:$ForceRetranslate.IsPresent -Mod $mod -Language $lang -DelayMs $DelayMs

        if ($run -lt $total -and $PauseBetweenRunsSeconds -gt 0) {
            Write-LogLine "Pausing ${PauseBetweenRunsSeconds}s before next mod/language..."
            Start-Sleep -Seconds $PauseBetweenRunsSeconds
        }
    }
}

Write-LogLine '' 
Write-LogLine 'All mod/language passes finished.' ([ConsoleColor]::Green)
if (-not [string]::IsNullOrWhiteSpace($LogPath)) {
    Write-LogLine "=== Translation finished $(Get-Date -Format u) ==="
}
