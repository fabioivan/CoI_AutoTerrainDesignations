param(
    [string]$LogPath,
    [string]$LogsDir = (Join-Path $env:APPDATA 'Captain of Industry\Logs'),
    [string[]]$Tags = @('[ATD Farming Perf]'),
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

$matches = Select-String -LiteralPath $LogPath -SimpleMatch -Pattern $Tags
if ($Last -gt 0) {
    $matches = $matches | Select-Object -Last $Last
}

Write-Output "Log: $LogPath"
foreach ($match in $matches) {
    Write-Output ("{0}: {1}" -f $match.LineNumber, $match.Line.Trim())
}
