# get-mod-log.ps1
# Extracts all ATD- and AFD-tagged rows from the newest (or a specified) CoI log file.
#
# Usage:
#   .\tools\get-mod-log.ps1                  # all [ATD / [AFD rows in newest log
#   .\tools\get-mod-log.ps1 -DllOnly         # version/DLL rows only (quick build-loaded check)
#   .\tools\get-mod-log.ps1 -Last 50         # last 50 mod-tagged rows
#   .\tools\get-mod-log.ps1 -LogPath C:\...\foo.log  # use a specific log file

param(
    [string]$LogPath,
    [string]$LogsDir = (Join-Path $env:APPDATA 'Captain of Industry\Logs'),
    [switch]$DllOnly,
    [int]$Last = 0
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    if (-not (Test-Path -LiteralPath $LogsDir)) {
        throw "Logs directory not found: $LogsDir"
    }

    $latestLog = Get-ChildItem -LiteralPath $LogsDir -Filter '*.log' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $latestLog) {
        throw "No .log files found in: $LogsDir"
    }

    $LogPath = $latestLog.FullName
}

if (-not (Test-Path -LiteralPath $LogPath)) {
    throw "Log file not found: $LogPath"
}

Write-Output "Log: $LogPath"

$modLines = Select-String -LiteralPath $LogPath -Pattern '\[(ATD|AFD)'

if ($DllOnly) {
    $modLines = $modLines | Where-Object { $_.Line -match 'dll:' }
}

if ($Last -gt 0) {
    $modLines = $modLines | Select-Object -Last $Last
}

foreach ($match in $modLines) {
    Write-Output ('{0}: {1}' -f $match.LineNumber, $match.Line.Trim())
}
