using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionVehiculos.Views.Vehicles
{
    /// <summary>
    /// Modal dialog para agregar/editar vehículos
    /// Sigue el patrón estándar de GestLog: Window con overlay semitransparente, card centrada, Header y Footer
    /// </summary>
    public partial class VehicleFormDialog : Window
    {
        public VehicleFormViewModel ViewModel { get; }

        /// <summary>
        /// Constructor para crear un nuevo vehículo
        /// </summary>
        public VehicleFormDialog(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            InitializeComponent();

            // Crear ViewModel para nuevo vehículo
            var vehicleService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService<IVehicleService>();
            var logger = ((App)System.Windows.Application.Current).ServiceProvider?.GetService<IGestLogLogger>();
            var photoStorage = ((App)System.Windows.Application.Current).ServiceProvider?.GetService<GestLog.Modules.GestionVehiculos.Interfaces.IPhotoStorageService>();

            if (vehicleService == null || logger == null || photoStorage == null)
                throw new InvalidOperationException("VehicleService, Logger o PhotoStorageService no están registrados en DI");

            ViewModel = new VehicleFormViewModel(vehicleService, logger, photoStorage);
            ViewModel.ConfigureForNew();
            DataContext = ViewModel;

            // Configurar como modal overlay
            try
            {
                this.Owner = System.Windows.Application.Current?.MainWindow;
                this.ShowInTaskbar = false;
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                // No crítico
            }

            // Manejar Escape para cerrar
            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    this.Close();
                    e.Handled = true;
                }
            };

            Loaded += async (s, e) =>
            {
                // Cargar datos iniciales si es necesario
                await System.Threading.Tasks.Task.CompletedTask;
            };
        }

        /// <summary>
        /// Constructor para editar un vehículo existente
        /// </summary>
        public VehicleFormDialog(VehicleDto vehicleToEdit, IDbContextFactory<GestLogDbContext> dbContextFactory) 
            : this(dbContextFactory)
        {
            ViewModel.ConfigureForEdit(vehicleToEdit);
        }

        /// <summary>
        /// Cierra el diálogo cuando se hace clic en el botón X
        /// </summary>
        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Cierra el diálogo cuando se hace clic en Cancelar
        /// </summary>
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// Impide cerrar el diálogo cuando se hace clic en el overlay
        /// </summary>
        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender == RootGrid)
            {
                // Evitar que el clic en el overlay cierre el diálogo
                e.Handled = true;
            }
        }

        /// <summary>
        /// Evita que los clics en el panel centrado se propaguen al overlay
        /// </summary>
        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
