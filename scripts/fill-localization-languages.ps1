# Expands Localization/strings.json to all 16 languages.
# Without -Translate: missing non-English values copy English.
# With -Translate: MyMemory API (free); -ForceRetranslate replaces values identical to English.
# Preserves {0} placeholders, rich text tags, and \n via token masking.
#
# Rate-limit friendly: processes one language at a time per mod and saves strings.json after
# each language pass. Use -Mod and -Language to run a single mod/language pair, e.g.:
#   .\scripts\fill-localization-languages.ps1 -Translate -ForceRetranslate -Mod HavensRespec -Language fr
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$Translate,
    [switch]$ForceRetranslate,
    [string]$Mod,
    [string]$Language,
    [int]$DelayMs = 0,
    [int]$DelayBetweenLanguagesMs = 5000,
    [int]$RateLimitBackoffSeconds = 60,
    [string]$CachePath = (Join-Path $PSScriptRoot ".translation-cache.json")
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$RequiredLangs = @(
    "en", "da", "de", "es", "fr", "it", "ja", "ko", "nl", "pt", "pt-BR", "ru", "sv", "zh-CN", "zh-TW", "uk"
)

# MyMemory langpair target codes (en|TARGET)
$MyMemoryTarget = @{
    "da" = "da"; "de" = "de"; "es" = "es"; "fr" = "fr"; "it" = "it"; "ja" = "ja"; "ko" = "ko"
    "nl" = "nl"; "pt" = "pt"; "pt-BR" = "pt-BR"; "ru" = "ru"; "sv" = "sv"
    "zh-CN" = "zh-CN"; "zh-TW" = "zh-TW"; "uk" = "uk"
}

$script:TranslateCache = @{}
if (Test-Path $CachePath) {
    try {
        $loaded = Get-Content -Raw -Encoding UTF8 $CachePath | ConvertFrom-Json
        foreach ($prop in $loaded.PSObject.Properties) {
            $script:TranslateCache[$prop.Name] = @{}
            foreach ($lp in $prop.Value.PSObject.Properties) {
                $script:TranslateCache[$prop.Name][$lp.Name] = [string]$lp.Value
            }
        }
        Write-Host "Loaded $($script:TranslateCache.Count) cached source strings from $CachePath"
    }
    catch {
        Write-Warning "Could not load cache: $($_.Exception.Message)"
    }
}

function Save-TranslateCache {
    $obj = @{}
    foreach ($src in ($script:TranslateCache.Keys | Sort-Object)) {
        $langs = @{}
        foreach ($lang in ($script:TranslateCache[$src].Keys | Sort-Object)) {
            $langs[$lang] = $script:TranslateCache[$src][$lang]
        }
        $obj[$src] = $langs
    }
    ($obj | ConvertTo-Json -Depth 4 -Compress:$false) | Set-Content -Path $CachePath -Encoding UTF8
}

function Protect-TranslationTokens {
    param([string]$Text)
    $tokens = [System.Collections.Generic.List[string]]::new()
    $i = 0
    $protected = $Text
    foreach ($m in [regex]::Matches($Text, '\{(\d+)\}')) {
        $tok = 'XPH{0}X' -f $i
        $protected = $protected.Replace($m.Value, $tok)
        $tokens.Add($m.Value) | Out-Null
        $i++
    }
    foreach ($m in [regex]::Matches($protected, '<(/?)[^>]+>')) {
        $tok = 'XRT{0}X' -f $i
        $protected = $protected.Replace($m.Value, $tok)
        $tokens.Add($m.Value) | Out-Null
        $i++
    }
    $parts = $protected -split '(\\n)'
    if ($parts.Count -gt 1) {
        $rebuilt = ''
        foreach ($part in $parts) {
            if ($part -eq '\n') {
                $tok = 'XNL{0}X' -f $i
                $rebuilt += $tok
                $tokens.Add('\n') | Out-Null
                $i++
            }
            else { $rebuilt += $part }
        }
        $protected = $rebuilt
    }
    return @{ Text = $protected; Tokens = $tokens }
}

