using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
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

            var edited = ShowPlanNoteEditor(item.NombrePlantilla, item.DetalleOpcional ?? string.Empty, item.ProveedorOpcional ?? string.Empty, item.RutaFacturaOpcional ?? string.Empty);
            if (edited != null)
            {
                item.DetalleOpcional = edited.Detalle;
                item.ProveedorOpcional = edited.Proveedor;
                item.RutaFacturaOpcional = edited.RutaFactura;
            }
        }

        private sealed class PlanNoteEditResult
        {
            public string Detalle { get; set; } = string.Empty;
            public string Proveedor { get; set; } = string.Empty;
            public string RutaFactura { get; set; } = string.Empty;
        }

        private static string? PickFacturaFile(Window owner)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar factura (PDF o imagen)",
                Filter = "Archivos PDF/Imagen|*.pdf;*.png;*.jpg;*.jpeg|PDF (*.pdf)|*.pdf|Imagen (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Todos los archivos (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            return dlg.ShowDialog(owner) == true ? dlg.FileName : null;
        }

        private void BtnAttachCommonFactura_Click(object sender, RoutedEventArgs e)
        {
            var selected = PickFacturaFile(this);
            if (selected == null)
            {
                return;
            }

            if (DataContext is EjecucionesMantenimientoViewModel vm)
            {
                vm.RegistroRutaFactura = selected;
            }
        }

        private PlanNoteEditResult? ShowPlanNoteEditor(string planName, string currentDetalle, string currentProveedor, string currentRutaFactura)
        {
            var editor = new Window
            {
                Title = $"Nota específica - {planName}",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                MinWidth = 560,
                MaxWidth = 640,
                Background = System.Windows.Media.Brushes.White
            };

            var root = new WpfGrid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });

            var lbl = new WpfTextBlock
            {
                Text = "Ingresa la nota específica para este plan (opcional):",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            WpfGrid.SetRow(lbl, 0);
            root.Children.Add(lbl);

            var txt = new System.Windows.Controls.TextBox
            {
                Text = currentDetalle,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                MinHeight = 130,
                MaxWidth = 580,
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
                MaxWidth = 580,
                Padding = new Thickness(10)
            };
            WpfGrid.SetRow(proveedorTxt, 3);
            root.Children.Add(proveedorTxt);

            var facturaGrid = new WpfGrid { Margin = new Thickness(0, 10, 0, 0) };
            facturaGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            facturaGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });

            var facturaTxt = new System.Windows.Controls.TextBox
            {
                Text = currentRutaFactura,
                IsReadOnly = true,
                MaxWidth = 470,
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
            facturaBtn.Click += (_, _) =>
            {
                var selected = PickFacturaFile(editor);
                if (selected != null)
                {
                    facturaTxt.Text = selected;
                }
            };
            WpfGrid.SetColumn(facturaBtn, 1);
            facturaGrid.Children.Add(facturaBtn);

            WpfGrid.SetRow(facturaGrid, 4);
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
            WpfGrid.SetRow(footer, 5);
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
                RutaFactura = facturaTxt.Text?.Trim() ?? string.Empty
            };
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
