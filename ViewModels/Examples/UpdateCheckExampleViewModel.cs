// Ejemplo práctico: Cómo integrar la detección de permisos en un ViewModel
// que verifica actualizaciones al iniciar la aplicación

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services;
using GestLog.Services.Core.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.ViewModels
{
    /// <summary>
    /// Ejemplo de cómo verificar actualizaciones y detectar problemas de permisos
    /// </summary>
    public partial class UpdateCheckExampleViewModel : ObservableObject
    {
        private readonly VelopackUpdateService _velopackService;
        private readonly IGestLogLogger _logger;

        // 🟢 Estado de la verificación
        [ObservableProperty]
        private bool isCheckingUpdates;

        // 📝 Mensaje de estado para el usuario
        [ObservableProperty]
        private string updateStatusMessage = string.Empty;

        // 🎨 Color del mensaje (verde, naranja, rojo, gris)
        [ObservableProperty]
        private string updateStatusColor = "#504F4E"; // Gris por defecto

        // ❌ Indica si hay un problema crítico (error de permisos)
        [ObservableProperty]
        private bool hasUpdateAccessError;

        public UpdateCheckExampleViewModel(
            VelopackUpdateService velopackService,
            IGestLogLogger logger)
        {
            _velopackService = velopackService;
            _logger = logger;
        }

        /// <summary>
        /// EJEMPLO 1: Verificar actualizaciones (simple)
        /// Ideal para: botón "Verificar actualizaciones" manual
        /// </summary>
        [RelayCommand]
        public async Task CheckUpdatesManualAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsCheckingUpdates = true;
                UpdateStatusMessage = "Verificando actualizaciones...";
                UpdateStatusColor = "#504F4E"; // Gris (procesando)
                HasUpdateAccessError = false;

                var result = await _velopackService.CheckForUpdatesAsync();

                // 🔴 CASO 1: Error de permisos/acceso - CRÍTICO
                if (result.HasAccessError)
                {
                    _logger.LogWarning($"❌ Error de acceso al servidor: {result.ErrorType}");
                    
                    UpdateStatusMessage = "❌ No se puede acceder al servidor de actualizaciones";
                    UpdateStatusColor = "#C0392B"; // Rojo (error)
                    HasUpdateAccessError = true;
                    
                    // Usuario verá un mensaje claro y podrá contactar soporte
                    // La aplicación puede mostrar un botón "Contactar soporte"
                    return;
                }

                // 🟡 CASO 2: Actualizaciones disponibles
                if (result.HasUpdatesAvailable)
                {
                    _logger.LogInformation("✅ Actualizaciones disponibles");
                    
                    UpdateStatusMessage = "Nueva actualización disponible";
                    UpdateStatusColor = "#F9B233"; // Ámbar (atención)
                    
                    // Preguntar al usuario si quiere actualizar
                    await _velopackService.NotifyAndPromptForUpdateAsync();
                    return;
                }

                // 🟢 CASO 3: Versión actual es la más reciente
                _logger.LogInformation("✅ Versión actual es la más reciente");
                
                UpdateStatusMessage = "Versión actual es la más reciente";
                UpdateStatusColor = "#2B8E3F"; // Verde (OK)
                HasUpdateAccessError = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al verificar actualizaciones");
                
                UpdateStatusMessage = "Error inesperado - Contacte soporte";
                UpdateStatusColor = "#C0392B"; // Rojo (error)
                HasUpdateAccessError = true;
            }
            finally
            {
                IsCheckingUpdates = false;
            }
        }

        /// <summary>
        /// EJEMPLO 2: Verificar actualizaciones al iniciar (silencioso)
        /// Ideal para: MainWindowViewModel al abrir la aplicación
        /// </summary>
        public async Task CheckUpdatesOnStartupAsync()
        {
            try
            {
                _logger.LogInformation("🔍 [STARTUP] Verificando actualizaciones...");
                
                var result = await _velopackService.CheckForUpdatesAsync();

                if (result.HasAccessError)
                {
                    // ❌ Error de permisos - registrar pero NO bloquear inicio
                    _logger.LogWarning(
                        $"⚠️ Error de acceso al servidor de actualizaciones: {result.ErrorType}");
                    
                    // Mostrar en navbar o status bar, sin interrumpir
                    UpdateStatusMessage = result.StatusMessage;
                    HasUpdateAccessError = true;
                    return;
                }

                if (result.HasUpdatesAvailable)
                {
                    // ✅ Actualizaciones disponibles - notificar silenciosamente
                    _logger.LogInformation("📬 Actualizaciones disponibles");
                    
                    // Opción 1: Mostrar notificación silenciosa después de 5 segundos
                    await Task.Delay(5000);
                    await _velopackService.NotifyAndPromptForUpdateAsync();
                }
                else
                {
                    // ℹ️ Todo bien - versión actual
                    _logger.LogInformation("✅ Versión actual es la más reciente");
                }
            }
            catch (Exception ex)
            {
                // No bloquear startup por error de actualización
                _logger.LogWarning(ex, "Advertencia al verificar actualizaciones en startup");
            }
        }

        /// <summary>
        /// EJEMPLO 3: Manejo completo con reintentos
        /// Ideal para: verificación periódica en background
        /// </summary>
        [RelayCommand]
        public async Task CheckUpdatesWithRetryAsync(int maxRetries = 3)
        {
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    _logger.LogInformation($"🔄 Intento {retryCount + 1}/{maxRetries}");
                    
                    var result = await _velopackService.CheckForUpdatesAsync();                    // ❌ Error de permisos - NO reintentar (problema permanente)
                    if (result.HasAccessError)
                    {
                        _logger.LogError(result.InnerException ?? new Exception(result.StatusMessage),
                            $"❌ Error de acceso (no se reintentará): {result.ErrorType}");
                        
                        HasUpdateAccessError = true;
                        UpdateStatusMessage = result.StatusMessage;
                        return;
                    }

                    // ✅ Éxito - salir del loop
                    if (result.HasUpdatesAvailable)
                    {
                        await _velopackService.NotifyAndPromptForUpdateAsync();
                    }
                    
                    return; // Éxito
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Verificación de actualizaciones cancelada");
                    throw; // No reintentar
                }
                catch (Exception ex) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    _logger.LogWarning(
                        $"⚠️ Intento {retryCount} falló, reintentando... ({ex.Message})");
                    
                    // Esperar 2 segundos antes de reintentar
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error final después de reintentos");
                    
                    UpdateStatusMessage = "No se pudo verificar actualizaciones";
                    UpdateStatusColor = "#C0392B"; // Rojo
                    return;
                }
            }
        }
    }
}

/*
 * NOTAS DE INTEGRACIÓN:
 * 
 * 1. En MainWindowViewModel.cs (al abrir app):
 *    ```csharp
 *    await _updateCheckViewModel.CheckUpdatesOnStartupAsync();
 *    ```
 *
 * 2. En MainWindow.xaml (mostrar estado):
 *    ```xaml
 *    <TextBlock Text="{Binding UpdateStatusMessage}" 
 *               Foreground="{Binding UpdateStatusColor, Converter=...}"
 *               Visibility="{Binding HasUpdateAccessError, Converter={StaticResource BoolToVisibility}}" />
 *    
 *    <Button Content="Reintentar"
 *            Command="{Binding CheckUpdatesManualCommand}"
 *            IsEnabled="{Binding HasUpdateAccessError}" />
 *    ```
 *
 * 3. En App.xaml.cs, al iniciar:
 *    ```csharp
 *    protected override void OnStartup(StartupEventArgs e)
 *    {
 *        base.OnStartup(e);
 *        var viewModel = ServiceLocator.GetInstance<MainWindowViewModel>();
 *        viewModel.CheckUpdatesOnStartupAsync(); // Fire and forget
 *    }
 *    ```
 */
