using System.Windows;
using System.Windows.Controls;
using GestLog.ViewModels.Tools.GestionEquipos;
using GestLog.Modules.DatabaseConnection;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.Usuarios.Interfaces;

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class EquiposInformaticosView : System.Windows.Controls.UserControl
    {
        public EquiposInformaticosView()
        {
            this.InitializeComponent();
            var app = (App)System.Windows.Application.Current;
            var serviceProvider = app.ServiceProvider;
            var dbContext = serviceProvider.GetRequiredService<GestLogDbContext>();
            var currentUserService = serviceProvider.GetRequiredService<ICurrentUserService>();
            this.DataContext = new EquiposInformaticosViewModel(dbContext, currentUserService);
        }
    }
}
