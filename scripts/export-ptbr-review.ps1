$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$OutPath = Join-Path $RepoRoot "pt-BR-translation-review.md"

$mods = @(
    @{ Name = "Birthday Reminder"; Path = "BirthdayReminder/Localization/strings.json" },
    @{ Name = "Crop Optimizer"; Path = "CropOptimizer/Localization/strings.json" },
    @{ Name = "Haven Dev Tools"; Path = "HavenDevTools/Localization/strings.json" },
    @{ Name = "Haven's Almanac"; Path = "HavensAlmanac/Localization/strings.json" },
    @{ Name = "Haven's Respec"; Path = "HavensRespec/Localization/strings.json" },
    @{ Name = "S.M.U.T."; Path = "SunHavenMuseumUtilityTracker/Localization/strings.json" },
    @{ Name = "Senpai's Chest"; Path = "SenpaisChest/Localization/strings.json" },
    @{ Name = "Sun Haven Todo"; Path = "SunhavenTodo/Localization/strings.json" },
    @{ Name = "The Vault"; Path = "TheVault/Localization/strings.json" }
)

function Format-Cell([string]$s) {
    if ($null -eq $s) { return "" }
    $s = $s -replace "`r`n", "<br>"
    $s = $s -replace "`n", "<br>"
    $s = $s -replace "\|", "\|"
    return $s
}

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# Portuguese (Brazil) translation review")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("Generated $(Get-Date -Format 'yyyy-MM-dd'). For each key the **English** source is shown next to the current **pt-BR** translation. Edit the pt-BR column where corrections are needed; placeholders like ``{0}``, ``<color=...>``, ``<b>`` must stay intact.")
[void]$sb.AppendLine("")

$total = 0
foreach ($mod in $mods) {
    $full = Join-Path $RepoRoot $mod.Path
    if (-not (Test-Path $full)) { Write-Warning "Missing: $($mod.Path)"; continue }
    $json = Get-Content $full -Raw -Encoding utf8 | ConvertFrom-Json
    $count = 0

    $rows = [System.Text.StringBuilder]::new()
    foreach ($prop in $json.PSObject.Properties) {
        $entry = $prop.Value
        $en = $entry.PSObject.Properties["en"]
        $ptbr = $entry.PSObject.Properties["pt-BR"]
        if (-not $en -and -not $ptbr) { continue }
        $enVal = if ($en) { [string]$en.Value } else { "" }
        $ptVal = if ($ptbr) { [string]$ptbr.Value } else { "" }
        [void]$rows.AppendLine("| ``$($prop.Name)`` | $(Format-Cell $enVal) | $(Format-Cell $ptVal) |")
        $count++
    }

    [void]$sb.AppendLine("## $($mod.Name) ($count strings)")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("| Key | English | Portuguese (Brazil) |")
    [void]$sb.AppendLine("| --- | --- | --- |")
    [void]$sb.Append($rows.ToString())
    [void]$sb.AppendLine("")
    $total += $count
    Write-Host "  $($mod.Name): $count strings"
}

[void]$sb.Insert(0, "")
Set-Content -Path $OutPath -Value $sb.ToString() -Encoding utf8
Write-Host "Wrote $total strings to $OutPath"
