using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.GestionCartera.ViewModels;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.Security;
using GestLog.Services.Configuration;
using GestLog.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionCartera.Views
{
    public partial class GestionCarteraView : System.Windows.Controls.UserControl
    {
        public GestionCarteraView()
        {
            InitializeComponent();
            
            // Usar inyección de dependencias para obtener el ViewModel
            var serviceProvider = LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<DocumentGenerationViewModel>();
            DataContext = viewModel;
        }        /// <summary>
        /// Evento para manejar el cambio de contraseña en el PasswordBox
        /// </summary>
        private void SmtpPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && DataContext is DocumentGenerationViewModel viewModel)
            {
                viewModel.SmtpPassword = passwordBox.Password;
            }
        }        /// <summary>
        /// Manejador para cambiar a la pestaña de envío de correos
        /// </summary>
        private void GoToEmailTab_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Llevar al bloque de envío automático en la vista command-center
                EmailSectionCard?.BringIntoView();
                
                // Ejecutar el comando del ViewModel para logging y limpieza
                if (DataContext is DocumentGenerationViewModel viewModel)
                {
                    viewModel.GoToEmailTabCommand.Execute(null);
                }
            }
            catch (System.Exception ex)
            {
                var logger = LoggingService.GetServiceProvider().GetRequiredService<IGestLogLogger>();
                logger.LogError(ex, "Error al navegar a la pestaña de envío de correos");
                
                System.Windows.MessageBox.Show($"Error al navegar a la pestaña de envío: {ex.Message}", 
                           "Error", 
                           System.Windows.MessageBoxButton.OK, 
                           System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Manejador para abrir la ventana de configuración SMTP
        /// </summary>
        private void ConfigureSmtp_Click(object sender, RoutedEventArgs e)
        {
            try
            {                var serviceProvider = LoggingService.GetServiceProvider();
                var emailService = serviceProvider.GetRequiredService<IEmailService>();
                var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
                var logger = serviceProvider.GetRequiredService<IGestLogLogger>();
                var viewModel = DataContext as DocumentGenerationViewModel;

                // La ventana lee su estado del almacén único; no hay que pasarle una copia.
                var smtpPersistenceService = LoggingService.GetService<ISmtpPersistenceService>();
                var configWindow = new SmtpConfigurationWindow(
                    emailService,
                    configurationService,
                    smtpPersistenceService,
                    logger)
                {
                    Owner = Window.GetWindow(this)
                };

                if (configWindow.ShowDialog() == true && viewModel != null)
                {
                    // Recargar desde el almacén en vez de copiar campo a campo (así BCC/CC no quedan atrás).
                    viewModel.ReloadSmtpConfigurationCommand.Execute(null);
                    logger.LogInformation("Configuración SMTP actualizada desde ventana de configuración");
                }
            }
            catch (System.Exception ex)
            {
                var logger = LoggingService.GetServiceProvider().GetRequiredService<IGestLogLogger>();
                logger.LogError(ex, "Error al abrir ventana de configuración SMTP");
                  System.Windows.MessageBox.Show($"Error al abrir configuración SMTP: {ex.Message}", 
                               "Error", 
                               System.Windows.MessageBoxButton.OK, 
                               System.Windows.MessageBoxImage.Error);
            }
        }
    }
}

