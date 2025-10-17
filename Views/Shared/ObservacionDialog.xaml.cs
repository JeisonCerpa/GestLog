using System.Windows;

namespace GestLog.Views.Shared
{
    public partial class ObservacionDialog : Window
    {
        public string Observacion { get; private set; } = string.Empty;

        public ObservacionDialog(string? existing = null)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(existing))
                ObservacionTextBox.Text = existing;
            ObservacionTextBox.Focus();
        }

        private void OnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            var txt = ObservacionTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(txt))
            {
                System.Windows.MessageBox.Show("La observación es obligatoria para dar de baja.", "Observación requerida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                ObservacionTextBox.Focus();
                return;
            }

            Observacion = txt;
            DialogResult = true;
            Close();
        }
    }
}
