using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.ViewModels;

namespace GestLog.Modules.Usuarios.ViewModels
{
    public partial class IdentidadCatalogosHomeViewModel : ObservableObject
    {
        private readonly IGestLogLogger _logger;        public IdentidadCatalogosHomeViewModel()
        {
            var serviceProvider = LoggingService.GetServiceProvider();
            _logger = serviceProvider.GetRequiredService<IGestLogLogger>();
            _logger.LogInformation("🎯 IdentidadCatalogosHomeViewModel inicializado correctamente");
        }

        [RelayCommand]
        private void AbrirPersonas()
        {
            try
            {
                _logger.LogInformation("🧭 Navegando a Gestión de Personas");
                
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(PersonaManagementViewModel));
                
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ PersonaManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Personas", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var view = new GestLog.Views.IdentidadCatalogos.Personas.PersonaManagementView { DataContext = viewModel };
                var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
                
                if (mainWindow != null)
                {
                    _logger.LogInformation("✅ Navegando a vista de Personas");
                    mainWindow.NavigateToView(view, "Gestión de Personas");
                }
                else
                {
                    _logger.LogWarning("❌ MainWindow no encontrada");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al abrir Gestión de Personas");
                System.Windows.MessageBox.Show($"Error al abrir Gestión de Personas: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AbrirUsuarios()
        {
            try
            {
                _logger.LogInformation("🧭 Navegando a Gestión de Usuarios");
                
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(UsuarioManagementViewModel));
                
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ UsuarioManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Usuarios", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var view = new GestLog.Views.IdentidadCatalogos.Usuarios.UsuarioManagementView { DataContext = viewModel };
                var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
                
                if (mainWindow != null)
                {
                    _logger.LogInformation("✅ Navegando a vista de Usuarios");
                    mainWindow.NavigateToView(view, "Gestión de Usuarios");
                }
                else
                {
                    _logger.LogWarning("❌ MainWindow no encontrada");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al abrir Gestión de Usuarios");
                System.Windows.MessageBox.Show($"Error al abrir Gestión de Usuarios: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AbrirCatalogos()
        {
            try
            {
                _logger.LogInformation("🧭 Navegando a Gestión de Catálogos");
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(CatalogosManagementViewModel));
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ CatalogosManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Catálogos", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                var view = new GestLog.Views.Usuarios.CatalogosManagementView { DataContext = viewModel };
                var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
                if (mainWindow != null)
                {
                    _logger.LogInformation("✅ Navegando a vista de Catálogos");
                    mainWindow.NavigateToView(view, "Gestión de Catálogos");
                }
                else
                {
                    _logger.LogWarning("❌ MainWindow no encontrada");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al abrir Gestión de Catálogos");
                System.Windows.MessageBox.Show($"Error al abrir Gestión de Catálogos: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AbrirAuditoria()
        {
            try
            {
                _logger.LogInformation("🧭 Navegando a Historial de Auditoría");
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(AuditoriaManagementViewModel));
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ AuditoriaManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Auditoría", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                var view = new GestLog.Views.IdentidadCatalogos.Catalogos.AuditoriaManagementView { DataContext = viewModel };
                var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
                if (mainWindow != null)
                {
                    _logger.LogInformation("✅ Navegando a vista de Auditoría");
                    mainWindow.NavigateToView(view, "Historial de Auditoría");
                }
                else
                {
                    _logger.LogWarning("❌ MainWindow no encontrada");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al abrir Historial de Auditoría");
                System.Windows.MessageBox.Show($"Error al abrir Historial de Auditoría: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AbrirTiposDocumento()
        {
            try
            {
                _logger.LogInformation("🧭 Navegando a Gestión de Tipos de Documento");
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(TipoDocumentoManagementViewModel));
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ TipoDocumentoManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Tipos de Documento", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                var view = new GestLog.Views.Usuarios.TipoDocumentoManagementView { DataContext = viewModel };
                var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
                if (mainWindow != null)
                {
                    _logger.LogInformation("✅ Navegando a vista de Tipos de Documento");
                    mainWindow.NavigateToView(view, "Gestión de Tipos de Documento");
                }
                else
                {
                    _logger.LogWarning("❌ MainWindow no encontrada");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al abrir Gestión de Tipos de Documento");
                System.Windows.MessageBox.Show($"Error al abrir Gestión de Tipos de Documento: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
