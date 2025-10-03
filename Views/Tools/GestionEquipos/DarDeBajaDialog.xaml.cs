using System.Windows;

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class DarDeBajaDialog : Window
    {
        // Cambiado: setter público para permitir binding TwoWay desde XAML
        public string? Observacion { get; set; }
        public DarDeBajaDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Con binding TwoWay y UpdateSourceTrigger=PropertyChanged, Observacion ya contiene el texto actual.
            // Asegurar no-null
            Observacion = Observacion?.Trim();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
