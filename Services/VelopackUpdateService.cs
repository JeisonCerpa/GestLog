using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Windows;
using GestLog.Services.Core.Logging;

namespace GestLog.Services
{
    public class VelopackUpdateService
    {
        private readonly IGestLogLogger _logger;
        private readonly string _updateUrl;

        public VelopackUpdateService(GestLog.Services.Core.Logging.IGestLogLogger logger, string updateUrl)
        {
            _logger = logger;
            _updateUrl = updateUrl;
        }

        /// <summary>
        /// Verifica si hay actualizaciones disponibles SIN descargarlas
        /// </summary>
        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                _logger.LogDebug("🔍 Verificando actualizaciones disponibles...");
                var updater = new UpdateManager(_updateUrl);
                var updateInfo = await updater.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    var currentVersion = updater.CurrentVersion;
                    var availableVersion = updateInfo.TargetFullRelease?.Version;
                    
                    _logger.LogDebug($"📋 Versión actual: {currentVersion}, Versión disponible: {availableVersion}");
                    
                    // Solo devolver true si hay una versión NUEVA disponible
                    if (availableVersion != null && currentVersion != null && availableVersion > currentVersion)
                    {
                        _logger.LogInformation($"✅ Nueva actualización disponible: {availableVersion} (actual: {currentVersion})");
                        return true;
                    }
                    else
                    {
                        _logger.LogDebug($"ℹ️ Ya tienes la versión más reciente: {currentVersion}");
                        return false;
                    }
                }
                else
                {
                    _logger.LogDebug("ℹ️ No se encontró información de actualizaciones en el servidor");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error verificando actualizaciones");
                return false;
            }
        }

        /// <summary>
        /// Descarga e instala las actualizaciones (muestra UI de progreso)
        /// </summary>
        public async Task DownloadAndInstallUpdatesAsync(Action<double>? onProgress = null)
        {
            try
            {
                _logger.LogInformation("📥 Iniciando descarga de actualizaciones...");
                var updater = new UpdateManager(_updateUrl);
                var updateInfo = await updater.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    await updater.DownloadUpdatesAsync(updateInfo, progress =>
                    {
                        onProgress?.Invoke(progress);
                    });
                      _logger.LogInformation("🔄 Preparando aplicación de actualizaciones...");
                    
                    // Verificar si se necesitan privilegios de administrador
                    if (!IsRunningAsAdministrator())
                    {
                        _logger.LogInformation("🔐 Se requieren privilegios de administrador para aplicar la actualización");
                        var elevated = await RestartAsAdministratorForUpdateAsync();
                        if (!elevated)
                        {
                            _logger.LogWarning("⚠️ Usuario canceló la elevación de privilegios. Actualización cancelada.");
                            return;
                        }
                        // Si llegamos aquí, la aplicación se reiniciará con privilegios elevados
                        return;
                    }
                    
                    // Si ya tenemos privilegios, continuar con la actualización
                    _logger.LogInformation("🔄 Aplicando actualizaciones y reiniciando...");
                    
                    // Programar el cierre controlado de la aplicación
                    await ScheduleApplicationShutdownAsync();
                    
                    updater.ApplyUpdatesAndRestart(updateInfo);
                }
                else
                {
                    _logger.LogWarning("⚠️ No se encontraron actualizaciones para descargar");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error descargando/instalando actualizaciones");
                throw;
            }
        }

        /// <summary>
        /// Programa el cierre controlado de la aplicación para permitir la actualización
        /// </summary>
        private async Task ScheduleApplicationShutdownAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Programando cierre controlado de la aplicación...");
                
                // Dar tiempo para completar operaciones pendientes
                await Task.Delay(1000);
                
                // Forzar cierre de la aplicación en el hilo principal
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        _logger.LogInformation("🔄 Cerrando aplicación para permitir actualización...");
                        System.Windows.Application.Current?.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error cerrando aplicación");
                        // Forzar cierre del proceso si es necesario
                        Environment.Exit(0);
                    }
                }));
                
                // Dar tiempo adicional para que el cierre se procese
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en el cierre programado");
                // Forzar cierre inmediato si hay problemas
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Método legacy: Verifica, descarga e instala actualizaciones en una sola operación
        /// </summary>
        public async Task<bool> CheckAndUpdateAsync(Action<double> onProgress, CancellationToken cancellationToken)
        {
            try
            {
                var updater = new UpdateManager(_updateUrl);
                var updateInfo = await updater.CheckForUpdatesAsync();
                if (updateInfo != null)
                {
                    await updater.DownloadUpdatesAsync(updateInfo, progress =>
                    {
                        onProgress?.Invoke(progress);
                    }, cancellationToken);
                    updater.ApplyUpdatesAndRestart(updateInfo);
                    _logger.LogDebug("Actualización aplicada. Reiniciando aplicación...");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el proceso de actualización Velopack");
                return false;
            }
        }

        /// <summary>
        /// Verifica si la aplicación se está ejecutando con privilegios de administrador
        /// </summary>
        private bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo verificar privilegios de administrador");
                return false;
            }
        }

        /// <summary>
        /// Reinicia la aplicación con privilegios de administrador para aplicar actualizaciones
        /// </summary>
        private async Task<bool> RestartAsAdministratorForUpdateAsync()
        {
            try
            {
                _logger.LogInformation("🔐 Solicitando privilegios de administrador para aplicar actualización...");
                  var result = System.Windows.MessageBox.Show(
                    "Para aplicar la actualización, GestLog necesita permisos de administrador.\n\n" +
                    "¿Desea continuar? La aplicación se reiniciará con privilegios elevados.",
                    "Permisos requeridos",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    _logger.LogInformation("❌ Usuario canceló la elevación de privilegios");
                    return false;
                }

                // Obtener la ruta del ejecutable actual
                var currentProcess = Process.GetCurrentProcess();
                var executablePath = currentProcess.MainModule?.FileName;                if (string.IsNullOrEmpty(executablePath))
                {
                    var errorMsg = "No se pudo obtener la ruta del ejecutable actual";
                    _logger.LogError(new InvalidOperationException(errorMsg), errorMsg);
                    return false;
                }

                // Configurar el proceso para ejecutar como administrador
                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = "--apply-update", // Parámetro para indicar que debe aplicar actualización
                    UseShellExecute = true,
                    Verb = "runas" // Solicita elevación de privilegios
                };

                // Iniciar el proceso elevado
                var elevatedProcess = Process.Start(startInfo);
                
                if (elevatedProcess != null)
                {
                    _logger.LogInformation("✅ Aplicación reiniciada con privilegios de administrador");
                    
                    // Cerrar la instancia actual
                    await ScheduleApplicationShutdownAsync();
                    return true;
                }                else
                {
                    var errorMsg = "❌ No se pudo iniciar la aplicación con privilegios elevados";
                    _logger.LogError(new InvalidOperationException(errorMsg), errorMsg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al reiniciar como administrador");
                return false;
            }
        }
    }
}