function Restore-TranslationTokens {
    param([string]$Text, [System.Collections.Generic.List[string]]$Tokens)
    $result = $Text
    for ($i = 0; $i -lt $Tokens.Count; $i++) {
        $ph = "XPH${i}X"
        $rt = "XRT${i}X"
        $nl = "XNL${i}X"
        $tok = $Tokens[$i]
        if ($result.Contains($ph)) { $result = $result.Replace($ph, $tok) }
        elseif ($result.Contains($rt)) { $result = $result.Replace($rt, $tok) }
        elseif ($result.Contains($nl)) { $result = $result.Replace($nl, $tok) }
        else {
            # MyMemory may have altered token spacing; try regex fallback for placeholders
            $result = $result -replace "XPH$i\s*X", $tok
            $result = $result -replace "XRT$i\s*X", $tok
            $result = $result -replace "XNL$i\s*X", $tok
        }
    }
    return $result
}

function Invoke-MachineTranslate {
    param([string]$Text, [string]$LangCode)
    if ([string]::IsNullOrWhiteSpace($Text)) { return $Text }

    if ($script:TranslateCache.ContainsKey($Text) -and $script:TranslateCache[$Text].ContainsKey($LangCode)) {
        return $script:TranslateCache[$Text][$LangCode]
    }

    $target = $MyMemoryTarget[$LangCode]
    if (-not $target) { return $Text }

    $script:idx = 0
    $masked = Protect-TranslationTokens -Text $Text
    $q = [uri]::EscapeDataString($masked.Text)
    $pair = "en|$target"
    $uri = "https://api.mymemory.translated.net/get?q=$q&langpair=$pair"

    $translated = $null
    $maxAttempts = 5
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        try {
            $resp = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 45
            $candidate = [string]$resp.responseData.translatedText
            $isRateLimited = ($resp.responseStatus -eq 429) -or ($candidate -match 'MYMEMORY WARNING')
            if ($isRateLimited) {
                $waitSec = $RateLimitBackoffSeconds * $attempt
                Write-Warning "MyMemory rate limit ($LangCode), attempt $attempt/$maxAttempts - waiting ${waitSec}s..."
                Start-Sleep -Seconds $waitSec
                continue
            }
            if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                $translated = Restore-TranslationTokens -Text $candidate -Tokens $masked.Tokens
                break
            }
        }
        catch {
            $msg = $_.Exception.Message
            $is429 = $msg -match '429'
            $waitSec = if ($is429) { $RateLimitBackoffSeconds * $attempt } else { 3 * $attempt }
            Write-Warning "Translate failed ($LangCode) attempt ${attempt}/${maxAttempts}: $msg - waiting ${waitSec}s..."
            Start-Sleep -Seconds $waitSec
        }
    }

    if ([string]::IsNullOrWhiteSpace($translated)) {
        Write-Warning "No translation for [$LangCode]: $($Text.Substring(0, [Math]::Min(60, $Text.Length)))..."
        return $null
    }

    if (-not $script:TranslateCache.ContainsKey($Text)) {
        $script:TranslateCache[$Text] = @{}
    }
    $script:TranslateCache[$Text][$LangCode] = $translated
  return $translated
}

function Needs-Translation {
    param([string]$Existing, [string]$English)
    if ([string]::IsNullOrWhiteSpace($Existing)) { return $true }
    if ($Existing.Trim() -eq $English.Trim()) { return $true }
    if ($ForceRetranslate) { return $true }
    return $false
}

function Get-LanguageFallback {
    param([string]$LangCode, [string]$Key, $OutTable, [string]$English)
    if ($LangCode -eq "zh-TW") {
        $zhCn = [string]$OutTable[$Key]["zh-CN"]
        if (-not [string]::IsNullOrWhiteSpace($zhCn) -and $zhCn.Trim() -ne $English.Trim()) {
            return $zhCn
        }
    }
    return $null
}

$ModPaths = @(
    "SunhavenTodo", "BirthdayReminder", "CropOptimizer", "SenpaisChest", "TheVault",
    "HavensAlmanac", "SunHavenMuseumUtilityTracker", "HavenDevTools", "HavensRespec"
)

if ($DelayMs -le 0) {
    $DelayMs = if ($Translate) { 8000 } else { 0 }
}

