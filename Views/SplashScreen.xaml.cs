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

        private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            // Eliminar cierre automático por timer: el SplashScreen solo se cierra manualmente desde App.xaml.cs
        }
    }
}
