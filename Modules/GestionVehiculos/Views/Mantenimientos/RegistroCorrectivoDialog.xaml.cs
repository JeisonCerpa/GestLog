using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;

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
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar factura (PDF o imagen)",
                Filter = "Archivos PDF/Imagen|*.pdf;*.png;*.jpg;*.jpeg|PDF (*.pdf)|*.pdf|Imagen (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Todos los archivos (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dlg.ShowDialog(this) == true)
            {
                _viewModel.RegistroRutaFactura = dlg.FileName;
            }
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
