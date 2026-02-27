using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.IO;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
using GestLog.Modules.GestionVehiculos.Services.Utilities;
using WpfGrid = System.Windows.Controls.Grid;
using WpfRowDefinition = System.Windows.Controls.RowDefinition;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class RegistroPreventivoDialog : Window
    {
        public RegistroPreventivoDialog(EjecucionesMantenimientoViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

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

        private void BtnEditPlanNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn)
            {
                return;
            }

            if (btn.DataContext is not EjecucionesMantenimientoViewModel.PlanPreventivoSelectionItem item)
            {
                return;
            }

            if (!item.IsSelected)
            {
                return;
            }

            var edited = ShowPlanNoteEditor(item.NombrePlantilla, item.DetalleOpcional ?? string.Empty, item.ProveedorOpcional ?? string.Empty, item.RutaFacturaOpcional ?? string.Empty, item.CostoOpcionalInput ?? string.Empty);
            if (edited != null)
            {
                item.DetalleOpcional = edited.Detalle;
                item.ProveedorOpcional = edited.Proveedor;
                item.RutaFacturaOpcional = edited.RutaFactura;
                item.CostoOpcionalInput = edited.CostoIndividual;
            }
        }

        private sealed class PlanNoteEditResult
        {
            public string Detalle { get; set; } = string.Empty;
            public string Proveedor { get; set; } = string.Empty;
            public string RutaFactura { get; set; } = string.Empty;
            public string CostoIndividual { get; set; } = string.Empty;
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

        private PlanNoteEditResult? ShowPlanNoteEditor(string planName, string currentDetalle, string currentProveedor, string currentRutaFactura, string currentCostoIndividual = "")
        {
            var facturaPath = currentRutaFactura?.Trim() ?? string.Empty;

            var editor = new Window
            {
                Title = $"Nota específica - {planName}",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                MinWidth = 640,
                MaxWidth = 760,
                Background = System.Windows.Media.Brushes.White
            };

            var root = new WpfGrid { Margin = new Thickness(18) };
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });

            var lbl = new WpfTextBlock
            {
                Text = "Configurar datos específicos del plan",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            };
            WpfGrid.SetRow(lbl, 0);
            root.Children.Add(lbl);

            var txt = new System.Windows.Controls.TextBox
            {
                Text = currentDetalle,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                MinHeight = 120,
                MaxWidth = 700,
                Padding = new Thickness(10)
            };
            WpfGrid.SetRow(txt, 1);
            root.Children.Add(txt);

            var proveedorLbl = new WpfTextBlock
            {
                Text = "Proveedor (opcional, sobrescribe el común):",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 6)
            };
            WpfGrid.SetRow(proveedorLbl, 2);
            root.Children.Add(proveedorLbl);

            var proveedorTxt = new System.Windows.Controls.TextBox
            {
                Text = currentProveedor,
                MaxWidth = 700,
                Padding = new Thickness(10)
            };
            WpfGrid.SetRow(proveedorTxt, 3);
            root.Children.Add(proveedorTxt);

            var costoGrid = new WpfGrid { Margin = new Thickness(0, 10, 0, 0) };
            costoGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });
            costoGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var costoLbl = new WpfTextBlock
            {
                Text = "Costo individual (opcional):",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            WpfGrid.SetColumn(costoLbl, 0);
            costoGrid.Children.Add(costoLbl);

            var costoTxt = new System.Windows.Controls.TextBox
            {
                Text = currentCostoIndividual,
                MaxWidth = 220,
                Padding = new Thickness(10)
            };
            WpfGrid.SetColumn(costoTxt, 1);
            costoGrid.Children.Add(costoTxt);

            WpfGrid.SetRow(costoGrid, 4);
            root.Children.Add(costoGrid);

            var facturaGrid = new WpfGrid { Margin = new Thickness(0, 10, 0, 0) };
            facturaGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            facturaGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });
            facturaGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });

            var facturaTxt = new System.Windows.Controls.TextBox
            {
                Text = GetFacturaDisplayName(facturaPath),
                IsReadOnly = true,
                MaxWidth = 500,
                Padding = new Thickness(10)
            };
            WpfGrid.SetColumn(facturaTxt, 0);
            facturaGrid.Children.Add(facturaTxt);

            var facturaBtn = new System.Windows.Controls.Button
            {
                Content = "Adjuntar factura",
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(10, 6, 10, 6)
            };
            facturaBtn.Click += async (_, _) =>
            {
                var selected = await PickFacturaFileAsync(editor);
                if (selected != null)
                {
                    facturaPath = selected;
                    facturaTxt.Text = GetFacturaDisplayName(facturaPath);
                }
            };
            WpfGrid.SetColumn(facturaBtn, 1);
            facturaGrid.Children.Add(facturaBtn);

            var facturaOpenBtn = new System.Windows.Controls.Button
            {
                Content = "Ver",
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(10, 6, 10, 6)
            };
            facturaOpenBtn.Click += async (_, _) =>
            {
                await FacturaStorageHelper.OpenFacturaAsync(editor, facturaPath);
            };
            WpfGrid.SetColumn(facturaOpenBtn, 2);
            facturaGrid.Children.Add(facturaOpenBtn);

            WpfGrid.SetRow(facturaGrid, 5);
            root.Children.Add(facturaGrid);

            var footer = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancelar",
                Width = 100,
                Margin = new Thickness(0, 0, 8, 0)
            };
            btnCancel.Click += (_, _) => editor.DialogResult = false;

            var btnSave = new System.Windows.Controls.Button
            {
                Content = "Guardar",
                Width = 100
            };
            btnSave.Click += (_, _) => editor.DialogResult = true;

            footer.Children.Add(btnCancel);
            footer.Children.Add(btnSave);
            WpfGrid.SetRow(footer, 6);
            root.Children.Add(footer);

            editor.Content = root;

            if (editor.ShowDialog() != true)
            {
                return null;
            }

            return new PlanNoteEditResult
            {
                Detalle = txt.Text?.Trim() ?? string.Empty,
                Proveedor = proveedorTxt.Text?.Trim() ?? string.Empty,
                RutaFactura = facturaPath,
                CostoIndividual = costoTxt.Text?.Trim() ?? string.Empty
            };
        }

        private static string GetFacturaDisplayName(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.GetFileName(path);
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
