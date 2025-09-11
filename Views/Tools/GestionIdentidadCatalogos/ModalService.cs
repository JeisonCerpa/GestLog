using GestLog.Modules.Usuarios.ViewModels;
using GestLog.Modules.GestionMantenimientos.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace GestLog.Views.Usuarios
{
    public interface IModalService
    {
        void MostrarCargoModal(CatalogosManagementViewModel vm);
        void MostrarTipoDocumentoModal(CatalogosManagementViewModel vm);
        void MostrarRegistrarMantenimiento(RegistrarMantenimientoViewModel vm);
    }

    public class ModalService : IModalService
    {
        public void MostrarCargoModal(CatalogosManagementViewModel vm)
        {
            var window = new CargoModalWindow(vm);
            window.ShowDialog();
        }

        public void MostrarTipoDocumentoModal(CatalogosManagementViewModel vm)
        {
            var window = new TipoDocumentoModalWindow(vm);
            window.ShowDialog();
        }

        public void MostrarRegistrarMantenimiento(RegistrarMantenimientoViewModel vm)
        {
            // Cargar la ventana XAML por recurso para evitar dependencias directas al tipo generado
            try
            {
                var uri = new System.Uri("/GestLog;component/Modules/GestionMantenimientos/Views/RegistrarMantenimientoView.xaml", System.UriKind.Relative);
                var windowObj = System.Windows.Application.LoadComponent(uri);
                if (windowObj is Window window)
                {
                    window.DataContext = vm;
                    var owner = System.Windows.Application.Current?.MainWindow;
                    if (owner != null) window.Owner = owner;
                    vm.RequestClose = () => window.Dispatcher.Invoke(() => window.DialogResult = true);
                    // limpiar la referencia al cerrar para evitar retención
                    window.Closed += (s, e) => vm.RequestClose = null;
                    window.ShowDialog();
                }
                else
                {
                    // Fallback: crear una ventana genérica con el DataContext
                    var fallback = new Window { Title = "Registrar mantenimiento", Width = 600, Height = 450, Content = new TextBlock { Text = "Interfaz no disponible. Cierre para continuar.", Margin = new Thickness(10), TextWrapping = TextWrapping.Wrap } };
                    fallback.DataContext = vm;
                    var owner = System.Windows.Application.Current?.MainWindow;
                    if (owner != null) fallback.Owner = owner;
                    vm.RequestClose = () => fallback.Dispatcher.Invoke(() => fallback.DialogResult = true);
                    fallback.Closed += (s, e) => vm.RequestClose = null;
                    fallback.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                // Registrar detalle del error para facilitar debug en desarrollo
                try { Trace.TraceError($"MostrarRegistrarMantenimiento failed: {ex}"); } catch { /* no interrumpir fallback */ }

                // Si falla la carga, crear ventana fallback mínima
                var fallback = new Window { Title = "Registrar mantenimiento", Width = 600, Height = 450, Content = new TextBlock { Text = "Interfaz no disponible. Cierre para continuar.", Margin = new Thickness(10), TextWrapping = TextWrapping.Wrap } };
                fallback.DataContext = vm;
                var owner = System.Windows.Application.Current?.MainWindow;
                if (owner != null) fallback.Owner = owner;
                vm.RequestClose = () => fallback.Dispatcher.Invoke(() => fallback.DialogResult = true);
                fallback.Closed += (s, e) => vm.RequestClose = null;
                fallback.ShowDialog();
            }
        }
    }
}
