using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using GestLog.Services.Core.Logging;
using GestLog.Services.Exceptions;
using GestLog.Modules.DaaterProccesor.Exceptions;

namespace GestLog.Services
{
    public class VelopackUpdateService
    {
        private readonly IGestLogLogger _logger;
        private readonly string _updateUrl;        public VelopackUpdateService(GestLog.Services.Core.Logging.IGestLogLogger logger, string updateUrl)
        {
            _logger = logger;
            _updateUrl = updateUrl;
        }

        /// <summary>
        /// Verifica si hay actualizaciones disponibles SIN descargarlas
        /// Retorna información estructurada para detectar errores de permisos específicamente
        /// </summary>
        public async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            _logger.LogInformation("🔍 [INICIO] CheckForUpdatesAsync llamado");
            try
            {
                _logger.LogInformation("🔍 Verificando actualizaciones disponibles...");
                _logger.LogInformation($"🌐 URL del servidor: {_updateUrl}");
                
                _logger.LogInformation("📦 Creando UpdateManager...");
                var updater = new UpdateManager(_updateUrl);
                _logger.LogInformation($"✅ UpdateManager creado. Versión actual: {updater.CurrentVersion}");
                
                _logger.LogInformation("🌐 Llamando a CheckForUpdatesAsync...");
                var updateInfo = await updater.CheckForUpdatesAsync();
                _logger.LogInformation($"📡 Respuesta del servidor recibida. UpdateInfo es null: {updateInfo == null}");
                
                if (updateInfo != null)
                {
                    var currentVersion = updater.CurrentVersion;
                    var availableVersion = updateInfo.TargetFullRelease?.Version;
                    
                    _logger.LogInformation($"📋 Versión actual: {currentVersion}, Versión disponible: {availableVersion}");
                    
                    // Solo devolver true si hay una versión NUEVA disponible
                    if (availableVersion != null && currentVersion != null && availableVersion > currentVersion)
                    {
                        _logger.LogInformation($"✅ Nueva actualización disponible: {availableVersion} (actual: {currentVersion})");
                        return new UpdateCheckResult
                        {
                            HasUpdatesAvailable = true,
                            HasAccessError = false,
                            StatusMessage = $"Actualización disponible: v{availableVersion}"
                        };
                    }
                    else
                    {
                        _logger.LogInformation($"ℹ️ Ya tienes la versión más reciente: {currentVersion}");
                        return new UpdateCheckResult
                        {
                            HasUpdatesAvailable = false,
                            HasAccessError = false,
                            StatusMessage = $"Versión actual ({currentVersion}) es la más reciente"
                        };
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ No se encontró información de actualizaciones en el servidor");
                    return new UpdateCheckResult
                    {
                        HasUpdatesAvailable = false,
                        HasAccessError = false,
                        StatusMessage = "No se encontró información de actualizaciones"
                    };
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("VelopackLocator"))
            {
                // Aplicación no está ejecutándose desde Velopack - esto es normal en desarrollo
                _logger.LogInformation("ℹ️ [CATCH-VELOPACK] Actualizaciones no disponibles (modo desarrollo)");
                _logger.LogInformation($"🔍 Excepción: {ex.GetType().Name}");
                _logger.LogInformation($"📝 Mensaje: {ex.Message}");
                return new UpdateCheckResult
                {
                    HasUpdatesAvailable = false,
                    HasAccessError = false,
                    StatusMessage = "Modo desarrollo - actualizaciones no disponibles"
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                // Error específico de permisos
                _logger.LogError(ex, "❌ [ERROR-PERMISOS] Acceso denegado al servidor de actualizaciones");
                _logger.LogInformation($"🔒 Ruta del servidor: {_updateUrl}");
                _logger.LogInformation($"👤 Verificar: permisos de usuario, credenciales de red, acceso a la carpeta compartida");
                
                return new UpdateCheckResult
                {
                    HasUpdatesAvailable = false,
                    HasAccessError = true,
                    StatusMessage = "❌ Acceso denegado al servidor de actualizaciones. Verifique permisos de usuario y credenciales de red.",
                    ErrorType = "UnauthorizedAccess",
                    InnerException = ex
                };
            }
            catch (System.IO.IOException ex) when (ex.Message.Contains("acceso") || ex.Message.Contains("Access"))
            {
                // Error de I/O relacionado con acceso
                _logger.LogError(ex, "❌ [ERROR-IO] Problema de acceso a la carpeta de actualizaciones");
                _logger.LogInformation($"🔒 Ruta del servidor: {_updateUrl}");
                _logger.LogInformation($"📝 Detalles: {ex.Message}");
                
                return new UpdateCheckResult
                {
                    HasUpdatesAvailable = false,
                    HasAccessError = true,
                    StatusMessage = "❌ No se puede acceder al servidor de actualizaciones. Verifique la ruta de red y los permisos.",
                    ErrorType = "IOAccess",
                    InnerException = ex
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ [CATCH-GENERAL] Error inesperado al verificar actualizaciones");
                _logger.LogInformation($"🔍 Tipo: {ex.GetType().Name}");
                _logger.LogInformation($"📝 Mensaje: {ex.Message}");
                _logger.LogInformation($"📚 Stack: {ex.StackTrace}");
                
                // Determinar si parece ser un error de red/permisos
                bool likelyAccessError = ex.GetType().Name.Contains("Network") || 
                                         ex.Message.Contains("access") || 
                                         ex.Message.Contains("denied") ||
                                         ex.Message.Contains("No such host") ||
                                         ex.Message.Contains("cannot find");
                
                return new UpdateCheckResult
                {
                    HasUpdatesAvailable = false,
                    HasAccessError = likelyAccessError,
                    StatusMessage = likelyAccessError 
                        ? "⚠️ Error de conexión al servidor de actualizaciones. Verifique la red y los permisos."
                        : "⚠️ Error inesperado al verificar actualizaciones",
                    ErrorType = ex.GetType().Name,
                    InnerException = ex
                };
            }        }

        /// <summary>
        /// Versión heredada que retorna bool - usa CheckForUpdatesAsync internamente
        /// </summary>
        [Obsolete("Use CheckForUpdatesAsync() que retorna UpdateCheckResult para mejor información de diagnóstico")]
        public async Task<bool> CheckForUpdatesOldAsync()
        {
            var result = await CheckForUpdatesAsync();
            return result.HasUpdatesAvailable;
        }

        /// <summary>
        /// Muestra una notificación al usuario sobre actualizaciones disponibles
        /// </summary>
        public async Task<bool> NotifyAndPromptForUpdateAsync()
        {
            try
            {
                var checkResult = await CheckForUpdatesAsync();
                  // Si hay error de acceso al servidor, mostrar mensaje específico
                if (checkResult.HasAccessError)
                {
                    _logger.LogWarning($"⚠️ Error de acceso al servidor: {checkResult.StatusMessage}");
                    System.Windows.MessageBox.Show(
                        checkResult.StatusMessage + "\n\n" +
                        "Configuración del servidor:\n" +
                        $"URL: {_updateUrl}\n\n" +
                        "Soluciones:\n" +
                        "1. Verifique que tiene acceso a la carpeta de red\n" +
                        "2. Ingrese con sus credenciales de dominio\n" +
                        "3. Contacte al administrador de sistemas si el problema persiste",
                        "Error de Conexión - Actualizaciones",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return false;
                }

                if (!checkResult.HasUpdatesAvailable)
                {
                    _logger.LogInformation("ℹ️ No hay actualizaciones disponibles");
                    return false;
                }

                // Obtener información de la versión disponible
                var updater = new UpdateManager(_updateUrl);
                var updateInfo = await updater.CheckForUpdatesAsync();
                var availableVersion = updateInfo?.TargetFullRelease?.Version;

                // Mostrar diálogo al usuario
                var result = System.Windows.MessageBox.Show(
                    $"Nueva actualización disponible: v{availableVersion}\n\n" +
                    "¿Desea descargar e instalar la actualización ahora?\n\n" +
                    "Nota: La aplicación se reiniciará para aplicar la actualización.",
                    "Actualización Disponible",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    await DownloadAndInstallUpdatesAsync();
                    return true;
                }
                else
                {
                    _logger.LogInformation("ℹ️ Usuario decidió no actualizar en este momento");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al notificar actualizaciones");
                return false;
            }
        }

        /// <summary>
        /// Descarga e instala las actualizaciones (SIN auto-elevación)
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
                    
                    _logger.LogInformation("✅ Aplicando actualizaciones directamente (sin auto-elevación)...");
                    
                    // Aplicar actualizaciones directamente - la app ya se ejecuta como admin
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
                  // Mostrar error amigable al usuario
                System.Windows.MessageBox.Show(
                    $"Error al aplicar la actualización:\n{ex.Message}\n\n" +
                    "Por favor, asegúrese de que:\n" +
                    "1. GestLog se está ejecutando como Administrador\n" +
                    "2. Tiene acceso al servidor de actualizaciones\n" +
                    "3. No hay otro proceso de GestLog ejecutándose",
                    "Error de Actualización",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                throw;
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
        /// Obtiene información sobre la versión actual y disponible
        /// </summary>
        public async Task<(string? current, string? available)> GetVersionInfoAsync()
        {
            try
            {
                var updater = new UpdateManager(_updateUrl);
                var updateInfo = await updater.CheckForUpdatesAsync();
                
                var currentVersion = updater.CurrentVersion?.ToString();
                var availableVersion = updateInfo?.TargetFullRelease?.Version?.ToString();
                
                return (currentVersion, availableVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo información de versiones");
                return (null, null);
            }
        }
    }
}
