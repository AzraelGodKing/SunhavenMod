# Resolves a working Python 3 executable on Windows (local dev + self-hosted runners).
# Dot-source: . (Join-Path $PSScriptRoot '..\lib\Resolve-Python.ps1')

function Test-PythonExecutable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Exe
    )

    if ([string]::IsNullOrWhiteSpace($Exe)) { return $false }
    if (-not (Test-Path -LiteralPath $Exe)) { return $false }

    try {
        $versionLine = & $Exe -c "import sys; print(sys.version_info[0])" 2>$null | Select-Object -First 1
        return ([string]$versionLine).Trim() -eq '3'
    }
    catch {
        return $false
    }
}

function Get-PyLauncherCandidates {
    $pyCmd = Get-Command py -ErrorAction SilentlyContinue
    if (-not $pyCmd) { return @() }

    $lines = & $pyCmd.Source -0p 2>$null
    if (-not $lines) { return @() }

    $paths = New-Object System.Collections.Generic.List[string]
    foreach ($line in $lines) {
        $text = [string]$line
        if ($text -match '\s+(?<path>[A-Za-z]:\\.+)$') {
            $paths.Add($Matches.path.Trim())
        }
    }
    return $paths
}

function Get-ProgramFilesPythonCandidates {
    $roots = @(
        Join-Path $env:LOCALAPPDATA 'Programs\Python'
        Join-Path ${env:ProgramFiles} 'Python'
        Join-Path ${env:ProgramFiles(x86)} 'Python'
    )

    $paths = New-Object System.Collections.Generic.List[string]
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) { continue }
        Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue |
            Sort-Object {
                if ($_.Name -match 'Python(\d+)') { [int]$Matches[1] } else { 0 }
            } -Descending |
            ForEach-Object {
                $exe = Join-Path $_.FullName 'python.exe'
                if (Test-Path $exe) { $paths.Add($exe) }
            }
    }
    return $paths
}

function Get-PythonExecutable {
    <#
    .SYNOPSIS
        Returns the path to a working Python 3 interpreter.

    .DESCRIPTION
        Resolution order:
        1. $env:PYTHON or $env:PYTHON3 (if executable and Python 3)
        2. On GitHub Actions: python3, then python on PATH (setup-python / tool cache on runner)
        3. Standard install folders (LocalAppData/Programs/Python, Program Files)
        4. py -0p registered installs (skips broken/missing entries)
        5. python3 / python on PATH (validated — ignores Windows Store stubs)

        Override locally when py launcher points at a stale self-hosted tool cache:
          $env:PYTHON = 'C:\Users\you\AppData\Local\Programs\Python\Python311\python.exe'
    #>
    [CmdletBinding()]
    param()

    $candidates = New-Object System.Collections.Generic.List[string]

    foreach ($name in @('PYTHON', 'PYTHON3')) {
        $fromEnv = [Environment]::GetEnvironmentVariable($name)
        if (-not [string]::IsNullOrWhiteSpace($fromEnv)) {
            $candidates.Add($fromEnv.Trim())
        }
    }

    if ($env:GITHUB_ACTIONS -eq 'true') {
        foreach ($cmdName in @('python3', 'python')) {
            $cmd = Get-Command $cmdName -ErrorAction SilentlyContinue
            if ($cmd) { $candidates.Add($cmd.Source) }
        }
    }

    foreach ($path in Get-ProgramFilesPythonCandidates) { $candidates.Add($path) }
    foreach ($path in Get-PyLauncherCandidates) { $candidates.Add($path) }

    foreach ($cmdName in @('python3', 'python')) {
        $cmd = Get-Command $cmdName -ErrorAction SilentlyContinue
        if ($cmd) { $candidates.Add($cmd.Source) }
    }

    $seen = @{}
    foreach ($candidate in $candidates) {
        $key = $candidate.ToLowerInvariant()
        if ($seen.ContainsKey($key)) { continue }
        $seen[$key] = $true
        if (Test-PythonExecutable -Exe $candidate) {
            return $candidate
        }
    }

    throw @"
Python 3 not found or none of the discovered installs run successfully.

Install Python 3 from https://www.python.org/downloads/ (check "Add to PATH"), or set:
  `$env:PYTHON = 'C:\Path\To\python.exe'

If `py -3` fails because it references a missing GitHub Actions tool cache, either set `$env:PYTHON`
or run: py -3.11 scripts\version\verify-version-consistency.py
"@
}

function Invoke-PythonScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptPath,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    $python = Get-PythonExecutable
    & $python $ScriptPath @Arguments
    return $LASTEXITCODE
}
