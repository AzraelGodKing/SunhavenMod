$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot '..\lib\RepoPaths.ps1')
$Repo = Get-RepoRoot -StartPath $PSScriptRoot
$Langs = Get-LocalizationLangs

function New-LangEntry([string]$English) {
    $entry = [ordered]@{}
    foreach ($lang in $Langs) { $entry[$lang] = $English }
    return [pscustomobject]$entry
}

function Add-ModKeys([string]$Mod, [hashtable]$KeyEnglish) {
    $path = Join-Path (Join-Path $Repo $Mod) "Localization\strings.json"
    $jsonText = [IO.File]::ReadAllText($path)
    $json = $jsonText | ConvertFrom-Json
    foreach ($pair in $KeyEnglish.GetEnumerator()) {
        $json | Add-Member -NotePropertyName $pair.Key -NotePropertyValue (New-LangEntry $pair.Value) -Force
    }
    $out = ($json | ConvertTo-Json -Depth 6) + [Environment]::NewLine
    [IO.File]::WriteAllText($path, $out, [Text.UTF8Encoding]::new($false))
    Write-Host "Updated $Mod (+$($KeyEnglish.Count) keys)"
}

Add-ModKeys "CropOptimizer" @{
    "crop.guess.no" = "no"
    "crop.guess.yes" = "yes"
    "crop.guess.yesValue" = "yes ({0})"
    "crop.tooltip.etaUnknown" = "<color={0}>ETA unknown - grow once to calibrate</color>"
    "crop.tooltip.manaInfused" = "<b>Mana infused</b>"
    "crop.tooltip.projectedGold" = "<b><color={0}>~{1:N0}g</color></b> at shop"
    "crop.tooltip.readyNow" = "<b><color={0}>Ready now</color></b>"
    "crop.tooltip.tile" = "<color={0}>Tile ({1}, {2})</color>"
    "crop.water.watered" = "<b><color=#8AD4FF>Watered</color></b>"
}

Add-ModKeys "TheVault" @{
    "vault.door.currency.blackbottlecap" = "Black Bottle Caps"
    "vault.door.currency.candycornpieces" = "Candy Corn Pieces"
    "vault.door.currency.communityTokens" = "Community Tokens"
    "vault.door.currency.doubloon" = "Doubloons"
    "vault.door.currency.keys" = "{0} Keys"
    "vault.door.currency.manashard" = "Mana Shards"
    "vault.door.currency.redcarnivalticket" = "Red Carnival Tickets"
    "vault.door.currency.seasonalTokens" = "{0} Tokens"
    "vault.door.locked" = "Locked: {0}"
    "vault.door.requires" = "Requires {0} {1}"
}
