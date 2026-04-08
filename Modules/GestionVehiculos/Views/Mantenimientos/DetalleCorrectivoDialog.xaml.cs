using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Services.Utilities;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class DetalleCorrectivoDialog : Window
    {
        public event System.Action<EjecucionMantenimientoDto>? SaveRequested;

        private readonly EjecucionMantenimientoDto _ejecucion;
        private EjecucionMantenimientoDto? _backup;
        private bool _isEditing;

        public DetalleCorrectivoDialog(EjecucionMantenimientoDto dto)
        {
            InitializeComponent();
            _ejecucion = dto;

            FillFields(dto);
            SetEditing(false);

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

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditing)
            {
                _backup = new EjecucionMantenimientoDto
                {
                    ResponsableEjecucion = _ejecucion.ResponsableEjecucion,
                    Proveedor = _ejecucion.Proveedor,
                    Costo = _ejecucion.Costo,
                    RutaFactura = _ejecucion.RutaFactura,
                    ObservacionesTecnico = _ejecucion.ObservacionesTecnico
                };

                SetEditing(true);
                return;
            }

            _ejecucion.ResponsableEjecucion = string.IsNullOrWhiteSpace(TxtResponsable.Text) ? null : TxtResponsable.Text.Trim();
            _ejecucion.Proveedor = string.IsNullOrWhiteSpace(TxtProveedor.Text) ? null : TxtProveedor.Text.Trim();
            var observacionesLimpias = CleanObservaciones(dtoText: TxtTimeline.Text);
            _ejecucion.ObservacionesTecnico = string.IsNullOrWhiteSpace(observacionesLimpias) ? null : observacionesLimpias;

            _ejecucion.Costo = TryParseCosto(TxtCosto.Text);

            SaveRequested?.Invoke(_ejecucion);
            FillFields(_ejecucion);
            SetEditing(false);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_backup != null)
            {
                _ejecucion.ResponsableEjecucion = _backup.ResponsableEjecucion;
                _ejecucion.Proveedor = _backup.Proveedor;
                _ejecucion.Costo = _backup.Costo;
                _ejecucion.RutaFactura = _backup.RutaFactura;
                _ejecucion.ObservacionesTecnico = _backup.ObservacionesTecnico;
                FillFields(_ejecucion);
            }

            SetEditing(false);
        }
        private async void BtnAttachFactura_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditing)
            {
                return;
            }

            var uploaded = await FacturaStorageHelper.PickAndUploadFacturaAsync(this, $"factura_correctivo_{_ejecucion.PlacaVehiculo}");
            if (!string.IsNullOrWhiteSpace(uploaded))
            {
                _ejecucion.RutaFactura = uploaded;
                TxtFactura.Text = GetFacturaDisplayName(uploaded);
            }
        }

        private async void BtnOpenFactura_Click(object sender, RoutedEventArgs e)
        {
            await FacturaStorageHelper.OpenFacturaAsync(this, _ejecucion.RutaFactura);
        }

        private void FillFields(EjecucionMantenimientoDto dto)
        {
            TxtTitulo.Text = dto.TituloActividad ?? "Detalle correctivo";
            TxtSubtitulo.Text = $"Placa: {dto.PlacaVehiculo}";
            TxtEstadoStatus.Text = dto.EstadoCorrectivoTexto;
            TxtFecha.Text = dto.FechaEjecucion.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            TxtResponsable.Text = string.IsNullOrWhiteSpace(dto.ResponsableEjecucion) ? "" : dto.ResponsableEjecucion;
            TxtProveedor.Text = string.IsNullOrWhiteSpace(dto.Proveedor) ? "" : dto.Proveedor;
            TxtCosto.Text = dto.Costo.HasValue ? FormatCostoCop(dto.Costo.Value) : string.Empty;
            TxtFactura.Text = GetFacturaDisplayName(dto.RutaFactura);
            TxtTimeline.Text = CleanObservaciones(dto.ObservacionesTecnico);
            DgItemsGasto.ItemsSource = dto.ItemsGasto;
        }

        private void SetEditing(bool value)
        {
            _isEditing = value;
            TxtResponsable.IsReadOnly = !value;
            TxtProveedor.IsReadOnly = !value;
            TxtCosto.IsReadOnly = !value;
            TxtFactura.IsReadOnly = true;
            TxtTimeline.IsReadOnly = !value;
            BtnAttachFactura.IsEnabled = value;

            BtnEdit.Content = value ? "Guardar" : "Editar";
            BtnCancel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string GetFacturaDisplayName(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return string.Empty;
            }

            return Path.GetFileName(ruta);
        }

        private static string FormatCostoCop(decimal value)
        {
            return $"{value.ToString("N2", CultureInfo.CreateSpecificCulture("es-CO"))} COP";
        }

        private static decimal? TryParseCosto(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var value = input.Trim().Replace("COP", string.Empty, System.StringComparison.OrdinalIgnoreCase).Trim();

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CreateSpecificCulture("es-CO"), out var parsed) && parsed >= 0)
            {
                return parsed;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed) && parsed >= 0)
            {
                return parsed;
            }

            if (decimal.TryParse(value, out parsed) && parsed >= 0)
            {
                return parsed;
            }

            return null;
        }

        private static string CleanObservaciones(string? dtoText)
        {
            if (string.IsNullOrWhiteSpace(dtoText))
            {
                return string.Empty;
            }

            var lines = dtoText
                .Replace("\r\n", "\n")
                .Split('\n')
                .Where(line =>
                {
                    var trimmed = line.TrimStart();
                    return !trimmed.StartsWith("Factura:", System.StringComparison.OrdinalIgnoreCase);
                })
                .ToArray();

            return string.Join(System.Environment.NewLine, lines).Trim();
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
