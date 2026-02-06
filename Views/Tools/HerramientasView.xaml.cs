using System.Windows;
using System.Windows.Controls;
using GestLog.Views.Tools.DaaterProccesor;
using GestLog.Views.Tools.ErrorLog;
using GestLog.Views.Tools.EnvioCatalogo;
using GestLog.Views.Configuration;
using GestLog.Views;
using GestLog.Services.Core.Logging;
using GestLog.Views.Tools.GestionIdentidadCatalogos.Personas;
using GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario;
using GestLog.Views.Tools.GestionIdentidadCatalogos;
using GestLog.Modules.GestionMantenimientos.Views;
using Microsoft.Extensions.DependencyInjection;
using GestLog.ViewModels.Tools;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.Views.Tools
{
    public partial class HerramientasView : System.Windows.Controls.UserControl
    {
        private MainWindow? _mainWindow;

        public HerramientasView()
        {
            InitializeComponent();
            _mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
            var viewModel = new GestLog.ViewModels.Tools.HerramientasViewModel(currentUserService);
            DataContext = viewModel;
        }    private void BtnDaaterProccesor_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessDaaterProcessor)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder a DaaterProccesor.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var daaterProccesorView = new DaaterProccesorView();
            _mainWindow?.NavigateToView(daaterProccesorView, "DaaterProccesor");
        }

        private void BtnConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var configurationView = new ConfigurationView();
                _mainWindow?.NavigateToView(configurationView, "Configuración");
            }
            catch (System.Exception ex)        {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar configuración desde herramientas");
            }
        }

        /// <summary>
        /// Muestra la ventana del registro de errores
        /// </summary>
        private void btnErrorLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                var errorLogViewModel = new GestLog.ViewModels.ErrorLogViewModel(currentUserService);
                var errorLogView = new ErrorLogView(errorLogViewModel);
                // Verificar que _mainWindow no sea null antes de pasarlo como parámetro
                if (_mainWindow != null)
                {
                    errorLogView.ShowErrorLog(_mainWindow);
                }
                else
                {
                    var currentWindow = Window.GetWindow(this);
                    if (currentWindow != null)
                    {
                        errorLogView.ShowErrorLog(currentWindow);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("No se pudo obtener una ventana propietaria para el visor de errores.",
                            "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                        errorLogView.Show();
                    }
                }
            }
            catch (System.Exception ex)        {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar registro de errores desde herramientas");
            }
        }    private void BtnGestionCartera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gestionCarteraView = new Views.Tools.GestionCartera.GestionCarteraView();
                _mainWindow?.NavigateToView(gestionCarteraView, "Gestión de Cartera");
            }        catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar gestión de cartera desde herramientas");
            }
        }

        private void BtnEnvioCatalogo_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessEnvioCatalogo)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder al Envío de Catálogo.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var envioCatalogoView = new EnvioCatalogoView();
                _mainWindow?.NavigateToView(envioCatalogoView, "Envío de Catálogo");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar envío de catálogo desde herramientas");
            }
        }        private void BtnGestionMantenimientos_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessGestionMantenimientos)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder a Gestión de Mantenimientos.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }            try
            {
                // Ahora navega a la vista contenedora con tabs
                var gestionMantenimientosView = new GestionMantenimientosView();
                _mainWindow?.NavigateToView(gestionMantenimientosView, "Gestión de Mantenimientos");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar gestión de mantenimientos desde herramientas");
            }
        }

        private void BtnGestionUsuarios_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Cargar la vista como Window usando reflexión, ya que está en el nuevo namespace
                var type = System.Type.GetType("GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario.UsuarioManagementView");
                if (type != null)
                {
                    var window = (System.Windows.Window?)System.Activator.CreateInstance(type);
                    if (window != null)
                    {
                        window.Owner = Window.GetWindow(this);
                        window.ShowDialog();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("No se pudo crear la ventana de gestión de usuarios.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("No se encontró la clase UsuarioManagementView en GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar gestión de usuarios desde herramientas");
            }
        }

        private void BtnGestionPersonas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var view = new PersonaManagementView();
                _mainWindow?.NavigateToView(view, "Gestión de Personas");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar gestión de personas desde herramientas");
            }
        }

        private void BtnGestionIdentidadCatalogos_Click(object sender, RoutedEventArgs e)
        {
            var serviceProvider = Services.Core.Logging.LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetService(typeof(GestLog.Modules.Usuarios.ViewModels.IdentidadCatalogosHomeViewModel)) as GestLog.Modules.Usuarios.ViewModels.IdentidadCatalogosHomeViewModel;
            var view = new GestLog.Views.Tools.GestionIdentidadCatalogos.IdentidadCatalogosHomeView { DataContext = viewModel };
            var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
            if (mainWindow != null)
                mainWindow.NavigateToView(view, "Gestión de Identidad y Catálogos");
        }

        private void BtnGestionEquipos_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessGestionEquipos)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder a Gestión de Equipos Informáticos.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }            try
            {
                var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var gestionVm = serviceProvider.GetService(typeof(GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos.GestionEquiposHomeViewModel)) as GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos.GestionEquiposHomeViewModel;
                var gestionEquiposView = new GestLog.Modules.GestionEquiposInformaticos.Views.Equipos.GestionEquiposHomeView();
                if (gestionVm != null)
                {
                    gestionEquiposView.DataContext = gestionVm;
                }

                _mainWindow?.NavigateToView(gestionEquiposView, "Gestión de Equipos Informáticos");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar Gestión de Equipos desde herramientas");
            }
        }

        private void BtnEquiposInformaticos_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessEquiposInformaticos)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder a Equipos Informáticos.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }            try
            {
                var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var gestionVm = serviceProvider.GetService(typeof(GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos.GestionEquiposHomeViewModel)) as GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos.GestionEquiposHomeViewModel;
                var equiposInformaticosView = new GestLog.Modules.GestionEquiposInformaticos.Views.Equipos.GestionEquiposHomeView();
                if (gestionVm != null)
                {
                    equiposInformaticosView.DataContext = gestionVm;
                }

                _mainWindow?.NavigateToView(equiposInformaticosView, "Equipos Informáticos");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar Equipos Informáticos desde herramientas");
            }
        }        private void BtnGestionVehiculos_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessGestionVehiculos)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder a Gestión de Vehículos.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                // ✅ Resolver vista y ViewModel desde DI explícitamente
                var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var gestionVehiculosViewModel = serviceProvider.GetRequiredService<GestLog.Modules.GestionVehiculos.ViewModels.Vehicles.GestionVehiculosHomeViewModel>();
                var gestionVehiculosView = new GestLog.Modules.GestionVehiculos.Views.Vehicles.GestionVehiculosHomeView(gestionVehiculosViewModel);
                _mainWindow?.NavigateToView(gestionVehiculosView, "Gestión de Vehículos");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar gestión de vehículos desde herramientas");
            }
        }
    }
}
