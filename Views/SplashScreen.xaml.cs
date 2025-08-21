using System.Windows;
using System.Windows.Threading;

namespace GestLog.Views
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            this.Loaded += SplashScreen_Loaded;
        }

        public void ShowStatus(string message)
        {
            StatusText.Text = message;
            StatusText.Visibility = Visibility.Visible;
        }

        public void ShowUpdateButtons()
        {
            var panel = this.FindName("UpdateButtonsPanel") as System.Windows.Controls.StackPanel;
            if (panel != null)
                panel.Visibility = Visibility.Visible;
        }

        public void HideUpdateButtons()
        {
            var panel = this.FindName("UpdateButtonsPanel") as System.Windows.Controls.StackPanel;
            if (panel != null)
                panel.Visibility = Visibility.Collapsed;
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // Indica que el usuario quiere actualizar
            this.Close();
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indica que el usuario quiere omitir la actualización
            this.Close();
        }

        private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            // Eliminar cierre automático por timer: el SplashScreen solo se cierra manualmente desde App.xaml.cs
        }
    }
}
