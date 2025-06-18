# Script para probar el comportamiento de shutdown de GestLog
# Versión: 1.0
# Fecha: 18 de junio de 2025

Write-Host "🧪 === PRUEBA DE SHUTDOWN COMPLETO DE GESTLOG ===" -ForegroundColor Cyan
Write-Host ""

# Función para obtener información de procesos
function Get-GestLogProcesses {
    return Get-Process | Where-Object {$_.ProcessName -like "*GestLog*"}
}

# Función para obtener conexiones de red activas
function Get-GestLogNetworkConnections {
    $processes = Get-GestLogProcesses
    $connections = @()
    foreach ($proc in $processes) {
        try {
            $conns = Get-NetTCPConnection | Where-Object {$_.OwningProcess -eq $proc.Id}
            $connections += $conns
        } catch {
            # Ignorar errores de acceso
        }
    }
    return $connections
}

# Verificar estado inicial
Write-Host "📊 Estado inicial:" -ForegroundColor Yellow
$initialProcesses = Get-GestLogProcesses
$initialConnections = Get-GestLogNetworkConnections

Write-Host "  - Procesos GestLog: $($initialProcesses.Count)" -ForegroundColor Green
Write-Host "  - Conexiones de red: $($initialConnections.Count)" -ForegroundColor Green

if ($initialProcesses.Count -gt 0) {
    Write-Host "  - PID principal: $($initialProcesses[0].Id)" -ForegroundColor Green
    Write-Host "  - Memoria utilizada: $([math]::Round($initialProcesses[0].WorkingSet / 1MB, 2)) MB" -ForegroundColor Green
}

Write-Host ""

# Esperar unos segundos para que la aplicación se estabilice
Write-Host "⏳ Esperando 5 segundos para estabilización..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Simular cierre de la aplicación (enviando señal de cierre)
Write-Host "🛑 Cerrando aplicación..." -ForegroundColor Yellow

try {
    $initialProcesses | ForEach-Object {
        Write-Host "  - Enviando señal de cierre a PID $($_.Id)" -ForegroundColor Cyan
        $_.CloseMainWindow()
    }
} catch {
    Write-Host "  - Error al enviar señal de cierre: $($_.Exception.Message)" -ForegroundColor Red
}

# Monitorear el proceso de shutdown
Write-Host ""
Write-Host "🔍 Monitoreando shutdown..." -ForegroundColor Yellow

$maxWaitTime = 15 # segundos
$checkInterval = 1 # segundo
$elapsedTime = 0

while ($elapsedTime -lt $maxWaitTime) {
    Start-Sleep -Seconds $checkInterval
    $elapsedTime += $checkInterval
    
    $currentProcesses = Get-GestLogProcesses
    $currentConnections = Get-GestLogNetworkConnections
    
    Write-Host "  [$elapsedTime s] Procesos: $($currentProcesses.Count), Conexiones: $($currentConnections.Count)" -ForegroundColor Gray
    
    if ($currentProcesses.Count -eq 0 -and $currentConnections.Count -eq 0) {
        Write-Host "✅ ¡Shutdown completado exitosamente!" -ForegroundColor Green
        Write-Host "  - Tiempo total: $elapsedTime segundos" -ForegroundColor Green
        Write-Host "  - Todos los procesos cerrados: ✅" -ForegroundColor Green
        Write-Host "  - Todas las conexiones cerradas: ✅" -ForegroundColor Green
        break
    }
}

# Verificación final
Write-Host ""
Write-Host "📋 Resultado final:" -ForegroundColor Yellow

$finalProcesses = Get-GestLogProcesses
$finalConnections = Get-GestLogNetworkConnections

if ($finalProcesses.Count -eq 0) {
    Write-Host "  ✅ Procesos: Todos cerrados correctamente" -ForegroundColor Green
} else {
    Write-Host "  ❌ Procesos: $($finalProcesses.Count) procesos aún activos" -ForegroundColor Red
    $finalProcesses | ForEach-Object {
        Write-Host "    - PID $($_.Id): $($_.ProcessName)" -ForegroundColor Red
    }
}

if ($finalConnections.Count -eq 0) {
    Write-Host "  ✅ Conexiones: Todas cerradas correctamente" -ForegroundColor Green
} else {
    Write-Host "  ❌ Conexiones: $($finalConnections.Count) conexiones aún activas" -ForegroundColor Red
}

# Forzar cierre si es necesario
if ($finalProcesses.Count -gt 0) {
    Write-Host ""
    Write-Host "🔨 Forzando cierre de procesos restantes..." -ForegroundColor Red
    $finalProcesses | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force
            Write-Host "  - Proceso $($_.Id) terminado forzosamente" -ForegroundColor Yellow
        } catch {
            Write-Host "  - Error terminando proceso $($_.Id): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "🏁 Prueba de shutdown completada" -ForegroundColor Cyan
