using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows;
using SystemUri = System.Windows.Application;
using System;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class CronogramaDiarioView : UserControl
    {
        public CronogramaDiarioView()
        {
            try
            {
                System.Windows.Application.LoadComponent(this, new Uri("/GestLog;component/Views/Tools/GestionEquipos/CronogramaDiarioView.xaml", UriKind.Relative));
            }
            catch { }
            this.Loaded += CronogramaDiarioView_Loaded;
        }        private async void CronogramaDiarioView_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext == null)
                {
                    var sp = LoggingService.GetServiceProvider();
                    var vm = sp.GetService(typeof(GestLog.Modules.GestionEquiposInformaticos.ViewModels.CronogramaDiarioViewModel)) as GestLog.Modules.GestionEquiposInformaticos.ViewModels.CronogramaDiarioViewModel;
                    if (vm != null)
                        DataContext = vm;
                }

                if (DataContext is GestLog.Modules.GestionEquiposInformaticos.ViewModels.CronogramaDiarioViewModel vm2 && vm2.Planificados.Count == 0)
                {
                    await vm2.LoadAsync(System.Threading.CancellationToken.None);
                }
            }
            catch { }
        }
    }
}
