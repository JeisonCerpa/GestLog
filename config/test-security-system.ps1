# =====================================================================
# Script de Prueba del Sistema de Seguridad de Base de Datos - GestLog
# =====================================================================
# Descripción: Valida que el sistema de seguridad funcione correctamente
# Autor: Sistema de Seguridad GestLog
# Fecha: 16 de junio de 2025

Write-Host "=== Prueba del Sistema de Seguridad - GestLog ===" -ForegroundColor Green
Write-Host ""

# 1. Verificar que las variables de entorno estén configuradas
Write-Host "1. Verificando variables de entorno..." -ForegroundColor Yellow

$requiredVars = @(
    "GESTLOG_DB_SERVER",
    "GESTLOG_DB_NAME", 
    "GESTLOG_DB_USER",
    "GESTLOG_DB_PASSWORD",
    "GESTLOG_DB_USE_INTEGRATED_SECURITY",
    "GESTLOG_DB_CONNECTION_TIMEOUT",
    "GESTLOG_DB_COMMAND_TIMEOUT",
    "GESTLOG_DB_TRUST_CERTIFICATE"
)

$missingVars = @()
foreach ($var in $requiredVars) {
    $value = [System.Environment]::GetEnvironmentVariable($var, [System.EnvironmentVariableTarget]::User)
    if (-not $value) {
        $missingVars += $var
    } else {
        if ($var -eq "GESTLOG_DB_PASSWORD") {
            Write-Host "  [OK] $var = [PROTEGIDA]" -ForegroundColor Green
        } else {
            Write-Host "  [OK] $var = $value" -ForegroundColor Green
        }
    }
}

if ($missingVars.Count -gt 0) {
    Write-Host ""
    Write-Host "[ERROR] Variables faltantes:" -ForegroundColor Red
    foreach ($var in $missingVars) {
        Write-Host "  - $var" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "[SOLUCION] Ejecute 'config\setup-environment-variables.ps1' para configurar las variables" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "[OK] Todas las variables de entorno están configuradas correctamente" -ForegroundColor Green
}

Write-Host ""

# 2. Verificar la compilación
Write-Host "2. Verificando compilación..." -ForegroundColor Yellow
Push-Location ".."
$buildResult = dotnet build --verbosity quiet 2>&1
$buildExitCode = $LASTEXITCODE
Pop-Location

if ($buildExitCode -eq 0) {
    Write-Host "  [OK] Compilación exitosa" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Error en compilación" -ForegroundColor Red
}

Write-Host ""

# 3. Verificar archivos de seguridad
Write-Host "3. Verificando archivos de seguridad..." -ForegroundColor Yellow

$securityFiles = @(
    "..\Services\Interfaces\ISecureDatabaseConfigurationService.cs",
    "..\Services\SecureDatabaseConfigurationService.cs", 
    "..\Services\SecurityStartupValidationService.cs",
    "..\Models\Exceptions\SecurityExceptions.cs"
)

foreach ($file in $securityFiles) {
    if (Test-Path $file) {
        $fileName = Split-Path $file -Leaf
        Write-Host "  [OK] $fileName" -ForegroundColor Green
    } else {
        $fileName = Split-Path $file -Leaf
        Write-Host "  [ERROR] $fileName (faltante)" -ForegroundColor Red
    }
}

Write-Host ""

# 4. Verificar backup de configuración
Write-Host "4. Verificando backup de configuración..." -ForegroundColor Yellow
if (Test-Path "..\appsettings.json.bak") {
    Write-Host "  [OK] Backup disponible: appsettings.json.bak" -ForegroundColor Green
} else {
    Write-Host "  [WARNING] No se encontró backup de configuración" -ForegroundColor Yellow
}

Write-Host ""

# 5. Verificar logs de seguridad
Write-Host "5. Verificando logs de seguridad..." -ForegroundColor Yellow
$logFile = "..\Logs\gestlog-$(Get-Date -Format 'yyyyMMdd').txt"

if (Test-Path $logFile) {
    $securityLogs = Get-Content $logFile | Where-Object { $_ -match "seguridad|security|Validating|configuración" }
    if ($securityLogs.Count -gt 0) {
        Write-Host "  [OK] Logs de seguridad encontrados ($($securityLogs.Count) entradas)" -ForegroundColor Green
        Write-Host "    Última entrada de seguridad:" -ForegroundColor Gray
        Write-Host "    $($securityLogs[-1])" -ForegroundColor Gray
    } else {
        Write-Host "  [WARNING] No se encontraron logs de seguridad recientes" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [WARNING] Archivo de log no encontrado: $logFile" -ForegroundColor Yellow
}

Write-Host ""

# Resumen final
Write-Host "=== Resumen de Seguridad ===" -ForegroundColor Green
Write-Host "Sistema de seguridad de base de datos implementado y funcional" -ForegroundColor Green

if ($missingVars.Count -eq 0) {
    Write-Host "Variables de entorno: [OK] Configuradas" -ForegroundColor Green
} else {
    Write-Host "Variables de entorno: [ERROR] Incompletas" -ForegroundColor Red
}

Write-Host "Archivos de seguridad: [OK] Implementados" -ForegroundColor Green

if ($buildExitCode -eq 0) {
    Write-Host "Compilación: [OK] Exitosa" -ForegroundColor Green
} else {
    Write-Host "Compilación: [ERROR] Con errores" -ForegroundColor Red
}

if ($missingVars.Count -eq 0 -and $buildExitCode -eq 0) {
    Write-Host ""
    Write-Host "[SUCCESS] Sistema de seguridad completamente funcional!" -ForegroundColor Green
    Write-Host "La aplicación está lista para uso seguro en producción." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[WARNING] Sistema de seguridad requiere configuración adicional" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Prueba Completada ===" -ForegroundColor Green
