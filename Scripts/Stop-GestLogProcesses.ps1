$ErrorActionPreference = 'SilentlyContinue'

$deadline = (Get-Date).AddSeconds(12)

do {
    $killedAny = $false

    $gestLogProcesses = Get-Process -Name GestLog -ErrorAction SilentlyContinue
    foreach ($process in $gestLogProcesses) {
        try {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            $killedAny = $true
        } catch {
        }
    }

    try {
        & cmd /c "taskkill /f /t /im GestLog.exe > nul 2>&1" | Out-Null
    } catch {
    }

    $dotnetProcesses = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like '*GestLog.dll*' }

    foreach ($process in $dotnetProcesses) {
        try {
            & cmd /c "taskkill /f /t /pid $($process.ProcessId) > nul 2>&1" | Out-Null
            Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
            $killedAny = $true
        } catch {
        }
    }

    if ($killedAny) {
        Start-Sleep -Milliseconds 350
    }

    $remainingGestLog = Get-Process -Name GestLog -ErrorAction SilentlyContinue
    $remainingDotnet = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like '*GestLog.dll*' }

    if (-not $remainingGestLog -and -not $remainingDotnet) {
        break
    }

} while ((Get-Date) -lt $deadline)

exit 0
