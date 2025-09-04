using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.DatabaseConnection;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionEquipos
{    public partial class AgregarEquipoInformaticoView : Window
    {
        public AgregarEquipoInformaticoView()
        {
            InitializeComponent();
            
            // Configurar DataContext usando DI para resolver dependencias
            var app = (App)System.Windows.Application.Current;
            var serviceProvider = app.ServiceProvider;
            var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
            var viewModel = new GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel(currentUserService);
            DataContext = viewModel;
            
            // Cargar personas cuando se carga la ventana
            this.Loaded += (sender, e) => {
                viewModel.Inicializar();
            };
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
