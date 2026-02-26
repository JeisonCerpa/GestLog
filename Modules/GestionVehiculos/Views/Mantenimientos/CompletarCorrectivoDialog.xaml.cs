using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class CompletarCorrectivoDialog : Window
    {
        public class PlanPreventivoSeleccionItem
        {
            public int PlanId { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
        }

        private readonly ObservableCollection<PlanPreventivoSeleccionItem> _planes = new();

        public long? KilometrajeAlCompletar { get; private set; }
        public string Responsable { get; private set; }
        public string Proveedor { get; private set; }
        public decimal? Costo { get; private set; }
        public string RutaFactura { get; private set; }
        public string Observaciones { get; private set; }
        public IReadOnlyCollection<int> PlanesPreventivosSeleccionados { get; private set; } = Array.Empty<int>();

        public CompletarCorrectivoDialog(
            long? kilometrajeInicial,
            string? responsableInicial,
            string? proveedorInicial,
            decimal? costoInicial,
            string? rutaFacturaInicial,
            string? observacionesInicial,
            IEnumerable<PlanMantenimientoVehiculoDto>? planesPreventivosDisponibles)
        {
            InitializeComponent();

            KilometrajeAlCompletar = kilometrajeInicial;
            Responsable = responsableInicial?.Trim() ?? string.Empty;
            Proveedor = proveedorInicial?.Trim() ?? string.Empty;
            Costo = costoInicial;
            RutaFactura = rutaFacturaInicial?.Trim() ?? string.Empty;
            Observaciones = observacionesInicial?.Trim() ?? string.Empty;

            if (planesPreventivosDisponibles != null)
            {
                foreach (var plan in planesPreventivosDisponibles)
                {
                    _planes.Add(new PlanPreventivoSeleccionItem
                    {
                        PlanId = plan.Id,
                        Nombre = plan.PlantillaNombre ?? $"Plan #{plan.Id}",
                        Estado = string.IsNullOrWhiteSpace(plan.EstadoPlan) ? "Sin estado" : plan.EstadoPlan,
                        IsSelected = false
                    });
                }
            }

            TxtKilometraje.Text = KilometrajeAlCompletar?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
            TxtResponsable.Text = Responsable;
            TxtProveedor.Text = Proveedor;
            TxtCosto.Text = Costo?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
            TxtRutaFactura.Text = RutaFactura;
            TxtObservaciones.Text = Observaciones;
            LstPlanesPreventivos.ItemsSource = _planes;

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

        private void BtnAttachFactura_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar factura (PDF o imagen)",
                Filter = "Archivos PDF/Imagen|*.pdf;*.png;*.jpg;*.jpeg|PDF (*.pdf)|*.pdf|Imagen (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Todos los archivos (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dlg.ShowDialog(this) == true)
            {
                TxtRutaFactura.Text = dlg.FileName;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!long.TryParse(TxtKilometraje.Text?.Trim(), out var km) || km <= 0)
            {
                TxtError.Text = "Debe ingresar un kilometraje válido.";
                return;
            }

            KilometrajeAlCompletar = km;
            Responsable = TxtResponsable.Text?.Trim() ?? string.Empty;
            Proveedor = TxtProveedor.Text?.Trim() ?? string.Empty;
            Observaciones = TxtObservaciones.Text?.Trim() ?? string.Empty;
            RutaFactura = TxtRutaFactura.Text?.Trim() ?? string.Empty;
            PlanesPreventivosSeleccionados = _planes.Where(p => p.IsSelected).Select(p => p.PlanId).Distinct().ToList();

            if (decimal.TryParse(TxtCosto.Text?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedCosto) && parsedCosto >= 0)
            {
                Costo = parsedCosto;
            }
            else if (decimal.TryParse(TxtCosto.Text?.Trim(), out parsedCosto) && parsedCosto >= 0)
            {
                Costo = parsedCosto;
            }
            else
            {
                Costo = null;
            }

            TxtError.Text = string.Empty;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
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
