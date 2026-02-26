using System.Globalization;
using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class DetalleCorrectivoDialog : Window
    {
        public DetalleCorrectivoDialog(EjecucionMantenimientoDto dto)
        {
            InitializeComponent();

            TxtTitulo.Text = dto.TituloActividad ?? "Detalle correctivo";
            TxtSubtitulo.Text = $"Placa: {dto.PlacaVehiculo}";
            TxtEstado.Text = dto.EstadoCorrectivoTexto;
            TxtFecha.Text = dto.FechaEjecucion.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            TxtResponsable.Text = dto.ResponsableEjecucion ?? "N/D";
            TxtProveedor.Text = dto.Proveedor ?? "N/D";
            TxtCosto.Text = dto.Costo.HasValue ? dto.Costo.Value.ToString("C2", CultureInfo.CurrentCulture) : "N/D";
            TxtFactura.Text = string.IsNullOrWhiteSpace(dto.RutaFactura) ? "N/D" : dto.RutaFactura;
            TxtTimeline.Text = dto.ObservacionesTecnico ?? string.Empty;

            ConfigurarParaVentanaPadre(System.Windows.Application.Current?.MainWindow);

            KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ConfigurarParaVentanaPadre(Window? parentWindow)
        {
            if (parentWindow == null)
            {
                return;
            }

            Owner = parentWindow;
            ShowInTaskbar = false;
            WindowState = WindowState.Maximized;

            Loaded += (_, __) =>
            {
                if (Owner == null)
                {
                    return;
                }

                Owner.LocationChanged += (_, __) =>
                {
                    if (WindowState != WindowState.Maximized)
                    {
                        WindowState = WindowState.Maximized;
                    }
                };
                Owner.SizeChanged += (_, __) =>
                {
                    if (WindowState != WindowState.Maximized)
                    {
                        WindowState = WindowState.Maximized;
                    }
                };
            };
        }
    }
}
