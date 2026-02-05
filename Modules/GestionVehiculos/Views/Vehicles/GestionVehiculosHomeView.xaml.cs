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
        }        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is GestionVehiculosHomeViewModel viewModel)
            {
                await viewModel.LoadVehiclesCommand.ExecuteAsync(null);
            }
        }
    }
}
