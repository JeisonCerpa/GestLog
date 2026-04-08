using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Services.Utilities;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    /// <summary>
    /// Interaction logic for EjecucionMantenimientoDetailDialog.xaml
    /// </summary>
    public partial class EjecucionMantenimientoDetailDialog : Window, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.Action<EjecucionMantenimientoDto>? SaveRequested;
        public event System.Action<EjecucionMantenimientoDto>? DeleteRequested;

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
                    UpdateFooterButtons();
                }
            }
        }

        private EjecucionMantenimientoDto? _backupCopy;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public EjecucionMantenimientoDetailDialog(EjecucionMantenimientoDto ejecucion)
        {
            InitializeComponent();
            DataContext = ejecucion;
            ConfigurarParaVentanaPadre(System.Windows.Application.Current?.MainWindow);

            KeyDown += (_, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    if (IsEditing)
                        CancelEdit();
                    else
                        this.Close();
                }
            };

            UpdateFooterButtons();
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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditing)
            {
                // first click: user intends to edit
                if (DataContext is EjecucionMantenimientoDto ejec)
                {
                    _backupCopy = new EjecucionMantenimientoDto
                    {
                        Id = ejec.Id,
                        PlacaVehiculo = ejec.PlacaVehiculo,
                        PlanMantenimientoId = ejec.PlanMantenimientoId,
                        FechaEjecucion = ejec.FechaEjecucion,
                        KMAlMomento = ejec.KMAlMomento,
                        ObservacionesTecnico = ejec.ObservacionesTecnico,
                        Costo = ejec.Costo,
                        RutaFactura = ejec.RutaFactura,
                        ResponsableEjecucion = ejec.ResponsableEjecucion,
                        Proveedor = ejec.Proveedor,
                        Estado = ejec.Estado,
                        FechaRegistro = ejec.FechaRegistro,
                        FechaActualizacion = ejec.FechaActualizacion
                    };
                }
                IsEditing = true;
            }
            else
            {
                // save request - limpiar observaciones antes de guardar
                if (DataContext is EjecucionMantenimientoDto ejec)
                {
                    ejec.ObservacionesTecnico = CleanObservaciones(ejec.ObservacionesTecnico);
                    SaveRequested?.Invoke(ejec);
                }
                IsEditing = false;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelEdit();
        }

        private void CancelEdit()
        {
            if (_backupCopy != null && DataContext is EjecucionMantenimientoDto original)
            {
                // restore fields
                original.PlacaVehiculo = _backupCopy.PlacaVehiculo;
                original.PlanMantenimientoId = _backupCopy.PlanMantenimientoId;
                original.FechaEjecucion = _backupCopy.FechaEjecucion;
                original.KMAlMomento = _backupCopy.KMAlMomento;
                original.ObservacionesTecnico = _backupCopy.ObservacionesTecnico;
                original.Costo = _backupCopy.Costo;
                original.RutaFactura = _backupCopy.RutaFactura;
                original.ResponsableEjecucion = _backupCopy.ResponsableEjecucion;
                original.Proveedor = _backupCopy.Proveedor;
                original.Estado = _backupCopy.Estado;
                original.FechaRegistro = _backupCopy.FechaRegistro;
                original.FechaActualizacion = _backupCopy.FechaActualizacion;
            }
            IsEditing = false;
        }

        private void UpdateFooterButtons()
        {
            // Los botones están vinculados directamente en XAML, no necesitan actualización
            // El estado de edición se maneja a través de binding a IsEditing
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EjecucionMantenimientoDto ejec)
            {
                DeleteRequested?.Invoke(ejec);
            }
        }

        private async void BtnAttachFactura_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditing || DataContext is not EjecucionMantenimientoDto ejec)
            {
                return;
            }

            var uploaded = await FacturaStorageHelper.PickAndUploadFacturaAsync(this, $"factura_preventivo_{ejec.PlacaVehiculo}");
            if (!string.IsNullOrWhiteSpace(uploaded))
            {
                ejec.RutaFactura = uploaded;
            }
        }

        private async void BtnOpenFactura_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not EjecucionMantenimientoDto ejec)
            {
                return;
            }

            await FacturaStorageHelper.OpenFacturaAsync(this, ejec.RutaFactura);
        }

        /// <summary>
        /// Formatea un decimal a formato COP con separador de miles (es-CO culture).
        /// Ejemplo: 150000 → "150.000,00 COP"
        /// </summary>
        private string FormatCostoCop(decimal value)
        {
            return $"{value.ToString("N2", CultureInfo.CreateSpecificCulture("es-CO"))} COP";
        }

        /// <summary>
        /// Intenta parsear un string de costo que puede contener "COP".
        /// Soporta múltiples culturas (es-CO, InvariantCulture, default).
        /// Retorna null si no puede parsearse o es negativo.
        /// </summary>
        private decimal? TryParseCosto(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Remover sufijo " COP" si existe
            string cleaned = input.Replace(" COP", "").Trim();

            // Intentar parsear con cultura es-CO primero
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.CreateSpecificCulture("es-CO"), out decimal resultEsco))
                return resultEsco < 0 ? null : resultEsco;

            // Intentar con InvariantCulture
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultInvariant))
                return resultInvariant < 0 ? null : resultInvariant;

            // Intentar con cultura default
            if (decimal.TryParse(cleaned, out decimal resultDefault))
                return resultDefault < 0 ? null : resultDefault;

            return null;
        }

        /// <summary>
        /// Limpia observaciones técnicas removiendo líneas que comienzan con "Factura:".
        /// Útil para ocultar rutas largas de archivo que se agregaban automáticamente.
        /// </summary>
        private string CleanObservaciones(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var lines = input.Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.None);
            var cleanedLines = lines.Where(line => !line.TrimStart().StartsWith("Factura:", StringComparison.OrdinalIgnoreCase)).ToList();

            return string.Join(Environment.NewLine, cleanedLines).Trim();
        }
    }
}