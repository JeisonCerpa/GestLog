using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos;
using System.Windows;
using System.Windows.Controls;

using GestLog.Modules.DatabaseConnection;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.Usuarios.Interfaces;
using Microsoft.EntityFrameworkCore;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Equipos
{
    public partial class EquiposInformaticosView : System.Windows.Controls.UserControl
    {
        public EquiposInformaticosView()
        {
            try
            {
                this.InitializeComponent();
                var app = (App)System.Windows.Application.Current;
                var serviceProvider = app.ServiceProvider;
                var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<GestLogDbContext>>();
                var currentUserService = serviceProvider.GetRequiredService<ICurrentUserService>();
                var databaseService = serviceProvider.GetRequiredService<IDatabaseConnectionService>();
                var logger = serviceProvider.GetRequiredService<IGestLogLogger>();
                this.DataContext = new EquiposInformaticosViewModel(dbContextFactory, currentUserService, databaseService, logger);
            }
            catch (Exception ex)
            {
                try
                {
                    System.Windows.MessageBox.Show($"Error al inicializar EquiposInformaticosView:\n{ex}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                catch
                {
                    // ignored
                }
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        private void ActualizarButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.DataContext is EquiposInformaticosViewModel vm)
            {
                // Preferir ejecutar el comando si está disponible
                if (vm.CargarEquiposCommand != null && vm.CargarEquiposCommand.CanExecute(null))
                {
                    vm.CargarEquiposCommand.Execute(null);
                    return;
                }

                // Si el comando no está disponible o no puede ejecutarse, llamar al método público
                _ = vm.CargarEquipos();
            }
        }
    }
}

