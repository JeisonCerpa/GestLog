using System.Windows;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.ViewModels.Cronograma;
using GestLog.Modules.GestionMantenimientos.Views.Seguimiento;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionMantenimientos.Views.Cronograma.SemanaDetalle
{
    public partial class SemanaDetalleDialog : Window
    {
        public SemanaDetalleDialog(SemanaDetalleViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void OnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;

            try
            {
                // Guardar referencia a la pantalla actual del owner
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                // Para un overlay modal, siempre maximizar para cubrir toda la pantalla
                // Esto evita problemas de DPI, pantallas múltiples y posicionamiento
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                // Fallback: maximizar en pantalla principal
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                // Si el Owner se mueve/redimensiona, mantener sincronizado
                this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }
        }

        private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
        {
            if (this.Owner == null) return;

            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Siempre maximizar para mantener el overlay cubriendo toda la pantalla
                    this.WindowState = WindowState.Maximized;
                }
                catch
                {
                    // En caso de error, asegurar que la ventana está maximizada
                    this.WindowState = WindowState.Maximized;
                }
            });
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Permitir que la rueda del mouse scroll el ScrollViewer
            var scrollViewer = sender as System.Windows.Controls.ScrollViewer;
            if (scrollViewer != null)
            {
                // Scroll vertical
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }        /// <summary>
        /// Abre el modal de detalles del seguimiento cuando el usuario hace clic en "Ver Detalles"
        /// </summary>
        private async void VerDetalles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtener el botón que fue clickeado
                var button = sender as System.Windows.Controls.Button;                if (button?.Tag is MantenimientoSemanaEstadoDto estadoMtno && estadoMtno.Seguimiento != null)
                {
                    // Asignar la Sede del MantenimientoSemanaEstadoDto al Seguimiento si no la tiene
                    if (estadoMtno.Seguimiento.Sede == null && estadoMtno.Sede.HasValue)
                    {
                        estadoMtno.Seguimiento.Sede = estadoMtno.Sede;
                    }                    // Obtener el servicio desde DI
                    var serviceProvider = (System.Windows.Application.Current as App)?.ServiceProvider;
                    var seguimientoService = serviceProvider?.GetRequiredService<ISeguimientoService>();
                    
                    if (seguimientoService == null)
                    {
                        System.Windows.MessageBox.Show("Error: No se pudo obtener el servicio de seguimientos.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }                    // Crear y mostrar el modal de detalles
                    var detalleDialog = new SeguimientoDetalleDialog(estadoMtno.Seguimiento, seguimientoService);
                    detalleDialog.Owner = this;
                    var resultado = detalleDialog.ShowDialog();
                    
                    // Si el resultado es true (guardado o eliminación), recargar los estados
                    if (resultado == true)
                    {
                        var viewModel = DataContext as SemanaDetalleViewModel;
                        if (viewModel != null)
                        {
                            await viewModel.RecargarEstadosAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al abrir detalles del seguimiento: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}


