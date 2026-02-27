using System.Globalization;
using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

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
            _ejecucion.ObservacionesTecnico = string.IsNullOrWhiteSpace(TxtTimeline.Text) ? null : TxtTimeline.Text.Trim();
            _ejecucion.RutaFactura = string.IsNullOrWhiteSpace(TxtFactura.Text) || TxtFactura.Text.Trim().Equals("N/D")
                ? null
                : TxtFactura.Text.Trim();

            if (decimal.TryParse(TxtCosto.Text?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedCosto) && parsedCosto >= 0)
            {
                _ejecucion.Costo = parsedCosto;
            }
            else if (decimal.TryParse(TxtCosto.Text?.Trim(), out parsedCosto) && parsedCosto >= 0)
            {
                _ejecucion.Costo = parsedCosto;
            }
            else
            {
                _ejecucion.Costo = null;
            }

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

        private void BtnAttachFactura_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditing)
            {
                return;
            }

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar factura (PDF o imagen)",
                Filter = "Archivos PDF/Imagen|*.pdf;*.png;*.jpg;*.jpeg|PDF (*.pdf)|*.pdf|Imagen (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Todos los archivos (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dlg.ShowDialog(this) == true)
            {
                TxtFactura.Text = dlg.FileName;
            }
        }

        private void FillFields(EjecucionMantenimientoDto dto)
        {
            TxtTitulo.Text = dto.TituloActividad ?? "Detalle correctivo";
            TxtSubtitulo.Text = $"Placa: {dto.PlacaVehiculo}";
            TxtEstado.Text = dto.EstadoCorrectivoTexto;
            TxtFecha.Text = dto.FechaEjecucion.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            TxtResponsable.Text = string.IsNullOrWhiteSpace(dto.ResponsableEjecucion) ? "" : dto.ResponsableEjecucion;
            TxtProveedor.Text = string.IsNullOrWhiteSpace(dto.Proveedor) ? "" : dto.Proveedor;
            TxtCosto.Text = dto.Costo.HasValue ? dto.Costo.Value.ToString(CultureInfo.CurrentCulture) : string.Empty;
            TxtFactura.Text = string.IsNullOrWhiteSpace(dto.RutaFactura) ? "" : dto.RutaFactura;
            TxtTimeline.Text = dto.ObservacionesTecnico ?? string.Empty;
        }

        private void SetEditing(bool value)
        {
            _isEditing = value;
            TxtResponsable.IsReadOnly = !value;
            TxtProveedor.IsReadOnly = !value;
            TxtCosto.IsReadOnly = !value;
            TxtFactura.IsReadOnly = !value;
            TxtTimeline.IsReadOnly = !value;
            BtnAttachFactura.IsEnabled = value;

            BtnEdit.Content = value ? "Guardar" : "Editar";
            BtnCancel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
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
