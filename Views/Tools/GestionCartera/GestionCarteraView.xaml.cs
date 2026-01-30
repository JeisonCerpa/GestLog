using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.GestionCartera.ViewModels;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.Security;
using GestLog.Services.Configuration;
using GestLog.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionCartera
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
                // Cambiar a la segunda pestaña (índice 1) que es "🚀 Envío Automático"
                mainTabControl.SelectedIndex = 1;
                
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
                var credentialService = serviceProvider.GetRequiredService<ICredentialService>();
                var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
                var logger = serviceProvider.GetRequiredService<IGestLogLogger>();
                var viewModel = DataContext as DocumentGenerationViewModel;                // Crear configuración actual del ViewModel
                var currentSettings = new Models.Configuration.SmtpSettings
                {
                    Server = viewModel?.SmtpServer ?? string.Empty,
                    Port = viewModel?.SmtpPort ?? 587,
                    UseSSL = viewModel?.EnableSsl ?? true,
                    Username = viewModel?.SmtpUsername ?? string.Empty,
                    Password = viewModel?.SmtpPassword ?? string.Empty,
                    UseAuthentication = !string.IsNullOrEmpty(viewModel?.SmtpUsername),
                    FromEmail = viewModel?.SmtpUsername ?? string.Empty,
                    IsConfigured = viewModel?.IsEmailConfigured ?? false
                };                // Abrir ventana de configuración
                var smtpPersistenceService = LoggingService.GetService<ISmtpPersistenceService>();
                var configWindow = new SmtpConfigurationWindow(
                    currentSettings, 
                    emailService, 
                    credentialService, 
                    configurationService,
                    smtpPersistenceService,
                    logger)
                {
                    Owner = Window.GetWindow(this)
                };

                if (configWindow.ShowDialog() == true)
                {
                    // Actualizar ViewModel con la nueva configuración
                    var newSettings = configWindow.Settings;
                    if (viewModel != null)
                    {
                        viewModel.SmtpServer = newSettings.Server;
                        viewModel.SmtpPort = newSettings.Port;
                        viewModel.EnableSsl = newSettings.UseSSL;
                        viewModel.SmtpUsername = newSettings.Username;
                        viewModel.SmtpPassword = newSettings.Password;
                        viewModel.IsEmailConfigured = newSettings.IsConfigured;

                        logger.LogInformation("Configuración SMTP actualizada desde ventana de configuración");
                    }
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
