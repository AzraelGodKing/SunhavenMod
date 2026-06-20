param([string]$JsonPath, [string]$Mod)
$ErrorActionPreference = "Stop"
$patch = Get-Content -Raw -Encoding UTF8 $JsonPath | ConvertFrom-Json
$targetPath = Join-Path $Mod "Localization\strings.json"
$json = Get-Content -Raw -Encoding UTF8 $targetPath | ConvertFrom-Json
$changed = $false
foreach ($lang in $patch.PSObject.Properties.Name) {
    foreach ($key in $patch.$lang.PSObject.Properties.Name) {
        $old = [string]$json.$key.$lang
        $new = [string]$patch.$lang.$key
        if ($old -cne $new) {
            $json.$key.$lang = $new
            $changed = $true
            Write-Host "$lang $key"
        }
    }
}
if ($changed) {
    ($json | ConvertTo-Json -Depth 6) | Set-Content -Path $targetPath -Encoding UTF8
    Write-Host "Saved $targetPath"
} else {
    Write-Host "No changes"
}
