using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos;
using System.Windows.Controls;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using UserControl = System.Windows.Controls.UserControl;
using GestLog.Modules.GestionEquiposInformaticos.Views.Perifericos;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Equipos
{
    public partial class GestionEquiposHomeView : UserControl
    {
        private PerifericosView? _perifericosView;        public GestionEquiposHomeView()
        {
            try
            {
                System.Windows.Application.LoadComponent(this, new System.Uri("/GestLog;component/Modules/GestionEquiposInformaticos/Views/Equipos/GestionEquiposHomeView.xaml", System.UriKind.Relative));
            }
            catch { }
            
            // Resolver DataContext
            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                var vm = DataContext as GestionEquiposHomeViewModel ?? serviceProvider?.GetService(typeof(GestionEquiposHomeViewModel)) as GestionEquiposHomeViewModel;
                if (vm != null)
                    DataContext = vm;

                // Carga inicial
                InitializeViewModels(vm);

                // Agregar eventos para manejo de pestañas
                this.Loaded += GestionEquiposHomeView_Loaded;
                this.IsVisibleChanged += GestionEquiposHomeView_IsVisibleChanged;
            }
            catch { }
        }        private async void InitializeViewModels(GestionEquiposHomeViewModel? vm)
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
        }private void GestionEquiposHomeView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Buscar el TabControl y la vista de periféricos
            if (FindName("tabEquipos") is System.Windows.Controls.TabControl tabControl)
            {
                tabControl.SelectionChanged += TabControl_SelectionChanged;
                
                // Buscar la vista de periféricos en el árbol visual
                _perifericosView = FindPerifericosView();
            }
        }        private async void GestionEquiposHomeView_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // Cuando la vista principal se hace visible, verificar que los datos de periféricos estén disponibles
            if ((bool)e.NewValue)
            {
                await EnsurePerifericosDataLoaded();
            }
        }private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.TabControl tabControl)
            {
                var selectedTab = tabControl.SelectedItem as System.Windows.Controls.TabItem;
                if (selectedTab?.Header?.ToString() == "Periféricos")
                {
                    // Cuando se selecciona el tab de periféricos, verificar que los datos estén cargados
                    await EnsurePerifericosDataLoaded();
                }
            }
        }        private async System.Threading.Tasks.Task EnsurePerifericosDataLoaded()
        {
            try
            {
                // Verificar que el ViewModel tenga datos, si no los tiene, cargarlos
                if (DataContext is GestionEquiposHomeViewModel vm && 
                    vm.PerifericosVm != null && 
                    vm.PerifericosVm.Perifericos.Count == 0)
                {
                    await vm.PerifericosVm.CargarPerifericosAsync();
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

