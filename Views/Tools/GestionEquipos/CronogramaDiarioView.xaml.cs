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
                // Cargar el XAML como recurso de la aplicación (evita dependencia del método generado InitializeComponent)
                System.Windows.Application.LoadComponent(this, new Uri("/GestLog;component/Views/Tools/GestionEquipos/CronogramaDiarioView.xaml", UriKind.Relative));
            }
            catch
            {
                // si falla, no bloquear la inicialización; el control puede instanciarse sin el XAML en entornos de compilación ligeros
            }

            this.Loaded += CronogramaDiarioView_Loaded;
        }

        private async void CronogramaDiarioView_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Si no hay DataContext, intentar resolver desde DI
                if (this.DataContext == null)
                {
                    var sp = LoggingService.GetServiceProvider();
                    var vm = sp.GetService(typeof(GestLog.Modules.GestionMantenimientos.ViewModels.CronogramaDiarioViewModel)) as GestLog.Modules.GestionMantenimientos.ViewModels.CronogramaDiarioViewModel;
                    if (vm != null)
                        this.DataContext = vm;
                }

                if (this.DataContext is GestLog.Modules.GestionMantenimientos.ViewModels.CronogramaDiarioViewModel vm2)
                {
                    // Ejecutar carga inicial si no hay items
                    if (vm2.Planificados.Count == 0)
                    {
                        await vm2.LoadAsync(System.Threading.CancellationToken.None);
                    }
                }
            }
            catch
            {
                // no bloquear UI por errores de carga
            }
        }
    }
}
