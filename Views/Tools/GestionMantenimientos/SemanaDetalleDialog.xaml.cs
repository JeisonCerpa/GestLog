using System.Windows;
using GestLog.Modules.GestionMantenimientos.Models;
using System.Collections.ObjectModel;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    public partial class SemanaDetalleDialog : Window
    {
        public SemanaDetalleDialog(SemanaDetalleViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void OnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class SemanaDetalleViewModel
    {
        public string Titulo { get; set; } = "";
        public string RangoFechas { get; set; } = "";
        public ObservableCollection<CronogramaMantenimientoDto> Mantenimientos { get; set; } = new();
        public SemanaDetalleViewModel(string titulo, string rango, ObservableCollection<CronogramaMantenimientoDto> mantenimientos)
        {
            Titulo = titulo;
            RangoFechas = rango;
            Mantenimientos = mantenimientos;
        }
    }
}
