using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
using GestLog.Modules.GestionVehiculos.Services.Utilities;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class RegistroPreventivoDialog : Window
    {
        private const string SharedDestinationKey = "SHARED";

        public sealed class PlanDestinoOption
        {
            public string Key { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
        }

        private sealed class GastoItemInput
            : INotifyPropertyChanged
        {
            private int _tipoGasto = 4;
            private string _descripcion = string.Empty;
            private string _proveedor = string.Empty;
            private string _valorInput = string.Empty;
            private string _numeroFactura = string.Empty;
            private string _rutaFactura = string.Empty;
            private string _planDestinoKey = SharedDestinationKey;

            public event PropertyChangedEventHandler? PropertyChanged;

            public int TipoGasto
            {
                get => _tipoGasto;
                set
                {
                    if (_tipoGasto == value) return;
                    _tipoGasto = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TipoGasto)));
                }
            }

            public string Descripcion
            {
                get => _descripcion;
                set
                {
                    if (_descripcion == value) return;
                    _descripcion = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Descripcion)));
                }
            }

            public string Proveedor
            {
                get => _proveedor;
                set
                {
                    if (_proveedor == value) return;
                    _proveedor = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Proveedor)));
                }
            }

            public string ValorInput
            {
                get => _valorInput;
                set
                {
                    if (_valorInput == value) return;
                    _valorInput = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValorInput)));
                }
            }

            public string NumeroFactura
            {
                get => _numeroFactura;
                set
                {
                    if (_numeroFactura == value) return;
                    _numeroFactura = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumeroFactura)));
                }
            }

            public string RutaFactura
            {
                get => _rutaFactura;
                set
                {
                    if (_rutaFactura == value) return;
                    _rutaFactura = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RutaFactura)));
                }
            }

            public string PlanDestinoKey
            {
                get => _planDestinoKey;
                set
                {
                    if (_planDestinoKey == value) return;
                    _planDestinoKey = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlanDestinoKey)));
                }
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

        private readonly ObservableCollection<GastoFacturaGroup> _itemsGasto = new();
        private readonly EjecucionesMantenimientoViewModel _viewModel;
        private int _currentStep = 1;
        private string _planFilterText = string.Empty;
        public ObservableCollection<PlanDestinoOption> PlanDestinoOptions { get; } = new();
        public ICollectionView? FilteredPlanes { get; private set; }

        public RegistroPreventivoDialog(EjecucionesMantenimientoViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = viewModel;
            IcItemsGasto.ItemsSource = _itemsGasto;
            InitializePlanFilter();
            RefreshPlanDestinoOptions();
            InitializeWizardState();

            viewModel.RegistroEsExtraordinario = false;
            viewModel.RegistroMotivoExtraordinario = string.Empty;

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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not EjecucionesMantenimientoViewModel vm)
            {
                return;
            }

            if (RbModoItems.IsChecked == true)
            {
                vm.RegistroItemsGasto = BuildItemsGasto(vm);
                var totalItems = vm.RegistroItemsGasto.Sum(x => x.Valor);
                vm.RegistroCostoInput = decimal.Round(totalItems, 2).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                vm.RegistroItemsGasto = new List<EjecucionMantenimientoItemGastoDto>();
            }

            await vm.RegistrarEjecucionAsync();
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender == RootGrid)
            {
                DialogResult = false;
                Close();
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private static Task<string?> PickFacturaFileAsync(Window owner)
        {
            return FacturaStorageHelper.PickAndUploadFacturaAsync(owner, "factura_preventivo");
        }

        private async void BtnAttachCommonFactura_Click(object sender, RoutedEventArgs e)
        {
            var selected = await PickFacturaFileAsync(this);
            if (selected == null)
            {
                return;
            }

            if (DataContext is EjecucionesMantenimientoViewModel vm)
            {
                vm.RegistroRutaFactura = selected;
            }
        }

        private async void BtnOpenCommonFactura_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EjecucionesMantenimientoViewModel vm)
            {
                await FacturaStorageHelper.OpenFacturaAsync(this, vm.RegistroRutaFactura);
            }
        }

        private void BtnAddGastoItem_Click(object sender, RoutedEventArgs e)
        {
            var item = new GastoItemInput
            {
                PlanDestinoKey = ResolveDefaultDestinationKey()
            };

            item.PropertyChanged += GastoItem_PropertyChanged;
            AddItemToGroup(item);
            UpdateTotalItemsAndSummary();
        }

        private void BtnAddFacturaGroup_Click(object sender, RoutedEventArgs e)
        {
            var newGroup = new GastoFacturaGroup { NumeroFactura = string.Empty, ProveedorFactura = string.Empty, RutaFactura = string.Empty };
            var item = new GastoItemInput { PlanDestinoKey = ResolveDefaultDestinationKey() };
            item.PropertyChanged += GastoItem_PropertyChanged;
            newGroup.Items.Add(item);
            _itemsGasto.Add(newGroup);
            UpdateTotalItemsAndSummary();
        }

        private void AddItemToGroup(GastoItemInput item)
        {
            // Por defecto, agregar a grupo "SIN-FACTURA"
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
                
                // Encontrar el grupo que contiene este item
                var group = _itemsGasto.FirstOrDefault(g => g.Items.Contains(item));
                if (group != null)
                {
                    group.Items.Remove(item);
                    UpdateGroupTotal(group);
                    
                    // Si el grupo quedó vacío, eliminarlo
                    if (group.Items.Count == 0)
                    {
                        _itemsGasto.Remove(group);
                    }
                }
                
                UpdateTotalItemsAndSummary();
            }
        }

        private List<EjecucionMantenimientoItemGastoDto> BuildItemsGasto(EjecucionesMantenimientoViewModel vm)
        {
            var items = new List<EjecucionMantenimientoItemGastoDto>();
            var planesSeleccionados = vm.PlanesPreventivoSeleccionables.Where(p => p.IsSelected).ToDictionary(p => p.PlanId, p => p.NombrePlantilla);

            foreach (var group in _itemsGasto)
            {
                var numeroFacturaGrupo = (group.NumeroFactura ?? string.Empty).Trim();
                var rutaFacturaGrupo = (group.RutaFactura ?? string.Empty).Trim();

                foreach (var item in group.Items)
                {
                    var descripcion = (item.Descripcion ?? string.Empty).Trim();
                    var proveedor = (item.Proveedor ?? string.Empty).Trim();
                    
                    // Usar la factura del grupo si el item no tiene
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
                        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? "Ítem de preventivo" : descripcion,
                        Proveedor = string.IsNullOrWhiteSpace(proveedor) ? null : proveedor,
                        Valor = valor < 0 ? 0m : decimal.Round(valor, 2),
                        NumeroFactura = string.IsNullOrWhiteSpace(numeroFactura) ? null : numeroFactura,
                        RutaFactura = string.IsNullOrWhiteSpace(rutaFactura) ? null : rutaFactura,
                        PlanMantenimientoDestinoId = TryResolvePlanDestinoId(item.PlanDestinoKey, planesSeleccionados.Keys),
                        EsCompartidoEntrePlanes = IsSharedDestination(item.PlanDestinoKey),
                        FechaDocumento = vm.RegistroFechaEjecucion.HasValue
                            ? new System.DateTimeOffset(vm.RegistroFechaEjecucion.Value.Date)
                            : System.DateTimeOffset.Now
                    });
                }
            }

            return items;
        }

        private void PlanSelectionChanged(object sender, RoutedEventArgs e)
        {
            RefreshPlanDestinoOptions();
            UpdateResumenRapido();
            ApplyPlanFilter();
        }

        private void InitializePlanFilter()
        {
            FilteredPlanes = CollectionViewSource.GetDefaultView(_viewModel.PlanesPreventivoSeleccionables);
            if (FilteredPlanes != null)
            {
                FilteredPlanes.Filter = item =>
                {
                    if (item is not EjecucionesMantenimientoViewModel.PlanPreventivoSelectionItem plan)
                    {
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(_planFilterText))
                    {
                        return true;
                    }

                    return plan.NombrePlantilla?.IndexOf(_planFilterText, StringComparison.OrdinalIgnoreCase) >= 0;
                };
            }

            ApplyPlanFilter();
        }

        private void TxtPlanFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _planFilterText = TxtPlanFilter?.Text?.Trim() ?? string.Empty;
            ApplyPlanFilter();
        }

        private void BtnClearPlanFilter_Click(object sender, RoutedEventArgs e)
        {
            if (TxtPlanFilter != null)
            {
                TxtPlanFilter.Text = string.Empty;
            }

            _planFilterText = string.Empty;
            ApplyPlanFilter();
        }

        private void ApplyPlanFilter()
        {
            FilteredPlanes?.Refresh();

            if (NoPlanesFilterResult == null || FilteredPlanes == null)
            {
                return;
            }

            var visibleCount = FilteredPlanes.Cast<object>().Count();
            NoPlanesFilterResult.Visibility = visibleCount == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void RefreshPlanDestinoOptions()
        {
            PlanDestinoOptions.Clear();
            PlanDestinoOptions.Add(new PlanDestinoOption
            {
                Key = SharedDestinationKey,
                Label = "Compartido (prorratear entre planes seleccionados)"
            });

            var planesSeleccionados = _viewModel.PlanesPreventivoSeleccionables
                .Where(p => p.IsSelected)
                .Select(p => new { p.PlanId, p.NombrePlantilla })
                .ToList();

            foreach (var plan in planesSeleccionados)
            {
                PlanDestinoOptions.Add(new PlanDestinoOption
                {
                    Key = BuildPlanDestinationKey(plan.PlanId),
                    Label = $"Solo {plan.NombrePlantilla}"
                });
            }

            var validKeys = new HashSet<string>(PlanDestinoOptions.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            foreach (var group in _itemsGasto)
            {
                foreach (var item in group.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.PlanDestinoKey) || !validKeys.Contains(item.PlanDestinoKey))
                    {
                        item.PlanDestinoKey = SharedDestinationKey;
                    }
                }
            }

            UpdateResumenRapido();
        }

        private void GastoItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GastoItemInput.ValorInput)
                || e.PropertyName == nameof(GastoItemInput.PlanDestinoKey)
                || e.PropertyName == nameof(GastoItemInput.Descripcion)
                || e.PropertyName == nameof(GastoItemInput.TipoGasto))
            {
                UpdateTotalItemsAndSummary();
            }
        }

        private void InitializeWizardState()
        {
            if (DataContext is EjecucionesMantenimientoViewModel vm)
            {
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(EjecucionesMantenimientoViewModel.RegistroFechaEjecucion)
                        || args.PropertyName == nameof(EjecucionesMantenimientoViewModel.RegistroKMAlMomentoInput)
                        || args.PropertyName == nameof(EjecucionesMantenimientoViewModel.RegistroResponsable))
                    {
                        UpdateResumenRapido();
                    }

                    if (args.PropertyName == nameof(EjecucionesMantenimientoViewModel.PlanesPreventivoSeleccionables))
                    {
                        InitializePlanFilter();
                        RefreshPlanDestinoOptions();
                    }
                };
            }

            UpdateStepUI();
            UpdateCostoPanels();
            UpdateTotalItemsAndSummary();
        }

        private void BtnNextStep_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == 1 && !_viewModel.HasPlanesSeleccionados)
            {
                _viewModel.ErrorMessage = "Debes seleccionar al menos un plan para continuar.";
                return;
            }

            if (_currentStep < 3)
            {
                _currentStep++;
                UpdateStepUI();
            }
        }

        private void BtnPrevStep_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 1)
            {
                _currentStep--;
                UpdateStepUI();
            }
        }

        private void UpdateStepUI()
        {
            Step1Panel.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

            TxtPasoActual.Text = $"Paso {_currentStep} de 3";

            BtnPrevStep.Visibility = _currentStep > 1 ? Visibility.Visible : Visibility.Collapsed;
            BtnNextStep.Visibility = _currentStep < 3 ? Visibility.Visible : Visibility.Collapsed;
            BtnSave.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

            _viewModel.ErrorMessage = string.Empty;
            UpdateResumenRapido();
        }

        private void CostoModeChanged(object sender, RoutedEventArgs e)
        {
            UpdateCostoPanels();
            UpdateTotalItemsAndSummary();
        }

        private void UpdateCostoPanels()
        {
            if (RbModoItems == null || PanelCostoItems == null || PanelCostoManual == null)
            {
                return;
            }

            var modoItems = RbModoItems.IsChecked == true;
            PanelCostoItems.Visibility = modoItems ? Visibility.Visible : Visibility.Collapsed;
            PanelCostoManual.Visibility = modoItems ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateTotalItemsAndSummary()
        {
            var total = CalculateItemsTotal();
            if (TxtTotalItems != null)
            {
                TxtTotalItems.Text = $"$ {total:N0}";
            }

            if (TxtCartItemsCountPrev != null)
            {
                var totalItems = _itemsGasto.Sum(g => g.Items.Count);
                TxtCartItemsCountPrev.Text = totalItems.ToString(CultureInfo.InvariantCulture);
            }

            if (TxtCartItemsTotalPrev != null)
            {
                TxtCartItemsTotalPrev.Text = $"$ {total:N0}";
            }

            if (RbModoItems != null && RbModoItems.IsChecked == true)
            {
                _viewModel.RegistroCostoInput = total.ToString(CultureInfo.InvariantCulture);
            }

            UpdateResumenRapido();
        }

        private decimal CalculateItemsTotal()
        {
            decimal total = 0m;
            foreach (var group in _itemsGasto)
            {
                foreach (var item in group.Items)
                {
                    var valorValido = decimal.TryParse(item.ValorInput?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor)
                        || decimal.TryParse(item.ValorInput?.Trim(), out valor);

                    if (valorValido && valor > 0)
                    {
                        total += decimal.Round(valor, 2);
                    }
                }
            }

            return decimal.Round(total, 2);
        }

        private void UpdateResumenRapido()
        {
            if (TxtResumenRapido == null)
            {
                return;
            }

            var planes = _viewModel.PlanesPreventivoSeleccionables.Count(x => x.IsSelected);
            var fecha = _viewModel.RegistroFechaEjecucion?.ToString("dd/MM/yyyy") ?? "Sin fecha";
            var km = string.IsNullOrWhiteSpace(_viewModel.RegistroKMAlMomentoInput) ? "Sin KM" : _viewModel.RegistroKMAlMomentoInput.Trim();
            var responsable = string.IsNullOrWhiteSpace(_viewModel.RegistroResponsable) ? "Sin responsable" : _viewModel.RegistroResponsable.Trim();
            var costo = RbModoItems?.IsChecked == true
                ? CalculateItemsTotal()
                : (decimal.TryParse(_viewModel.RegistroCostoInput?.Trim(), out var manual) ? manual : 0m);

            TxtResumenRapido.Text = $"Resumen: {planes} plan(es) · Fecha: {fecha} · KM: {km} · Responsable: {responsable} · Total: $ {costo:N0}";
        }

        private string ResolveDefaultDestinationKey()
        {
            var planesSeleccionados = _viewModel.PlanesPreventivoSeleccionables.Where(p => p.IsSelected).ToList();
            if (planesSeleccionados.Count == 1)
            {
                return BuildPlanDestinationKey(planesSeleccionados[0].PlanId);
            }

            return SharedDestinationKey;
        }

        private static bool IsSharedDestination(string? key)
        {
            return string.IsNullOrWhiteSpace(key)
                || key.Equals(SharedDestinationKey, System.StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildPlanDestinationKey(int planId) => $"PLAN:{planId}";

        private static int? TryResolvePlanDestinoId(string? key, IEnumerable<int> planIds)
        {
            if (string.IsNullOrWhiteSpace(key) || IsSharedDestination(key))
            {
                return null;
            }

            if (!key.StartsWith("PLAN:", System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var rawId = key.Substring(5).Trim();
            if (!int.TryParse(rawId, out var planId))
            {
                return null;
            }

            return planIds.Contains(planId) ? planId : null;
        }

        private async void BtnAttachSharedInvoicePrev_Click(object sender, RoutedEventArgs e)
        {
            var selected = await PickFacturaFileAsync(this);
            if (selected == null)
            {
                return;
            }

            if (TxtSharedInvoicePathPrev != null)
            {
                TxtSharedInvoicePathPrev.Text = selected;
            }
        }

        private void BtnApplySharedInvoicePrev_Click(object sender, RoutedEventArgs e)
        {
            var numero = TxtSharedInvoiceNumberPrev?.Text?.Trim() ?? string.Empty;
            var ruta = TxtSharedInvoicePathPrev?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(numero) && string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            // Aplicar a los grupos que no tienen factura
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
            UpdateTotalItemsAndSummary();
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
