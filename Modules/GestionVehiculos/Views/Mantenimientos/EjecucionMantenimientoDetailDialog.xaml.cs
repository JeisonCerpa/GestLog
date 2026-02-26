using System.Windows;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

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
                // save request
                if (DataContext is EjecucionMantenimientoDto ejec)
                {
                    SaveRequested?.Invoke(ejec);
                }
                IsEditing = false;
            }
        }

        private void BtnCancelFooter_Click(object sender, RoutedEventArgs e)
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
            if (BtnEditFooter != null)
            {
                BtnEditFooter.Content = IsEditing ? "Guardar" : "Editar";
            }
            if (BtnCancelFooter != null)
            {
                BtnCancelFooter.Visibility = IsEditing ? Visibility.Visible : Visibility.Collapsed;
            }
            if (BtnDeleteFooter != null)
            {
                BtnDeleteFooter.IsEnabled = !IsEditing;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EjecucionMantenimientoDto ejec)
            {
                DeleteRequested?.Invoke(ejec);
            }
        }
    }
}