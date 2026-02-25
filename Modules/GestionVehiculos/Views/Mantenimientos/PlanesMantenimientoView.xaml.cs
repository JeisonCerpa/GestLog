using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    /// <summary>
    /// Interacción lógica para PlanesMantenimientoView.xaml
    /// </summary>
    public partial class PlanesMantenimientoView : System.Windows.Controls.UserControl
    {
        public PlanesMantenimientoView()
        {
            InitializeComponent();
        }

        private async void BtnOpenPlanModal_Click(object sender, RoutedEventArgs e)
        {
            var sp = ((App)System.Windows.Application.Current).ServiceProvider;
            var vm = DataContext as PlanesMantenimientoViewModel;

            if (vm == null)
            {
                vm = sp?.GetService(typeof(PlanesMantenimientoViewModel)) as PlanesMantenimientoViewModel;
                if (vm != null)
                {
                    DataContext = vm;
                }
            }

            if (vm == null)
            {
                return;
            }

            var placa = vm.FilterPlaca;
            if (string.IsNullOrWhiteSpace(placa))
            {
                placa = TryResolvePlateFromParent();
                if (!string.IsNullOrWhiteSpace(placa))
                {
                    await vm.InitializeForVehicleAsync(placa);
                }
            }

            var dialog = new PlanMantenimientoDialog(vm);
            var owner = System.Windows.Application.Current?.Windows.Count > 0
                ? System.Windows.Application.Current.Windows[0]
                : System.Windows.Application.Current?.MainWindow;
            dialog.Owner = owner;
            dialog.ShowDialog();
        }

        private string TryResolvePlateFromParent()
        {
            DependencyObject? parent = this;
            while (parent != null)
            {
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                if (parent is FrameworkElement fe && fe.DataContext != null)
                {
                    var prop = fe.DataContext.GetType().GetProperty("Plate");
                    if (prop != null)
                    {
                        var value = prop.GetValue(fe.DataContext) as string;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }
            }

            return string.Empty;
        }

        // Eventos que pueden ser escuchados por el contenedor (ej. VehicleDetailsView)
        public event Action<GestLog.Modules.GestionVehiculos.Models.DTOs.PlanMantenimientoVehiculoDto>? HistoryRequested;
        public event Action<GestLog.Modules.GestionVehiculos.Models.DTOs.PlanMantenimientoVehiculoDto>? EditRequested;

        private void OverflowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.ContextMenu != null)
            {
                // Ensure the context menu has the correct placement target and data context
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.DataContext = btn.DataContext;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void MenuItem_Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.CommandParameter is GestLog.Modules.GestionVehiculos.Models.DTOs.PlanMantenimientoVehiculoDto plan)
            {
                var vm = DataContext as PlanesMantenimientoViewModel;
                if (vm != null)
                {
                    vm.EditPlanCommand.Execute(plan);
                    EditRequested?.Invoke(plan);

                    // Abrir dialogo de edición inmediatamente
                    vm.PrepareEditPlan(plan);
                var dialog = new PlanMantenimientoDialog(vm);
                    var owner = System.Windows.Application.Current?.Windows.Count > 0
                        ? System.Windows.Application.Current.Windows[0]
                        : System.Windows.Application.Current?.MainWindow;
                    dialog.Owner = owner;
                    dialog.ShowDialog();
                }
            }
        }

        private void MenuItem_History_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.CommandParameter is GestLog.Modules.GestionVehiculos.Models.DTOs.PlanMantenimientoVehiculoDto plan)
            {
                var vm = DataContext as PlanesMantenimientoViewModel;
                vm?.OpenHistoryCommand.Execute(plan);
                HistoryRequested?.Invoke(plan);
            }
        }
    }
}
