using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.DaaterProccesor.Views;
using GestLog.Modules.EnvioCatalogo.Views;
using GestLog.Views;
using GestLog.Services.Core.Logging;
using GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Personas;
using GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Usuario;
using GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos;
using GestLog.Modules.GestionMantenimientos.Views;
using Microsoft.Extensions.DependencyInjection;
using GestLog.ViewModels.Tools;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.Modules.Shell.Views
{
    public partial class HerramientasView : System.Windows.Controls.UserControl
    {
        private MainWindow? _mainWindow;

        public HerramientasView()
        {
            InitializeComponent();
            _mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
            var viewModel = new GestLog.ViewModels.Tools.HerramientasViewModel(currentUserService);
            DataContext = viewModel;
        }

        private void BtnEquiposInformaticos_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessEquiposInformaticos)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder a Equipos InformÃ¡ticos.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var gestionVm = serviceProvider.GetService(typeof(GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos.GestionEquiposHomeViewModel)) as GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos.GestionEquiposHomeViewModel;
                var equiposInformaticosView = new GestLog.Modules.GestionEquiposInformaticos.Views.Equipos.GestionEquiposHomeView();
                if (gestionVm != null)
                {
                    equiposInformaticosView.DataContext = gestionVm;
                }

                _mainWindow?.NavigateToView(equiposInformaticosView, "Equipos InformÃ¡ticos");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar Equipos InformÃ¡ticos desde herramientas");
            }
        }

        private void BtnGestionMantenimientos_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessGestionMantenimientos)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder a GestiÃ³n de Mantenimientos.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var gestionMantenimientosView = new GestionMantenimientosView();
                _mainWindow?.NavigateToView(gestionMantenimientosView, "GestiÃ³n de Mantenimientos");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar gestiÃ³n de mantenimientos desde herramientas");
            }
        }

        private void BtnGestionVehiculos_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessGestionVehiculos)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder a GestiÃ³n de VehÃ­culos.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var gestionVehiculosViewModel = serviceProvider.GetRequiredService<GestLog.Modules.GestionVehiculos.ViewModels.Vehicles.GestionVehiculosHomeViewModel>();
                var gestionVehiculosView = new GestLog.Modules.GestionVehiculos.Views.Vehicles.GestionVehiculosHomeView(gestionVehiculosViewModel);
                _mainWindow?.NavigateToView(gestionVehiculosView, "GestiÃ³n de VehÃ­culos");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar gestiÃ³n de vehÃ­culos desde herramientas");
            }
        }

        private void BtnGestionCartera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gestionCarteraView = new GestLog.Modules.GestionCartera.Views.GestionCarteraView();
                _mainWindow?.NavigateToView(gestionCarteraView, "GestiÃ³n de Cartera");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar gestiÃ³n de cartera desde herramientas");
            }
        }

        private void BtnDaaterProccesor_Click(object sender, RoutedEventArgs e)
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

        private void BtnEnvioCatalogo_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessEnvioCatalogo)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder al EnvÃ­o de CatÃ¡logo.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var envioCatalogoView = new EnvioCatalogoView();
                _mainWindow?.NavigateToView(envioCatalogoView, "EnvÃ­o de CatÃ¡logo");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar envÃ­o de catÃ¡logo desde herramientas");
            }
        }

        private void BtnGestionIdentidadCatalogos_Click(object sender, RoutedEventArgs e)
        {
            var serviceProvider = Services.Core.Logging.LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetService(typeof(GestLog.Modules.Usuarios.ViewModels.IdentidadCatalogosHomeViewModel)) as GestLog.Modules.Usuarios.ViewModels.IdentidadCatalogosHomeViewModel;
            var view = new GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.IdentidadCatalogosHomeView { DataContext = viewModel };
            var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
            if (mainWindow != null)
                mainWindow.NavigateToView(view, "GestiÃ³n de Identidad y CatÃ¡logos");
        }

        private void BtnGestionUsuarios_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var type = System.Type.GetType("GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Usuario.UsuarioManagementView");
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
                        System.Windows.MessageBox.Show("No se pudo crear la ventana de gestiÃ³n de usuarios.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("No se encontrÃ³ la clase UsuarioManagementView en GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Usuario.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar gestiÃ³n de usuarios desde herramientas");
            }
        }

        private void BtnGestionPersonas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var view = new PersonaManagementView();
                _mainWindow?.NavigateToView(view, "GestiÃ³n de Personas");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar gestiÃ³n de personas desde herramientas");
            }
        }

        private void BtnGestionEquipos_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GestLog.ViewModels.Tools.HerramientasViewModel;
            if (viewModel != null && !viewModel.CanAccessGestionEquipos)
            {
                System.Windows.MessageBox.Show("No tiene permisos para acceder a GestiÃ³n de Equipos InformÃ¡ticos.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var gestionVm = serviceProvider.GetService(typeof(GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos.GestionEquiposHomeViewModel)) as GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos.GestionEquiposHomeViewModel;
                var gestionEquiposView = new GestLog.Modules.GestionEquiposInformaticos.Views.Equipos.GestionEquiposHomeView();
                if (gestionVm != null)
                {
                    gestionEquiposView.DataContext = gestionVm;
                }

                _mainWindow?.NavigateToView(gestionEquiposView, "GestiÃ³n de Equipos InformÃ¡ticos");
            }
            catch (System.Exception ex)
            {
                var errorHandler = LoggingService.GetErrorHandler();
                errorHandler.HandleException(ex, "Mostrar GestiÃ³n de Equipos desde herramientas");
            }
        }
    }
}


