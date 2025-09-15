// filepath: e:\Softwares\GestLog\Views\Tools\GestionEquipos\RegistroEjecucionPlanDialog.xaml.cs
using System;
using System.Windows;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class RegistroEjecucionPlanDialog : Window
    {
        public bool Guardado { get; private set; }
        public RegistroEjecucionPlanDialog(RegistroEjecucionPlanViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.OnEjecucionRegistrada += (s,e)=> { Guardado = true; DialogResult = true; Close(); };
        }
        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
