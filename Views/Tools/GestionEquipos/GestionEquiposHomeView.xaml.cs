using System.Windows.Controls;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using UserControl = System.Windows.Controls.UserControl;

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class GestionEquiposHomeView : UserControl
    {
        private PerifericosView? _perifericosView;

        public GestionEquiposHomeView()
        {
            try
            {
                System.Windows.Application.LoadComponent(this, new System.Uri("/GestLog;component/Views/Tools/GestionEquipos/GestionEquiposHomeView.xaml", System.UriKind.Relative));
            }
            catch { }
            
            // Resolver DataContext
            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                var vm = DataContext as GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel ?? serviceProvider?.GetService(typeof(GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel)) as GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel;
                if (vm != null)
                    DataContext = vm;

                // Carga inicial
                InitializeViewModels(vm);

                // Agregar eventos para manejo de pestañas
                this.Loaded += GestionEquiposHomeView_Loaded;
                this.IsVisibleChanged += GestionEquiposHomeView_IsVisibleChanged;
            }
            catch { }
        }

        private async void InitializeViewModels(GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel? vm)
        {
            try
            {
                if (vm == null) return;

                var cronogramaVm = vm.CronogramaVm;
                if (cronogramaVm != null && cronogramaVm.Planificados.Count == 0)
                    _ = cronogramaVm.LoadAsync(System.Threading.CancellationToken.None);

                var perifericosVm = vm.PerifericosVm;
                if (perifericosVm != null)
                {
                    // Cargar periféricos inicialmente
                    await perifericosVm.CargarPerifericosAsync();
                }
            }
            catch { }
        }        private void GestionEquiposHomeView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Buscar el TabControl y la vista de periféricos
            if (FindName("tabEquipos") is System.Windows.Controls.TabControl tabControl)
            {
                tabControl.SelectionChanged += TabControl_SelectionChanged;
                
                // Buscar la vista de periféricos en el árbol visual
                _perifericosView = FindPerifericosView();
            }
        }

        private async void GestionEquiposHomeView_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // Cuando la vista principal se hace visible, recargar periféricos si la pestaña está seleccionada
            if ((bool)e.NewValue && _perifericosView != null)
            {
                await RefreshPerifericosIfNeeded();
            }
        }        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.TabControl tabControl)
            {
                var selectedTab = tabControl.SelectedItem as System.Windows.Controls.TabItem;
                if (selectedTab?.Header?.ToString() == "Periféricos")
                {
                    await RefreshPerifericosIfNeeded();
                }
            }
        }

        private async System.Threading.Tasks.Task RefreshPerifericosIfNeeded()
        {
            try
            {
                if (_perifericosView != null)
                {
                    await _perifericosView.RefreshDataAsync();
                }
            }
            catch { }
        }

        private PerifericosView? FindPerifericosView()
        {
            try
            {
                // Buscar recursivamente en el árbol visual
                return FindVisualChild<PerifericosView>(this);
            }
            catch
            {
                return null;
            }
        }

        private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    return found;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