$modsToProcess = if ([string]::IsNullOrWhiteSpace($Mod)) { $ModPaths } else { @($Mod.Trim()) }
foreach ($m in $modsToProcess) {
    if ($ModPaths -notcontains $m) {
        throw "Unknown mod '$m'. Valid: $($ModPaths -join ', ')"
    }
}

$targetLangs = @($RequiredLangs | Where-Object { $_ -ne "en" })
if (-not [string]::IsNullOrWhiteSpace($Language)) {
    $langNorm = $Language.Trim()
    if ($langNorm -eq "en") { throw "English is the source language; pick a target such as fr, de, ja." }
    $matched = $targetLangs | Where-Object { $_ -eq $langNorm }
    if (-not $matched) {
        $alias = @{ "pt-br" = "pt-BR"; "zh-cn" = "zh-CN"; "zh-tw" = "zh-TW" }
        if ($alias.ContainsKey($langNorm.ToLowerInvariant())) {
            $langNorm = $alias[$langNorm.ToLowerInvariant()]
            $matched = $targetLangs | Where-Object { $_ -eq $langNorm }
        }
    }
    if (-not $matched) { throw "Unknown language '$Language'. Valid: $($targetLangs -join ', ')" }
    $targetLangs = @($langNorm)
}

$stats = @{}
$apiCalls = 0

foreach ($modName in $modsToProcess) {
    $path = Join-Path (Join-Path $RepoRoot $modName) "Localization\strings.json"
    if (-not (Test-Path $path)) {
        Write-Warning "Skip $modName - missing strings.json"
        continue
    }

    Write-Host "Processing $modName ..."
    $root = Get-Content -Raw -Encoding UTF8 $path | ConvertFrom-Json
    $out = [ordered]@{}
    $modTranslated = 0

    foreach ($prop in $root.PSObject.Properties) {
        $en = [string]$prop.Value.en
        if ([string]::IsNullOrWhiteSpace($en)) { $en = [string]$prop.Value }
        $entry = [ordered]@{ en = $en }
        foreach ($lang in $RequiredLangs) {
            if ($lang -eq "en") { continue }
            $entry[$lang] = [string]$prop.Value.$lang
        }
        $out[$prop.Name] = $entry
    }

    foreach ($lang in $targetLangs) {
        Write-Host "  Language: $lang" -ForegroundColor Yellow
        $langUpdated = 0

        foreach ($prop in $root.PSObject.Properties) {
            $key = $prop.Name
            $en = [string]$out[$key].en
            $existing = [string]$out[$key][$lang]

            if (-not (Needs-Translation -Existing $existing -English $en)) {
                continue
            }

            if ($Translate) {
                $translated = Invoke-MachineTranslate -Text $en -LangCode $lang
                if ($null -eq $translated) {
                    $translated = Get-LanguageFallback -LangCode $lang -Key $key -OutTable $out -English $en
                }
                if ($null -eq $translated) { $translated = $en }
                $out[$key][$lang] = $translated
                if ($translated -ne $en) { $langUpdated++; $modTranslated++ }
                $apiCalls++
                if ($DelayMs -gt 0) { Start-Sleep -Milliseconds $DelayMs }
                if ($apiCalls % 25 -eq 0) { Save-TranslateCache }
            }
            else {
                $out[$key][$lang] = $en
                $langUpdated++
                $modTranslated++
            }
        }

        ($out | ConvertTo-Json -Depth 6) | Set-Content -Path $path -Encoding UTF8
        if ($Translate) { Save-TranslateCache }
        Write-Host "    -> $langUpdated cells updated for $lang" -ForegroundColor Cyan

        if ($targetLangs.Count -gt 1 -and $DelayBetweenLanguagesMs -gt 0) {
            Start-Sleep -Milliseconds $DelayBetweenLanguagesMs
        }
    }

    $stats[$modName] = $modTranslated
    Write-Host "  -> $($stats[$modName]) total non-English cells updated for $modName" -ForegroundColor Green
}

if ($Translate) { Save-TranslateCache }

Write-Host ""
Write-Host "Summary (non-English cells written per mod):"
foreach ($m in ($stats.Keys | Sort-Object)) {
    Write-Host "  $m : $($stats[$m])"
}
Write-Host "Languages processed: $($targetLangs -join ', ')"
Write-Host "API/cache lookups this run: $apiCalls"
Write-Host "Done."
