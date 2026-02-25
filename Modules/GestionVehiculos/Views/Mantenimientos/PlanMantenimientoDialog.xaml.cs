using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class PlanMantenimientoDialog : Window
    {
        public PlanMantenimientoDialog(PlanesMantenimientoViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            if (viewModel != null)
            {
                viewModel.RequestClose += () =>
                {
                    // usar Dispatcher para asegurarnos de que la ventana ya esté en el árbol visual
                    this.Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        if (this.IsLoaded && this.IsVisible)
                        {
                            try
                            {
                                this.DialogResult = true;
                            }
                            catch { }
                            this.Close();
                        }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                };
            }

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
            if (DataContext is PlanesMantenimientoViewModel vm)
            {
                vm.ResetEditContext();
            }
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (DataContext is PlanesMantenimientoViewModel vm)
            {
                vm.ResetEditContext();
            }
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
