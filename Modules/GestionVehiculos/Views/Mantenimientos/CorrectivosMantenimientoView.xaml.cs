namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    /// <summary>
    /// Interaction logic for CorrectivosMantenimientoView.xaml
    /// </summary>
    public partial class CorrectivosMantenimientoView : System.Windows.Controls.UserControl
    {
        public CorrectivosMantenimientoView()
        {
            InitializeComponent();
        }

        private void BtnNuevoCorrectivo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.Mantenimientos.CorrectivosMantenimientoViewModel vm)
            {
                return;
            }

            vm.PrepararNuevoCorrectivo();

            var dialog = new RegistroCorrectivoDialog(vm)
            {
                Owner = System.Windows.Application.Current?.Windows.Count > 0
                    ? System.Windows.Application.Current.Windows[0]
                    : System.Windows.Application.Current?.MainWindow
            };

            dialog.ShowDialog();
        }

        private async void ActionButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.Mantenimientos.CorrectivosMantenimientoViewModel vm)
            {
                return;
            }

            if (sender is not System.Windows.Controls.Button btn || btn.DataContext is not Models.DTOs.EjecucionMantenimientoDto dto)
            {
                return;
            }

            var estado = dto.EstadoCorrectivoEnum ?? Models.Enums.EstadoMantenimientoCorrectivoVehiculo.FallaReportada;
            switch (estado)
            {
                case Models.Enums.EstadoMantenimientoCorrectivoVehiculo.FallaReportada:
                {
                    var proveedor = dto.Proveedor ?? string.Empty;
                    var observaciones = string.Empty;
                    if (!ShowEnviarAReparacionDialog(ref proveedor, ref observaciones))
                    {
                        return;
                    }

                    await vm.EnviarAReparacionAsync(dto, proveedor, observaciones);
                    break;
                }

                case Models.Enums.EstadoMantenimientoCorrectivoVehiculo.EnTaller:
                {
                    var planesDisponibles = await vm.GetPlanesPreventivosParaCompletarAsync(dto.PlacaVehiculo);

                    var kilometraje = dto.KMAlMomento;
                    var responsable = dto.ResponsableEjecucion ?? string.Empty;
                    var proveedor = dto.Proveedor ?? string.Empty;
                    var costo = dto.Costo;
                    var rutaFactura = dto.RutaFactura ?? string.Empty;
                    var observaciones = string.Empty;
                    if (!ShowCompletarDialog(planesDisponibles, ref kilometraje, ref responsable, ref proveedor, ref costo, ref rutaFactura, ref observaciones, out var planesSeleccionados))
                    {
                        return;
                    }

                    await vm.CompletarCorrectivoAsync(dto, kilometraje, responsable, proveedor, costo, rutaFactura, observaciones, planesSeleccionados);
                    break;
                }

                case Models.Enums.EstadoMantenimientoCorrectivoVehiculo.Completado:
                case Models.Enums.EstadoMantenimientoCorrectivoVehiculo.Cancelado:
                    ShowDetallesDialog(dto);
                    break;
            }
        }

        private bool ShowEnviarAReparacionDialog(ref string proveedor, ref string observaciones)
        {
            var dialog = new EnviarReparacionCorrectivoDialog(proveedor, observaciones)
            {
                Owner = System.Windows.Application.Current?.Windows.Count > 0
                    ? System.Windows.Application.Current.Windows[0]
                    : System.Windows.Application.Current?.MainWindow
            };

            var result = dialog.ShowDialog() == true;
            if (result)
            {
                proveedor = dialog.Proveedor;
                observaciones = dialog.Observaciones;
            }

            return result;
        }

        private bool ShowCompletarDialog(
            System.Collections.Generic.IReadOnlyCollection<Models.DTOs.PlanMantenimientoVehiculoDto> planesDisponibles,
            ref long kilometraje,
            ref string responsable,
            ref string proveedor,
            ref decimal? costo,
            ref string rutaFactura,
            ref string observaciones,
            out System.Collections.Generic.IReadOnlyCollection<Models.DTOs.PlanMantenimientoVehiculoDto> planesSeleccionados)
        {
            planesSeleccionados = System.Array.Empty<Models.DTOs.PlanMantenimientoVehiculoDto>();

            var dialog = new CompletarCorrectivoDialog(kilometraje, responsable, proveedor, costo, rutaFactura, observaciones, planesDisponibles)
            {
                Owner = System.Windows.Application.Current?.Windows.Count > 0
                    ? System.Windows.Application.Current.Windows[0]
                    : System.Windows.Application.Current?.MainWindow
            };

            var result = dialog.ShowDialog() == true;
            if (result)
            {
                kilometraje = dialog.KilometrajeAlCompletar ?? kilometraje;
                responsable = dialog.Responsable;
                proveedor = dialog.Proveedor;
                observaciones = dialog.Observaciones;
                rutaFactura = dialog.RutaFactura;
                costo = dialog.Costo;

                if (dialog.PlanesPreventivosSeleccionados.Count > 0)
                {
                    planesSeleccionados = System.Linq.Enumerable.ToList(
                        System.Linq.Enumerable.Where(
                            planesDisponibles,
                            p => dialog.PlanesPreventivosSeleccionados.Contains(p.Id)));
                }
            }

            return result;
        }

        private void ShowDetallesDialog(Models.DTOs.EjecucionMantenimientoDto dto)
        {
            var dialog = new DetalleCorrectivoDialog(dto)
            {
                Owner = System.Windows.Application.Current?.Windows.Count > 0
                    ? System.Windows.Application.Current.Windows[0]
                    : System.Windows.Application.Current?.MainWindow
            };

            dialog.ShowDialog();
        }
    }
}
