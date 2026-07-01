using System.Windows.Controls;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    /// <summary>
    /// Interacción lógica para PlantillasMantenimientoView.xaml
    /// </summary>
    public partial class PlantillasMantenimientoView : System.Windows.Controls.UserControl
    {
        public PlantillasMantenimientoView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is PlantillasMantenimientoViewModel viewModel)
                {
                    await viewModel.LoadPlantillasAsync();
                }
            };
        }

        private void BtnNuevaPlantilla_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is PlantillasMantenimientoViewModel viewModel)
            {
                viewModel.OpenCrearPlantillaCommand.Execute(null);
            }

            var dialog = new CrearEditarPlantillaDialog
            {
                DataContext = DataContext,
                Owner = System.Windows.Window.GetWindow(this)
            };

            dialog.ShowDialog();
        }

        private void BtnEditarPlantilla_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Models.DTOs.PlantillaMantenimientoDto plantilla)
            {
                if (DataContext is PlantillasMantenimientoViewModel viewModel)
                {
                    viewModel.NuevaPlantillaNombre = plantilla.Nombre;
                    viewModel.NuevaPlantillaDescripcion = plantilla.Descripcion ?? string.Empty;
                    viewModel.NuevoIntervaloKm = plantilla.IntervaloKM;
                    viewModel.NuevoIntervaloDias = plantilla.IntervaloDias;
                    viewModel.NuevoTipoIntervalo = plantilla.TipoIntervalo;
                    viewModel.SelectedPlantilla = plantilla;
                    viewModel.PlantillaEnEdicion = plantilla;
                    viewModel.ErrorMessage = string.Empty;
                    viewModel.SuccessMessage = string.Empty;
                }

                var dialog = new CrearEditarPlantillaDialog
                {
                    DataContext = DataContext,
                    Owner = System.Windows.Window.GetWindow(this)
                };

                dialog.ShowDialog();
            }
        }

        private void BtnEliminarPlantilla_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Models.DTOs.PlantillaMantenimientoDto plantilla)
            {
                if (DataContext is PlantillasMantenimientoViewModel viewModel)
                {
                    viewModel.SelectedPlantilla = plantilla;
                    viewModel.EliminarPlantillaCommand.Execute(null);
                }
            }
        }
    }
}
