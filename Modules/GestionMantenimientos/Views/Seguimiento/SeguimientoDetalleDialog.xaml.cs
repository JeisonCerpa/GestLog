using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.ViewModels.Seguimiento;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;
using CommunityToolkit.Mvvm.Messaging;
using MessageBox = System.Windows.MessageBox;

namespace GestLog.Modules.GestionMantenimientos.Views.Seguimiento
{
    /// <summary>
    /// Diálogo modal para visualizar detalles de un seguimiento ejecutado.
    /// Proporciona información completa sobre equipos, fechas, descripciones y observaciones.
    /// Permite editar y eliminar seguimientos según restricciones de estado y tiempo.
    /// </summary>
    public partial class SeguimientoDetalleDialog : Window
    {
        private readonly ISeguimientoService _seguimientoService;
        public SeguimientoMantenimientoDto? SeguimientoEditado { get; set; }

        public SeguimientoDetalleDialog(SeguimientoMantenimientoDto seguimientoDto, ISeguimientoService seguimientoService)
        {
            InitializeComponent();

            if (seguimientoDto == null)
                throw new ArgumentNullException(nameof(seguimientoDto));
            
            if (seguimientoService == null)
                throw new ArgumentNullException(nameof(seguimientoService));

            _seguimientoService = seguimientoService;

            // Configurar el ViewModel y el contexto de datos
            DataContext = new SeguimientoDetalleViewModel(seguimientoDto);

            // Maximizar la ventana para que el overlay cubra toda la pantalla
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        /// <summary>
        /// Maneja el evento de tecla presionada para cerrar con ESC
        /// </summary>
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        /// <summary>
        /// Maneja el clic en el overlay para cerrar el diálogo
        /// </summary>
        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Cerrar solo si se hace clic en el Grid del overlay, no en contenido dentro
            if (e.Source == sender)
            {
                Close();
            }
        }

        /// <summary>
        /// Previene que el clic en el panel principal cierre el diálogo
        /// </summary>
        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Maneja el evento del botón Cerrar
        /// </summary>
        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Maneja el evento del botón Editar
        /// </summary>
        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SeguimientoDetalleViewModel;
            if (viewModel?.Seguimiento == null)
            {
                return;
            }

            // Entrar en modo edición
            viewModel.EntrarModoEdicion();
        }        /// <summary>
        /// Maneja el evento del botón Guardar cambios
        /// </summary>
        private async void Guardar_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SeguimientoDetalleViewModel;
            if (viewModel?.Seguimiento == null)
            {
                return;
            }

            try
            {
                // Guardar los cambios en el ViewModel
                viewModel.GuardarCambios();
                
                // Guardar en la base de datos de forma asíncrona
                await _seguimientoService.UpdateAsync(viewModel.Seguimiento);
                
                MessageBox.Show(
                    "Cambios guardados correctamente.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Enviar mensaje de actualización
                WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage(true));

                // Marcar como guardado
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al guardar los cambios:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Maneja el evento del botón Cancelar edición
        /// </summary>
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SeguimientoDetalleViewModel;
            if (viewModel == null)
            {
                return;
            }

            // Salir del modo edición sin guardar
            viewModel.SalirModoEdicion();
        }        /// <summary>
        /// Maneja el evento del botón Eliminar con confirmación
        /// </summary>
        private async void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SeguimientoDetalleViewModel;
            if (viewModel?.Seguimiento == null)
            {
                return;
            }

            // Solicitar confirmación
            var resultado = MessageBox.Show(
                $"¿Está seguro de que desea eliminar este seguimiento?\n\nEquipo: {viewModel.Seguimiento.Codigo}\nFecha: {viewModel.Seguimiento.FechaRegistro:dd/MM/yyyy}",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    // Validar que el código no sea nulo
                    if (string.IsNullOrWhiteSpace(viewModel.Seguimiento.Codigo))
                    {
                        throw new InvalidOperationException("El código del seguimiento no puede estar vacío.");
                    }
                    
                    // Verificar si es preventivo o correctivo
                    bool esPreventivo = viewModel.Seguimiento.TipoMtno == GestLog.Modules.GestionMantenimientos.Models.Enums.TipoMantenimiento.Preventivo;
                    
                    if (esPreventivo)
                    {
                        // Para preventivos: marcar como NoRealizado en lugar de eliminar
                        viewModel.Seguimiento.Estado = GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.NoRealizado;
                        viewModel.Seguimiento.FechaRealizacion = null;
                        await _seguimientoService.UpdateAsync(viewModel.Seguimiento);
                        
                        MessageBox.Show(
                            "Seguimiento marcado como no realizado.",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        // Para correctivos: eliminar completamente
                        await _seguimientoService.DeleteAsync(viewModel.Seguimiento.Codigo);
                        
                        MessageBox.Show(
                            "Seguimiento eliminado correctamente.",
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }

                    // Enviar mensaje de actualización para recargar la vista semanal
                    WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage(true));

                    // Marcar como eliminado y cerrar
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al eliminar el seguimiento:\n\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }
}
