using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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

            _ejecucionService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService<IEjecucionMantenimientoService>();

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
