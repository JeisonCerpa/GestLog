using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ComponentModel;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
using GestLog.Modules.GestionVehiculos.Services.Utilities;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class RegistroCorrectivoDialog : Window
    {
        private sealed class GastoItemInput : INotifyPropertyChanged
        {
            private int _tipoGasto = 4;
            private string _descripcion = string.Empty;
            private string _proveedor = string.Empty;
            private string _valorInput = string.Empty;
            private string _numeroFactura = string.Empty;
            private string _rutaFactura = string.Empty;

            public event PropertyChangedEventHandler? PropertyChanged;

            public int TipoGasto
            {
                get => _tipoGasto;
                set { if (_tipoGasto == value) return; _tipoGasto = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TipoGasto))); }
            }

            public string Descripcion
            {
                get => _descripcion;
                set { if (_descripcion == value) return; _descripcion = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Descripcion))); }
            }

            public string Proveedor
            {
                get => _proveedor;
                set { if (_proveedor == value) return; _proveedor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Proveedor))); }
            }

            public string ValorInput
            {
                get => _valorInput;
                set { if (_valorInput == value) return; _valorInput = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValorInput))); }
            }

            public string NumeroFactura
            {
                get => _numeroFactura;
                set { if (_numeroFactura == value) return; _numeroFactura = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumeroFactura))); }
            }

            public string RutaFactura
            {
                get => _rutaFactura;
                set { if (_rutaFactura == value) return; _rutaFactura = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RutaFactura))); }
            }
        }

        private sealed class GastoFacturaGroup : INotifyPropertyChanged
        {
            private decimal _totalGrupo;
            
            public event PropertyChangedEventHandler? PropertyChanged;

            public string NumeroFactura { get; set; } = string.Empty;
            public string ProveedorFactura { get; set; } = string.Empty;
            public string RutaFactura { get; set; } = string.Empty;
            public ObservableCollection<GastoItemInput> Items { get; } = new();

            public decimal TotalGrupo
            {
                get => _totalGrupo;
                set
                {
                    if (_totalGrupo == value) return;
                    _totalGrupo = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalGrupo)));
                }
            }

            public string DisplayKey => string.IsNullOrWhiteSpace(NumeroFactura) ? "SIN-FACTURA" : NumeroFactura;
        }

        private readonly CorrectivosMantenimientoViewModel _viewModel;
        private readonly ObservableCollection<GastoFacturaGroup> _itemsGasto = new();

        public RegistroCorrectivoDialog(CorrectivosMantenimientoViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            IcItemsGasto.ItemsSource = _itemsGasto;
            UpdateCartSummary();

            _viewModel.RegistroCorrectivoExitoso += OnRegistroCorrectivoExitoso;

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

        private void OnRegistroCorrectivoExitoso()
        {
            Dispatcher.Invoke(() =>
            {
                DialogResult = true;
                Close();
            });
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RegistroItemsGasto = BuildItemsGasto();
            await _viewModel.RegistrarCorrectivoAsync();
        }

        private void BtnAddGastoItem_Click(object sender, RoutedEventArgs e)
        {
            var item = new GastoItemInput();
            item.PropertyChanged += GastoItem_PropertyChanged;
            AddItemToGroup(item);
            UpdateCartSummary();
        }

        private void BtnAddFacturaGroup_Click(object sender, RoutedEventArgs e)
        {
            var newGroup = new GastoFacturaGroup { NumeroFactura = string.Empty, ProveedorFactura = string.Empty, RutaFactura = string.Empty };
            var item = new GastoItemInput();
            item.PropertyChanged += GastoItem_PropertyChanged;
            newGroup.Items.Add(item);
            _itemsGasto.Add(newGroup);
            UpdateCartSummary();
        }

        private void AddItemToGroup(GastoItemInput item)
        {
            var groupKey = "SIN-FACTURA";
            var group = _itemsGasto.FirstOrDefault(g => g.DisplayKey == groupKey);
            
            if (group == null)
            {
                group = new GastoFacturaGroup { NumeroFactura = string.Empty, ProveedorFactura = string.Empty, RutaFactura = string.Empty };
                _itemsGasto.Add(group);
            }

            group.Items.Add(item);
            UpdateGroupTotal(group);
        }

        private void UpdateGroupTotal(GastoFacturaGroup group)
        {
            decimal total = 0m;
            foreach (var item in group.Items)
            {
                var valorValido = decimal.TryParse(item.ValorInput?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor)
                    || decimal.TryParse(item.ValorInput?.Trim(), out valor);

                if (valorValido && valor > 0)
                {
                    total += decimal.Round(valor, 2);
                }
            }

            group.TotalGrupo = decimal.Round(total, 2);
        }

        private void BtnRemoveGastoItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GastoItemInput item)
            {
                item.PropertyChanged -= GastoItem_PropertyChanged;
                
                var group = _itemsGasto.FirstOrDefault(g => g.Items.Contains(item));
                if (group != null)
                {
                    group.Items.Remove(item);
                    UpdateGroupTotal(group);
                    
                    if (group.Items.Count == 0)
                    {
                        _itemsGasto.Remove(group);
                    }
                }
                
                UpdateCartSummary();
            }
        }

        private List<EjecucionMantenimientoItemGastoDto> BuildItemsGasto()
        {
            var items = new List<EjecucionMantenimientoItemGastoDto>();

            foreach (var group in _itemsGasto)
            {
                var numeroFacturaGrupo = (group.NumeroFactura ?? string.Empty).Trim();
                var rutaFacturaGrupo = (group.RutaFactura ?? string.Empty).Trim();

                foreach (var item in group.Items)
                {
                    var descripcion = (item.Descripcion ?? string.Empty).Trim();
                    var proveedor = (item.Proveedor ?? string.Empty).Trim();
                    
                    var numeroFactura = string.IsNullOrWhiteSpace(item.NumeroFactura) 
                        ? numeroFacturaGrupo 
                        : (item.NumeroFactura ?? string.Empty).Trim();
                    
                    var rutaFactura = string.IsNullOrWhiteSpace(item.RutaFactura) 
                        ? rutaFacturaGrupo 
                        : (item.RutaFactura ?? string.Empty).Trim();

                    var valorValido = decimal.TryParse(item.ValorInput?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor)
                        || decimal.TryParse(item.ValorInput?.Trim(), out valor);

                    if (!valorValido)
                    {
                        valor = 0m;
                    }

                    if (string.IsNullOrWhiteSpace(descripcion) && valor <= 0 && string.IsNullOrWhiteSpace(proveedor) && string.IsNullOrWhiteSpace(rutaFactura))
                    {
                        continue;
                    }

                    items.Add(new EjecucionMantenimientoItemGastoDto
                    {
                        TipoGasto = item.TipoGasto,
                        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? "Ítem de correctivo" : descripcion,
                        Proveedor = string.IsNullOrWhiteSpace(proveedor) ? null : proveedor,
                        Valor = valor < 0 ? 0m : decimal.Round(valor, 2),
                        NumeroFactura = string.IsNullOrWhiteSpace(numeroFactura) ? null : numeroFactura,
                        RutaFactura = string.IsNullOrWhiteSpace(rutaFactura) ? null : rutaFactura,
                        FechaDocumento = _viewModel.RegistroFechaEjecucion.HasValue
                            ? new System.DateTimeOffset(_viewModel.RegistroFechaEjecucion.Value.Date)
                            : System.DateTimeOffset.Now
                    });
                }
            }

            return items;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnAttachFactura_Click(object sender, RoutedEventArgs e)
        {
            async Task AdjuntarFacturaAsync()
            {
                var uploaded = await FacturaStorageHelper.PickAndUploadFacturaAsync(this, "factura_correctivo");
                if (!string.IsNullOrWhiteSpace(uploaded))
                {
                    _viewModel.RegistroRutaFactura = uploaded;
                }
            }

            _ = AdjuntarFacturaAsync();
        }

        private async void BtnAttachSharedInvoice_Click(object sender, RoutedEventArgs e)
        {
            var uploaded = await FacturaStorageHelper.PickAndUploadFacturaAsync(this, "factura_correctivo");
            if (!string.IsNullOrWhiteSpace(uploaded))
            {
                TxtSharedInvoicePath.Text = uploaded;
            }
        }

        private void BtnApplySharedInvoice_Click(object sender, RoutedEventArgs e)
        {
            var numero = TxtSharedInvoiceNumber.Text?.Trim() ?? string.Empty;
            var ruta = TxtSharedInvoicePath.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(numero) && string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            foreach (var group in _itemsGasto)
            {
                if (string.IsNullOrWhiteSpace(group.NumeroFactura) && !string.IsNullOrWhiteSpace(numero))
                {
                    group.NumeroFactura = numero;
                }

                if (string.IsNullOrWhiteSpace(group.RutaFactura) && !string.IsNullOrWhiteSpace(ruta))
                {
                    group.RutaFactura = ruta;
                }
            }

            IcItemsGasto.Items.Refresh();
            UpdateCartSummary();
        }

        private void GastoItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GastoItemInput.ValorInput)
                || e.PropertyName == nameof(GastoItemInput.NumeroFactura)
                || e.PropertyName == nameof(GastoItemInput.RutaFactura))
            {
                UpdateCartSummary();
            }
        }

        private void UpdateCartSummary()
        {
            var total = 0m;
            var totalItems = 0;
            
            foreach (var group in _itemsGasto)
            {
                foreach (var item in group.Items)
                {
                    totalItems++;
                    if (decimal.TryParse(item.ValorInput?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var val)
                        || decimal.TryParse(item.ValorInput?.Trim(), out val))
                    {
                        if (val > 0)
                        {
                            total += decimal.Round(val, 2);
                        }
                    }
                }
            }

            TxtCartItemsCount.Text = totalItems.ToString(CultureInfo.InvariantCulture);
            TxtCartItemsTotal.Text = $"$ {decimal.Round(total, 2):N0}";
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel.RegistroCorrectivoExitoso -= OnRegistroCorrectivoExitoso;
            base.OnClosed(e);
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
