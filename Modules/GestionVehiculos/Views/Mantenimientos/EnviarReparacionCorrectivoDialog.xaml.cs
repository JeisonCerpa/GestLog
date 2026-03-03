using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class EnviarReparacionCorrectivoDialog : Window
    {
        private readonly IEjecucionMantenimientoService? _ejecucionService;

        public string Proveedor { get; private set; }
        public string Observaciones { get; private set; }

        public EnviarReparacionCorrectivoDialog(string? proveedorInicial, string? observacionesInicial)
        {
            InitializeComponent();

            _ejecucionService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService<IEjecucionMantenimientoService>();

            Proveedor = proveedorInicial?.Trim() ?? string.Empty;
            Observaciones = observacionesInicial?.Trim() ?? string.Empty;

            CmbProveedor.Text = Proveedor;
            TxtObservaciones.Text = Observaciones;

            _ = CargarProveedoresSugeridosAsync();

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

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var proveedor = CmbProveedor.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(proveedor))
            {
                TxtError.Text = "Debe indicar proveedor.";
                return;
            }

            Proveedor = proveedor;
            Observaciones = TxtObservaciones.Text?.Trim() ?? string.Empty;
            TxtError.Text = string.Empty;

            DialogResult = true;
            Close();
        }

        private async Task CargarProveedoresSugeridosAsync()
        {
            if (_ejecucionService == null)
            {
                return;
            }

            try
            {
                var proveedores = await _ejecucionService.GetSuggestedProveedoresAsync(limit: 100);
                CmbProveedor.ItemsSource = proveedores;
            }
            catch
            {
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
