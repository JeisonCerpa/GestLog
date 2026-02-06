using System;
using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;

namespace GestLog.Modules.GestionVehiculos.Views.Vehicles
{
    public partial class GestionVehiculosHomeView : System.Windows.Controls.UserControl
    {
        public GestionVehiculosHomeView()
        {
            InitializeComponent();
        }

        public GestionVehiculosHomeView(GestionVehiculosHomeViewModel viewModel) : this()
        {
            this.DataContext = viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is GestionVehiculosHomeViewModel viewModel)
            {
                await viewModel.LoadVehiclesCommand.ExecuteAsync(null);
            }
        }

        private async void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.DataContext is GestLog.Modules.GestionVehiculos.ViewModels.Vehicles.GestionVehiculosHomeViewModel viewModel)
                {
                    // Ejecutar el método asíncrono del ViewModel en el hilo UI
                    await viewModel.AgregarVehiculoAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[GestionVehiculosHomeView] DataContext no es GestionVehiculosHomeViewModel");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GestionVehiculosHomeView] Excepción en BtnAgregar_Click: {ex.Message}");
            }
        }
    }
}
