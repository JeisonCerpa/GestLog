using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Perifericos;
using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos;
using GestLog.Modules.DatabaseConnection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Perifericos
{
    public partial class PerifericosView : System.Windows.Controls.UserControl
    {
        public PerifericosView()
        {
            InitializeComponent();
            var app = (App)System.Windows.Application.Current;
            var serviceProvider = app.ServiceProvider;
            var logger = serviceProvider.GetRequiredService<IGestLogLogger>();
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<GestLogDbContext>>();
            var databaseService = serviceProvider.GetRequiredService<IDatabaseConnectionService>();
            this.DataContext = new PerifericosViewModel(logger, dbContextFactory, databaseService);
        }        private void ActualizarButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.DataContext is PerifericosViewModel vm)
            {
                // Preferir ejecutar el comando si está disponible
                if (vm.CargarPerifericosCommand != null && vm.CargarPerifericosCommand.CanExecute(null))
                {
                    vm.CargarPerifericosCommand.Execute(null);
                    return;
                }

                // Si el comando no está disponible o no puede ejecutarse, llamar al método público
                _ = vm.CargarPerifericosAsync();
            }
        }
    }
}

