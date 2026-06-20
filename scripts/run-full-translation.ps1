# Wrapper for long-running full localization translation (logs to translate-run.log).
$ErrorActionPreference = "Stop"
$log = Join-Path $PSScriptRoot "translate-run.log"
$start = Get-Date
"=== Translation started $($start.ToString('u')) ===" | Out-File -FilePath $log -Encoding UTF8

try {
    & (Join-Path $PSScriptRoot "translate-all-localization.ps1") `
        -ForceRetranslate `
        -DelayMs 5000 `
        -PauseBetweenRunsSeconds 20 `
        2>&1 | Tee-Object -FilePath $log -Append
    "=== Translation finished OK $(Get-Date -Format u) ===" | Out-File -FilePath $log -Append -Encoding UTF8
}
catch {
    "=== Translation FAILED: $($_.Exception.Message) $(Get-Date -Format u) ===" | Out-File -FilePath $log -Append -Encoding UTF8
    throw
}
