using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
using GestLog.Modules.GestionVehiculos.Services.Utilities;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class RegistroCorrectivoDialog : Window
    {
        private readonly CorrectivosMantenimientoViewModel _viewModel;

        public RegistroCorrectivoDialog(CorrectivosMantenimientoViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

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
            await _viewModel.RegistrarCorrectivoAsync();
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
