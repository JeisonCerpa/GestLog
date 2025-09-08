using System.Windows;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.ViewModels.Tools.GestionEquipos;
using GestLog.Modules.DatabaseConnection;

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class DetallesEquipoInformaticoView : Window
    {
        public DetallesEquipoInformaticoView(EquipoInformaticoEntity equipo)
        {
            InitializeComponent();

            // Intentar obtener GestLogDbContext desde Application.Current.Properties si fue registrado
            GestLogDbContext? db = null;
            try
            {
                if (System.Windows.Application.Current?.Properties.Contains("DbContext") == true)
                    db = System.Windows.Application.Current.Properties["DbContext"] as GestLogDbContext;
            }
            catch { }

            // Pasar null si no se encuentra; ViewModel maneja null
            DataContext = new DetallesEquipoInformaticoViewModel(equipo, db);
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
