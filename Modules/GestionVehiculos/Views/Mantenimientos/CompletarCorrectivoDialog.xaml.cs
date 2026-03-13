using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls; // needed for Grid, Orientation, etc
// aliases used by plan note editor dialog construction
using WpfGrid = System.Windows.Controls.Grid;
using WpfRowDefinition = System.Windows.Controls.RowDefinition;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class CompletarCorrectivoDialog : Window
    {
        private readonly IEjecucionMantenimientoService? _ejecucionService;

        public class GastoItemInput : INotifyPropertyChanged
        {
            private int _tipoGasto = (int)GestLog.Modules.GestionVehiculos.Models.Enums.TipoGastoMantenimientoVehiculo.Otro;
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

        public class GastoFacturaGroup : INotifyPropertyChanged
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

        public class PlanPreventivoCostoAsignado
        {
            public int PlanId { get; set; }
            public decimal CostoAsignado { get; set; }
            public bool EsCostoPersonalizado { get; set; }
            // ruta de la factura asociada al plan (solo si se personalizó el costo)
            public string FacturaRuta { get; set; } = string.Empty;
            // campos opcionales heredados de "editar nota"
            public string DetalleOpcional { get; set; } = string.Empty;
            public string ProveedorOpcional { get; set; } = string.Empty;
            public string RutaFacturaOpcional { get; set; } = string.Empty;
            public string CostoOpcionalInput { get; set; } = string.Empty;
        }

        public class PlanPreventivoSeleccionItem
        {
            public int PlanId { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
            public bool IsCustomCost { get; set; }
            public string CustomCostInput { get; set; } = string.Empty;
            // si el costo es personalizado, se adjunta aquí la factura correspondiente
            public string InvoicePath { get; set; } = string.Empty;
            // campos para la edición de nota (igual que en RegistroPreventivo)
            public string DetalleOpcional { get; set; } = string.Empty;
            public string ProveedorOpcional { get; set; } = string.Empty;
            public string RutaFacturaOpcional { get; set; } = string.Empty;
            public string CostoOpcionalInput { get; set; } = string.Empty;
            public bool HasDetalleOpcional => !string.IsNullOrWhiteSpace(DetalleOpcional);
        }

        private readonly ObservableCollection<PlanPreventivoSeleccionItem> _planes = new();
        private readonly ObservableCollection<GastoFacturaGroup> _itemsGasto = new();

        public long? KilometrajeAlCompletar { get; private set; }
        public string Responsable { get; private set; }
        public string Proveedor { get; private set; }
        public decimal? Costo { get; private set; }
        public string RutaFactura { get; private set; }
        public string Observaciones { get; private set; }
        public string TituloActividad { get; private set; }
        public IReadOnlyCollection<int> PlanesPreventivosSeleccionados { get; private set; } = Array.Empty<int>();
        public IReadOnlyCollection<PlanPreventivoCostoAsignado> PlanesPreventivosConCosto { get; private set; } = Array.Empty<PlanPreventivoCostoAsignado>();
        public IReadOnlyCollection<EjecucionMantenimientoItemGastoDto> ItemsGasto { get; private set; } = Array.Empty<EjecucionMantenimientoItemGastoDto>();

        public CompletarCorrectivoDialog(
            long? kilometrajeInicial,
            string? responsableInicial,
            string? proveedorInicial,
            decimal? costoInicial,
            string? rutaFacturaInicial,
            string? observacionesInicial,
            string? tituloActividadInicial,
            IEnumerable<PlanMantenimientoVehiculoDto>? planesPreventivosDisponibles,
            IEnumerable<EjecucionMantenimientoItemGastoDto>? itemsGastoIniciales = null)
        {
            InitializeComponent();

            _ejecucionService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService<IEjecucionMantenimientoService>();

            KilometrajeAlCompletar = kilometrajeInicial;
            Responsable = responsableInicial?.Trim() ?? string.Empty;
            Proveedor = proveedorInicial?.Trim() ?? string.Empty;
            Costo = costoInicial;
            RutaFactura = rutaFacturaInicial?.Trim() ?? string.Empty;
            Observaciones = observacionesInicial?.Trim() ?? string.Empty;
            TituloActividad = tituloActividadInicial?.Trim() ?? string.Empty;

            if (planesPreventivosDisponibles != null)
            {
                foreach (var plan in planesPreventivosDisponibles)
                {
                    _planes.Add(new PlanPreventivoSeleccionItem
                    {
                        PlanId = plan.Id,
                        Nombre = plan.PlantillaNombre ?? $"Plan #{plan.Id}",
                        Estado = string.IsNullOrWhiteSpace(plan.EstadoPlan) ? "Sin estado" : plan.EstadoPlan,
                        IsSelected = false,
                        IsCustomCost = false,
                        CustomCostInput = string.Empty,
                        InvoicePath = string.Empty,
                        DetalleOpcional = string.Empty,
                        ProveedorOpcional = string.Empty,
                        RutaFacturaOpcional = string.Empty,
                        CostoOpcionalInput = string.Empty
                    });
                }
            }

            // bind the wrap‑panel list to our private collection
            IcPlanesPreventivos.ItemsSource = _planes;

            TxtKilometraje.Text = KilometrajeAlCompletar?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
            CmbResponsable.Text = Responsable;
            CmbProveedor.Text = Proveedor;
            TxtCosto.Text = Costo?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
            TxtRutaFactura.Text = RutaFactura;
            TxtObservaciones.Text = Observaciones;
            TxtTituloActividad.Text = TituloActividad;

            if (itemsGastoIniciales != null)
            {
                // Agrupar items iniciales por factura
                var groupsByFactura = itemsGastoIniciales
                    .GroupBy(item => string.IsNullOrWhiteSpace(item.NumeroFactura) ? "SIN-FACTURA" : item.NumeroFactura.Trim())
                    .ToList();

                foreach (var grouping in groupsByFactura)
                {
                    var group = new GastoFacturaGroup
                    {
                        NumeroFactura = grouping.Key == "SIN-FACTURA" ? string.Empty : grouping.Key,
                        RutaFactura = grouping.FirstOrDefault()?.RutaFactura ?? string.Empty
                    };

                    foreach (var itemDto in grouping)
                    {
                        var gastoItem = new GastoItemInput
                        {
                            TipoGasto = itemDto.TipoGasto,
                            Descripcion = itemDto.Descripcion ?? string.Empty,
                            Proveedor = itemDto.Proveedor ?? string.Empty,
                            ValorInput = itemDto.Valor.ToString(CultureInfo.CurrentCulture),
                            NumeroFactura = itemDto.NumeroFactura ?? string.Empty,
                            RutaFactura = itemDto.RutaFactura ?? string.Empty
                        };

                        gastoItem.PropertyChanged += GastoItem_PropertyChanged;
                        group.Items.Add(gastoItem);
                    }

                    UpdateGroupTotal(group);
                    _itemsGasto.Add(group);
                }
            }

            if (_itemsGasto.Count == 0 || _itemsGasto.Sum(g => g.Items.Count) == 0)
            {
                var newItem = new GastoItemInput
                {
                    TipoGasto = (int)GestLog.Modules.GestionVehiculos.Models.Enums.TipoGastoMantenimientoVehiculo.Otro,
                    Descripcion = string.IsNullOrWhiteSpace(TituloActividad) ? "" : TituloActividad,
                    Proveedor = Proveedor,
                    ValorInput = Costo?.ToString(CultureInfo.CurrentCulture) ?? string.Empty,
                    RutaFactura = RutaFactura
                };

                newItem.PropertyChanged += GastoItem_PropertyChanged;
                AddItemToGroup(newItem);
            }

            IcItemsGasto.ItemsSource = _itemsGasto;
            UpdateCartSummary();

            _ = CargarSugerenciasAsync();

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

        private async void BtnAttachFactura_Click(object sender, RoutedEventArgs e)
        {
            var uploaded = await FacturaStorageHelper.PickAndUploadFacturaAsync(this, "factura_correctivo");
            if (!string.IsNullOrWhiteSpace(uploaded))
            {
                TxtRutaFactura.Text = uploaded;
            }
        }

        private async void BtnOpenFactura_Click(object sender, RoutedEventArgs e)
        {
            await FacturaStorageHelper.OpenFacturaAsync(this, TxtRutaFactura.Text);
        }

        private async void BtnAttachPlanFactura_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is PlanPreventivoSeleccionItem item)
            {
                var uploaded = await FacturaStorageHelper.PickAndUploadFacturaAsync(this, "factura_planpreventivo");
                if (!string.IsNullOrWhiteSpace(uploaded))
                {
                    item.InvoicePath = uploaded;
                    // force UI update if necessary
                }
            }
        }

        private void BtnAddGastoItem_Click(object sender, RoutedEventArgs e)
        {
            var item = new GastoItemInput
            {
                TipoGasto = (int)GestLog.Modules.GestionVehiculos.Models.Enums.TipoGastoMantenimientoVehiculo.Otro
            };

            item.PropertyChanged += GastoItem_PropertyChanged;
            AddItemToGroup(item);
            UpdateCartSummary();
        }

        private void BtnAddFacturaGroup_Click(object sender, RoutedEventArgs e)
        {
            var newGroup = new GastoFacturaGroup { NumeroFactura = string.Empty, ProveedorFactura = string.Empty, RutaFactura = string.Empty };
            var item = new GastoItemInput { TipoGasto = (int)GestLog.Modules.GestionVehiculos.Models.Enums.TipoGastoMantenimientoVehiculo.Otro };
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

        private async void BtnOpenPlanFactura_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is PlanPreventivoSeleccionItem item)
            {
                if (!string.IsNullOrWhiteSpace(item.InvoicePath))
                {
                    await FacturaStorageHelper.OpenFacturaAsync(this, item.InvoicePath);
                }
            }
        }

        private async void BtnAttachSharedInvoiceCompletar_Click(object sender, RoutedEventArgs e)
        {
            var uploaded = await FacturaStorageHelper.PickAndUploadFacturaAsync(this, "factura_correctivo");
            if (!string.IsNullOrWhiteSpace(uploaded))
            {
                TxtSharedInvoicePathCompletar.Text = uploaded;
            }
        }

        private void BtnApplySharedInvoiceCompletar_Click(object sender, RoutedEventArgs e)
        {
            var numero = TxtSharedInvoiceNumberCompletar.Text?.Trim() ?? string.Empty;
            var ruta = TxtSharedInvoicePathCompletar.Text?.Trim() ?? string.Empty;

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

            TxtCartItemsCountCompletar.Text = totalItems.ToString(CultureInfo.InvariantCulture);
            TxtCartItemsTotalCompletar.Text = $"$ {decimal.Round(total, 2):N0}";
        }

        private void BtnEditPlanNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn)
            {
                return;
            }

            if (btn.DataContext is not PlanPreventivoSeleccionItem item)
            {
                return;
            }

            if (!item.IsSelected)
            {
                return;
            }

            var edited = ShowPlanNoteEditor(
                item.Nombre,
                item.DetalleOpcional ?? string.Empty,
                item.ProveedorOpcional ?? string.Empty,
                item.RutaFacturaOpcional ?? string.Empty,
                item.CostoOpcionalInput ?? string.Empty,
                item.IsCustomCost);
            if (edited != null)
            {
                item.DetalleOpcional = edited.Detalle;
                item.ProveedorOpcional = edited.Proveedor;
                item.RutaFacturaOpcional = edited.RutaFactura;
                item.CostoOpcionalInput = edited.CostoIndividual;
                item.IsCustomCost = edited.UsarCostoPersonalizado;
                item.CustomCostInput = edited.CostoIndividual;
                item.InvoicePath = edited.RutaFactura;

                IcPlanesPreventivos.Items.Refresh();
            }
        }

        private sealed class PlanNoteEditResult
        {
            public string Detalle { get; set; } = string.Empty;
            public string Proveedor { get; set; } = string.Empty;
            public string RutaFactura { get; set; } = string.Empty;
            public string CostoIndividual { get; set; } = string.Empty;
            public bool UsarCostoPersonalizado { get; set; }
        }

        private PlanNoteEditResult? ShowPlanNoteEditor(string planName, string currentDetalle, string currentProveedor, string currentRutaFactura, string currentCostoIndividual = "", bool currentUsarCostoPersonalizado = false)
        {
            var facturaPath = currentRutaFactura?.Trim() ?? string.Empty;

            var editor = new Window
            {
                Title = $"Nota específica - {planName}",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                MinWidth = 700,
                MaxWidth = 860,
                MinHeight = 560,
                Background = System.Windows.Media.Brushes.White
            };

            var inputStyle = TryFindResource("InputStyle") as Style;
            var primaryButtonStyle = TryFindResource("PrimaryButtonStyle") as Style;
            var ghostButtonStyle = TryFindResource("GhostButton") as Style;

            var root = new WpfGrid { Margin = new Thickness(18) };
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });

            var title = new WpfTextBlock
            {
                Text = "Configurar datos específicos del plan",
                FontWeight = FontWeights.SemiBold,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 12)
            };
            WpfGrid.SetRow(title, 0);
            root.Children.Add(title);

            var detalleLabel = new WpfTextBlock
            {
                Text = "Detalle del plan (opcional)",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            WpfGrid.SetRow(detalleLabel, 1);
            root.Children.Add(detalleLabel);

            var detalleBox = new System.Windows.Controls.TextBox
            {
                Text = currentDetalle,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MinHeight = 120,
                MaxHeight = 160,
                Padding = new Thickness(10),
                Style = inputStyle
            };
            WpfGrid.SetRow(detalleBox, 2);
            root.Children.Add(detalleBox);

            var proveedorCostoGrid = new WpfGrid { Margin = new Thickness(0, 10, 0, 0) };
            proveedorCostoGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            proveedorCostoGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(16) });
            proveedorCostoGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(250) });

            var proveedorStack = new System.Windows.Controls.StackPanel();
            proveedorStack.Children.Add(new WpfTextBlock
            {
                Text = "Proveedor (opcional, sobrescribe el común)",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            });
            var provBox = new System.Windows.Controls.TextBox
            {
                Text = currentProveedor,
                Padding = new Thickness(10),
                Style = inputStyle
            };
            proveedorStack.Children.Add(provBox);
            WpfGrid.SetColumn(proveedorStack, 0);
            proveedorCostoGrid.Children.Add(proveedorStack);

            var costoStack = new System.Windows.Controls.StackPanel();
            var chkCostoPersonalizado = new System.Windows.Controls.CheckBox
            {
                Content = "Costo personalizado",
                IsChecked = currentUsarCostoPersonalizado,
                Margin = new Thickness(0, 0, 0, 6)
            };
            costoStack.Children.Add(chkCostoPersonalizado);
            var costoBox = new System.Windows.Controls.TextBox
            {
                Text = currentCostoIndividual,
                Padding = new Thickness(10),
                IsEnabled = currentUsarCostoPersonalizado,
                Style = inputStyle
            };
            chkCostoPersonalizado.Checked += (_, __) => costoBox.IsEnabled = true;
            chkCostoPersonalizado.Unchecked += (_, __) => costoBox.IsEnabled = false;
            costoStack.Children.Add(costoBox);
            WpfGrid.SetColumn(costoStack, 2);
            proveedorCostoGrid.Children.Add(costoStack);

            WpfGrid.SetRow(proveedorCostoGrid, 3);
            root.Children.Add(proveedorCostoGrid);

            var facturaLabel = new WpfTextBlock
            {
                Text = "Factura del plan",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 12, 0, 6)
            };
            WpfGrid.SetRow(facturaLabel, 4);
            root.Children.Add(facturaLabel);

            var facturaGrid = new WpfGrid();
            facturaGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            facturaGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });
            facturaGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });

            var rutaBox = new System.Windows.Controls.TextBox
            {
                Text = facturaPath,
                IsReadOnly = true,
                Padding = new Thickness(10),
                Style = inputStyle
            };
            WpfGrid.SetColumn(rutaBox, 0);
            facturaGrid.Children.Add(rutaBox);

            var btnAttach = new System.Windows.Controls.Button
            {
                Content = "Adjuntar",
                Margin = new Thickness(8, 0, 0, 0),
                MinWidth = 88,
                Style = ghostButtonStyle
            };
            btnAttach.Click += async (_, __) =>
            {
                var sel = await FacturaStorageHelper.PickAndUploadFacturaAsync(editor, "factura_planpreventivo");
                if (!string.IsNullOrWhiteSpace(sel))
                {
                    facturaPath = sel;
                    rutaBox.Text = facturaPath;
                }
            };
            WpfGrid.SetColumn(btnAttach, 1);
            facturaGrid.Children.Add(btnAttach);

            var btnOpen = new System.Windows.Controls.Button
            {
                Content = "Ver",
                Margin = new Thickness(8, 0, 0, 0),
                MinWidth = 72,
                Style = ghostButtonStyle
            };
            btnOpen.Click += async (_, __) => await FacturaStorageHelper.OpenFacturaAsync(editor, facturaPath);
            WpfGrid.SetColumn(btnOpen, 2);
            facturaGrid.Children.Add(btnOpen);

            WpfGrid.SetRow(facturaGrid, 5);
            root.Children.Add(facturaGrid);

            var footer = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 14, 0, 0)
            };

            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancelar",
                MinWidth = 96,
                Margin = new Thickness(0, 0, 8, 0),
                Style = ghostButtonStyle
            };
            btnCancel.Click += (_, __) => editor.DialogResult = false;

            var btnSave = new System.Windows.Controls.Button
            {
                Content = "Guardar",
                MinWidth = 96,
                Style = primaryButtonStyle
            };
            btnSave.Click += (_, __) => editor.DialogResult = true;

            footer.Children.Add(btnCancel);
            footer.Children.Add(btnSave);
            WpfGrid.SetRow(footer, 6);
            root.Children.Add(footer);

            editor.Content = root;

            if (editor.ShowDialog() == true)
            {
                var usarCostoPersonalizado = chkCostoPersonalizado.IsChecked == true;
                var costoIndividual = usarCostoPersonalizado ? (costoBox.Text?.Trim() ?? string.Empty) : string.Empty;
                var rutaFactura = rutaBox.Text?.Trim() ?? string.Empty;

                if (usarCostoPersonalizado)
                {
                    if (string.IsNullOrWhiteSpace(costoIndividual))
                    {
                        System.Windows.MessageBox.Show(editor, "Debes ingresar costo personalizado.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return null;
                    }

                    if (string.IsNullOrWhiteSpace(rutaFactura))
                    {
                        System.Windows.MessageBox.Show(editor, "Debes adjuntar factura cuando usas costo personalizado.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return null;
                    }
                }

                return new PlanNoteEditResult
                {
                    Detalle = detalleBox.Text?.Trim() ?? string.Empty,
                    Proveedor = provBox.Text?.Trim() ?? string.Empty,
                    RutaFactura = rutaFactura,
                    CostoIndividual = costoIndividual,
                    UsarCostoPersonalizado = usarCostoPersonalizado
                };
            }

            return null;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!long.TryParse(TxtKilometraje.Text?.Trim(), out var km) || km <= 0)
            {
                TxtError.Text = "Debe ingresar un kilometraje válido.";
                return;
            }

            KilometrajeAlCompletar = km;
            Responsable = CmbResponsable.Text?.Trim() ?? string.Empty;
            Proveedor = CmbProveedor.Text?.Trim() ?? string.Empty;
            Observaciones = TxtObservaciones.Text?.Trim() ?? string.Empty;
            TituloActividad = TxtTituloActividad.Text?.Trim() ?? string.Empty;
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

            var itemsGasto = new List<EjecucionMantenimientoItemGastoDto>();
            foreach (var group in _itemsGasto)
            {
                var numeroFacturaGrupo = (group.NumeroFactura ?? string.Empty).Trim();
                var rutaFacturaGrupo = (group.RutaFactura ?? string.Empty).Trim();

                foreach (var item in group.Items)
                {
                var descripcion = (item.Descripcion ?? string.Empty).Trim();
                var proveedorItem = (item.Proveedor ?? string.Empty).Trim();
                    
                    var numeroFactura = string.IsNullOrWhiteSpace(item.NumeroFactura) ? numeroFacturaGrupo : (item.NumeroFactura ?? string.Empty).Trim();
                    var rutaFacturaItem = string.IsNullOrWhiteSpace(item.RutaFactura) ? rutaFacturaGrupo : (item.RutaFactura ?? string.Empty).Trim();

                var valorValido = decimal.TryParse(item.ValorInput?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor)
                    || decimal.TryParse(item.ValorInput?.Trim(), out valor);

                if (!valorValido)
                {
                    valor = 0m;
                }

                if (string.IsNullOrWhiteSpace(descripcion) && valor <= 0 && string.IsNullOrWhiteSpace(proveedorItem) && string.IsNullOrWhiteSpace(rutaFacturaItem))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(descripcion))
                {
                    TxtError.Text = "Cada ítem de gasto debe tener descripción.";
                    return;
                }

                if (valor < 0)
                {
                    TxtError.Text = "El valor de cada ítem de gasto debe ser mayor o igual a cero.";
                    return;
                }

                itemsGasto.Add(new EjecucionMantenimientoItemGastoDto
                {
                    TipoGasto = item.TipoGasto,
                    Descripcion = descripcion,
                    Proveedor = string.IsNullOrWhiteSpace(proveedorItem) ? null : proveedorItem,
                    Valor = decimal.Round(valor, 2),
                    NumeroFactura = string.IsNullOrWhiteSpace(numeroFactura) ? null : numeroFactura,
                    RutaFactura = string.IsNullOrWhiteSpace(rutaFacturaItem) ? null : rutaFacturaItem,
                    FechaDocumento = DateTimeOffset.Now
                });
                }
            }

            if (itemsGasto.Count > 0)
            {
                var totalItems = decimal.Round(itemsGasto.Sum(x => x.Valor), 2);
                Costo = totalItems;

                var primerProveedor = itemsGasto.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Proveedor))?.Proveedor;
                if (string.IsNullOrWhiteSpace(Proveedor) && !string.IsNullOrWhiteSpace(primerProveedor))
                {
                    Proveedor = primerProveedor;
                    CmbProveedor.Text = Proveedor;
                }

                var primeraRuta = itemsGasto.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.RutaFactura))?.RutaFactura;
                if (string.IsNullOrWhiteSpace(RutaFactura) && !string.IsNullOrWhiteSpace(primeraRuta))
                {
                    RutaFactura = primeraRuta;
                    TxtRutaFactura.Text = RutaFactura;
                }
            }

            ItemsGasto = itemsGasto;

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
                        if (string.IsNullOrWhiteSpace(plan.InvoicePath))
                        {
                            TxtError.Text = $"Debe adjuntar la factura para '{plan.Nombre}' cuando personaliza el costo.";
                            return;
                        }

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
                            FacturaRuta = plan.InvoicePath,
                            DetalleOpcional = plan.DetalleOpcional,
                            ProveedorOpcional = plan.ProveedorOpcional,
                            RutaFacturaOpcional = plan.RutaFacturaOpcional,
                            CostoOpcionalInput = plan.CostoOpcionalInput
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
                            FacturaRuta = string.Empty,
                            DetalleOpcional = plan.DetalleOpcional,
                            ProveedorOpcional = plan.ProveedorOpcional,
                            RutaFacturaOpcional = plan.RutaFacturaOpcional,
                            CostoOpcionalInput = plan.CostoOpcionalInput
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

        private async Task CargarSugerenciasAsync()
        {
            if (_ejecucionService == null)
            {
                return;
            }

            try
            {
                var responsables = await _ejecucionService.GetSuggestedResponsablesAsync(limit: 100);
                var proveedores = await _ejecucionService.GetSuggestedProveedoresAsync(limit: 100);

                CmbResponsable.ItemsSource = responsables;
                CmbProveedor.ItemsSource = proveedores;
            }
            catch
            {
                CmbResponsable.ItemsSource = Array.Empty<string>();
                CmbProveedor.ItemsSource = Array.Empty<string>();
            }
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
