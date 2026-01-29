using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Data;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;

namespace GestLog.Modules.GestionMantenimientos.Views.Seguimiento
{
    /// <summary>
    /// Lógica de interacción para SeguimientoView.xaml
    /// </summary>
    public partial class SeguimientoView : System.Windows.Controls.UserControl
    {        
        public SeguimientoView()
        {
            InitializeComponent();
            // Asignar el DataContext usando DI
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<GestLog.Modules.GestionMantenimientos.ViewModels.Seguimiento.SeguimientoViewModel>();
            DataContext = viewModel;
            // El filtrado se realiza ahora en el ViewModel, no en el code-behind ni con CollectionViewSource
            // var cvs = (CollectionViewSource)this.Resources["SeguimientosFiltrados"];
            // cvs.Filter += OnSeguimientoFilter;
        }

        /// <summary>
        /// Abre el modal de detalles del seguimiento cuando el usuario hace clic en "Ver Detalles"
        /// </summary>
        private void VerDetalles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtener el botón que fue clickeado
                var button = sender as System.Windows.Controls.Button;
                if (button?.Tag is SeguimientoMantenimientoDto seguimiento && seguimiento != null)
                {
                    // Obtener la ventana padre para establecer como Owner del dialog
                    var window = Window.GetWindow(this);
                    
                    // Obtener el servicio de inyección de dependencias
                    var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                    var seguimientoService = serviceProvider.GetRequiredService<ISeguimientoService>();
                    
                    // Crear y mostrar el modal de detalles
                    var detalleDialog = new SeguimientoDetalleDialog(seguimiento, seguimientoService);
                    detalleDialog.Owner = window;
                    detalleDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al abrir detalles del seguimiento: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}


