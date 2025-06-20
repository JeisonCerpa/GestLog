using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.EnvioCatalogo.ViewModels;
using GestLog.Modules.EnvioCatalogo.Services;
using GestLog.Services.Core.Logging;
using GestLog.Services.Configuration;
using GestLog.Services.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.EnvioCatalogo
{    /// <summary>
    /// Vista para el módulo de Envío de Catálogo
    /// </summary>
    public partial class EnvioCatalogoView : System.Windows.Controls.UserControl
    {        public EnvioCatalogoView()
        {
            InitializeComponent();
            
            // Configurar ViewModel con DI completa
            var serviceProvider = LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<EnvioCatalogoViewModel>();
            DataContext = viewModel;
        }

        /// <summary>
        /// Rellena los campos con datos de prueba
        /// </summary>
        private void BtnUsarDatosPrueba_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EnvioCatalogoViewModel viewModel)
            {
                viewModel.ClientName = "CLIENTE 123";
                viewModel.ClientNIT = "12345698-3";
            }
        }        /// <summary>
        /// Manejador para el cambio de contraseña SMTP
        /// </summary>
        private void SmtpPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && DataContext is EnvioCatalogoViewModel viewModel)
            {
                viewModel.SmtpPassword = passwordBox.Password;
                
                // Actualizar estado de configuración
                viewModel.IsSmtpConfigured = !string.IsNullOrWhiteSpace(viewModel.SmtpServer) &&
                                           !string.IsNullOrWhiteSpace(viewModel.SmtpUsername) &&
                                           !string.IsNullOrWhiteSpace(passwordBox.Password);
            }
        }        /// <summary>
        /// Manejador para abrir la ventana de configuración SMTP
        /// </summary>
        private void ConfigureSmtp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is EnvioCatalogoViewModel viewModel)
                {
                    var configWindow = new SmtpConfigurationWindow(viewModel)
                    {
                        Owner = Window.GetWindow(this)
                    };

                    if (configWindow.ShowDialog() == true)
                    {
                        var serviceProvider = LoggingService.GetServiceProvider();
                        var logger = serviceProvider.GetRequiredService<IGestLogLogger>();
                        logger.LogInformation("Configuración SMTP actualizada desde ventana de configuración");
                    }
                }
            }
            catch (System.Exception ex)
            {
                var logger = LoggingService.GetServiceProvider().GetRequiredService<IGestLogLogger>();
                logger.LogError(ex, "Error al abrir configuración SMTP");
                System.Windows.MessageBox.Show($"Error al abrir configuración SMTP: {ex.Message}", 
                               "Error", 
                               System.Windows.MessageBoxButton.OK, 
                               System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
