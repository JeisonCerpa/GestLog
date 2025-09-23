using System.Windows;
using System.Windows.Controls;
using GestLog.ViewModels.Tools.GestionEquipos;
using GestLog.Modules.DatabaseConnection;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.Usuarios.Interfaces;
using Microsoft.EntityFrameworkCore;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class EquiposInformaticosView : System.Windows.Controls.UserControl
    {
        public EquiposInformaticosView()
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
    }
}
