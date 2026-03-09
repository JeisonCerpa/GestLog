using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.ViewModels;
using ModulosUsuarios = global::Modules.Usuarios.ViewModels;

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
        private async Task AbrirPersonas()
        {            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(PersonaManagementViewModel)) as PersonaManagementViewModel;
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ PersonaManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Personas", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                await viewModel.InicializarAsync();
                var view = new GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Personas.PersonaManagementView { DataContext = viewModel };
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
        {            try
            {                
                var serviceProvider = LoggingService.GetServiceProvider();                // Debug: verificar tipos registrados
                _logger.LogDebug("🔍 Verificando registro de UsuarioManagementViewModel en DI");
                var viewModel = serviceProvider.GetRequiredService<global::Modules.Usuarios.ViewModels.UsuarioManagementViewModel>();
                
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ UsuarioManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Usuarios", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                _logger.LogInformation("✅ UsuarioManagementViewModel resuelto exitosamente desde DI");

                var view = new GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Usuario.UsuarioManagementView { DataContext = viewModel };
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
        {            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(CatalogosManagementViewModel));
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ CatalogosManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Catálogos", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                var view = new GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Catalogos.CatalogosManagementView { DataContext = viewModel };
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
        private async Task AbrirCargos()
        {            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(CatalogosManagementViewModel)) as CatalogosManagementViewModel;
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ CatalogosManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Cargos", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                await viewModel.InitializeAsync();
                var view = new GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Catalogo.Cargos.CargosView { DataContext = viewModel };
                var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
                if (mainWindow != null)
                {
                    _logger.LogInformation("✅ Navegando a vista de Cargos");
                    mainWindow.NavigateToView(view, "Gestión de Cargos");
                }
                else
                {
                    _logger.LogWarning("❌ MainWindow no encontrada");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al abrir Gestión de Cargos");
                System.Windows.MessageBox.Show($"Error al abrir Gestión de Cargos: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task AbrirTiposDocumento()
        {            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(CatalogosManagementViewModel)) as CatalogosManagementViewModel;
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ CatalogosManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Tipos de Documento", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                await viewModel.InitializeAsync();
                var view = new GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Catalogo.TipoDocumento.TipoDocumentoManagementView { DataContext = viewModel };
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

        [RelayCommand]
        private void AbrirRoles()
        {            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(RolManagementViewModel));
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ RolManagementViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Roles", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                var view = new GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Catalogo.Roles.RolesView { DataContext = viewModel };
                var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
                if (mainWindow != null)
                {
                    _logger.LogInformation("✅ Navegando a vista de Roles");
                    mainWindow.NavigateToView(view, "Gestión de Roles");
                }
                else
                {
                    _logger.LogWarning("❌ MainWindow no encontrada");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al abrir Gestión de Roles");
                System.Windows.MessageBox.Show($"Error al abrir Gestión de Roles: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AbrirGestionPermisosRol()
        {            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetService(typeof(GestionPermisosRolViewModel));
                if (viewModel == null)
                {
                    _logger.LogWarning("❌ GestionPermisosRolViewModel no se pudo resolver desde DI");
                    System.Windows.MessageBox.Show("Error: No se pudo cargar el módulo de Gestión de Permisos por Rol", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                var view = new GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Catalogo.Permisos.GestionPermisosRolView();
                view.DataContext = viewModel;
                var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
                if (mainWindow != null)
                {
                    _logger.LogInformation("✅ Navegando a vista de Gestión de Permisos por Rol");
                    mainWindow.NavigateToView(view as System.Windows.Controls.UserControl, "Gestión de Permisos por Rol");
                }
                else
                {
                    _logger.LogWarning("❌ MainWindow no encontrada");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al abrir Gestión de Permisos por Rol");
                System.Windows.MessageBox.Show($"Error al abrir Gestión de Permisos por Rol: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
