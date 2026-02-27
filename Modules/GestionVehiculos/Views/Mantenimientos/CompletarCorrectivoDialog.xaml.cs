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
            public bool IsCustomCost { get; set; }
            public string CustomCostInput { get; set; } = string.Empty;
            public bool IsOutsideCorrectivoInvoice { get; set; }
        }

        public class PlanPreventivoCostoAsignado
        {
            public int PlanId { get; set; }
            public decimal CostoAsignado { get; set; }
            public bool EsCostoPersonalizado { get; set; }
            public bool IsOutsideCorrectivoInvoice { get; set; }
        }

        private readonly ObservableCollection<PlanPreventivoSeleccionItem> _planes = new();

        public long? KilometrajeAlCompletar { get; private set; }
        public string Responsable { get; private set; }
        public string Proveedor { get; private set; }
        public decimal? Costo { get; private set; }
        public string RutaFactura { get; private set; }
        public string Observaciones { get; private set; }
        public IReadOnlyCollection<int> PlanesPreventivosSeleccionados { get; private set; } = Array.Empty<int>();
        public IReadOnlyCollection<PlanPreventivoCostoAsignado> PlanesPreventivosConCosto { get; private set; } = Array.Empty<PlanPreventivoCostoAsignado>();

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

            var planesSeleccionados = _planes.Where(p => p.IsSelected).ToList();
            if (planesSeleccionados.Count > 0)
            {
                var asignaciones = new List<PlanPreventivoCostoAsignado>();
                decimal sumaPersonalizados = 0m;
                var sinPersonalizar = new List<PlanPreventivoSeleccionItem>();

                foreach (var plan in planesSeleccionados)
                {
                    if (plan.IsCustomCost)
                    {
                        if (!decimal.TryParse(plan.CustomCostInput?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var costoPlan) || costoPlan < 0)
                        {
                            if (!decimal.TryParse(plan.CustomCostInput?.Trim(), out costoPlan) || costoPlan < 0)
                            {
                                TxtError.Text = $"Costo personalizado inválido para '{plan.Nombre}'.";
                                return;
                            }
                        }

                        sumaPersonalizados += costoPlan;
                        asignaciones.Add(new PlanPreventivoCostoAsignado
                        {
                            PlanId = plan.PlanId,
                            CostoAsignado = decimal.Round(costoPlan, 2),
                            EsCostoPersonalizado = true,
                            IsOutsideCorrectivoInvoice = plan.IsOutsideCorrectivoInvoice
                        });
                    }
                    else
                    {
                        sinPersonalizar.Add(plan);
                    }
                }

                if (sinPersonalizar.Count > 0)
                {
                    if (!Costo.HasValue)
                    {
                        TxtError.Text = "Si hay planes sin costo personalizado, debes ingresar costo del correctivo para prorratear.";
                        return;
                    }

                    var restante = Costo.Value - sumaPersonalizados;
                    if (restante < 0)
                    {
                        TxtError.Text = "La suma de costos personalizados supera el costo total del correctivo.";
                        return;
                    }

                    var prorrateado = decimal.Round(restante / sinPersonalizar.Count, 2);
                    foreach (var plan in sinPersonalizar)
                    {
                        asignaciones.Add(new PlanPreventivoCostoAsignado
                        {
                            PlanId = plan.PlanId,
                            CostoAsignado = prorrateado,
                            EsCostoPersonalizado = false,
                            IsOutsideCorrectivoInvoice = false
                        });
                    }
                }

                PlanesPreventivosConCosto = asignaciones;
            }
            else
            {
                PlanesPreventivosConCosto = Array.Empty<PlanPreventivoCostoAsignado>();
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
