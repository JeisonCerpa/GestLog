using System.Windows;
using System.Windows.Controls;
using GestLog.Views.Tools.DaaterProccesor;
using GestLog.Views.Tools.ErrorLog;
using GestLog.Views.Tools.EnvioCatalogo;
using GestLog.Views.Configuration;
using GestLog.Views;
using GestLog.Services.Core.Logging;
using GestLog.Views.IdentidadCatalogos.Personas;
using GestLog.Views.IdentidadCatalogos.Usuarios;
using GestLog.Views.IdentidadCatalogos;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools
{
    public partial class HerramientasView : System.Windows.Controls.UserControl
    {
        private MainWindow? _mainWindow;

        public HerramientasView()
        {
            InitializeComponent();
            _mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }    private void BtnDaaterProccesor_Click(object sender, RoutedEventArgs e)
        {
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
                var errorLogView = new ErrorLogView();
                
                // Verificar que _mainWindow no sea null antes de pasarlo como parámetro
                if (_mainWindow != null)
                {
                    errorLogView.ShowErrorLog(_mainWindow);
                }
                else
                {
                    // Usar la ventana actual si _mainWindow es null
                    var currentWindow = Window.GetWindow(this);
                    if (currentWindow != null)
                    {
                        errorLogView.ShowErrorLog(currentWindow);
                    }
                    else                {
                        // Si no hay ventana disponible, mostrar sin propietario
                        System.Windows.MessageBox.Show("No se pudo obtener una ventana propietaria para el visor de errores.",
                            "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                        errorLogView.Show();
                    }
                }        }
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
        }

        private void BtnGestionMantenimientos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ahora navega a la vista contenedora con tabs
                var gestionMantenimientosView = new Views.Tools.GestionMantenimientos.GestionMantenimientosView();
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
                var type = System.Type.GetType("GestLog.Views.IdentidadCatalogos.Usuarios.UsuarioManagementView");
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
                    System.Windows.MessageBox.Show("No se encontró la clase UsuarioManagementView en GestLog.Views.IdentidadCatalogos.Usuarios.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var view = new IdentidadCatalogosHomeView { DataContext = viewModel };
            var mainWindow = System.Windows.Application.Current.MainWindow as GestLog.MainWindow;
            if (mainWindow != null)
                mainWindow.NavigateToView(view, "Gestión de Identidad y Catálogos");
        }
    }
}
