using System;
using System.Windows;
using System.Windows.Media;
using GestLog.Modules.EnvioCatalogo.ViewModels;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.EnvioCatalogo
{
    /// <summary>
    /// Ventana de configuración SMTP para el módulo de Envío de Catálogo
    /// </summary>
    public partial class SmtpConfigurationWindow : Window
    {
        private readonly EnvioCatalogoViewModel _viewModel;
        private readonly IGestLogLogger _logger;

        public SmtpConfigurationWindow(EnvioCatalogoViewModel viewModel)
        {
            InitializeComponent();
            
            _viewModel = viewModel;
            
            var serviceProvider = LoggingService.GetServiceProvider();
            _logger = serviceProvider.GetRequiredService<IGestLogLogger>();
            
            LoadCurrentConfiguration();
            UpdateStatus();
        }

        /// <summary>
        /// Carga la configuración actual en los campos
        /// </summary>
        private void LoadCurrentConfiguration()
        {
            try
            {
                ServerTextBox.Text = _viewModel.SmtpServer ?? "";
                PortTextBox.Text = _viewModel.SmtpPort > 0 ? _viewModel.SmtpPort.ToString() : "587";
                EmailTextBox.Text = _viewModel.SmtpUsername ?? "";
                SslCheckBox.IsChecked = _viewModel.EnableSsl;
                
                _logger.LogInformation("Configuración SMTP cargada en ventana de configuración");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar configuración SMTP en ventana");
            }
        }

        /// <summary>
        /// Preset para Gmail
        /// </summary>
        private void GmailPreset_Click(object sender, RoutedEventArgs e)
        {
            ServerTextBox.Text = "smtp.gmail.com";
            PortTextBox.Text = "587";
            SslCheckBox.IsChecked = true;
            UpdateStatus();
            _logger.LogInformation("Preset Gmail aplicado");
        }        /// <summary>
        /// Preset para Zoho
        /// </summary>
        private void ZohoPreset_Click(object sender, RoutedEventArgs e)
        {
            ServerTextBox.Text = "smtppro.zoho.com";
            PortTextBox.Text = "587";
            SslCheckBox.IsChecked = true;
            UpdateStatus();
            _logger.LogInformation("Preset Zoho aplicado");
        }

        /// <summary>
        /// Preset para Office 365
        /// </summary>
        private void Office365Preset_Click(object sender, RoutedEventArgs e)
        {
            ServerTextBox.Text = "smtp.office365.com";
            PortTextBox.Text = "587";
            SslCheckBox.IsChecked = true;
            UpdateStatus();
            _logger.LogInformation("Preset Office 365 aplicado");
        }

        /// <summary>
        /// Guarda la configuración
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar campos obligatorios
                if (string.IsNullOrWhiteSpace(ServerTextBox.Text))
                {
                    System.Windows.MessageBox.Show("El servidor SMTP es obligatorio.", "Error de validación", 
                                   System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    System.Windows.MessageBox.Show("El email/usuario es obligatorio.", "Error de validación", 
                                   System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    System.Windows.MessageBox.Show("La contraseña es obligatoria.", "Error de validación", 
                                   System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(PortTextBox.Text, out int port) || port <= 0 || port > 65535)
                {
                    System.Windows.MessageBox.Show("El puerto debe ser un número válido entre 1 y 65535.", "Error de validación", 
                                   System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Guardar en el ViewModel
                _viewModel.SmtpServer = ServerTextBox.Text.Trim();
                _viewModel.SmtpPort = port;
                _viewModel.SmtpUsername = EmailTextBox.Text.Trim();
                _viewModel.SmtpPassword = PasswordBox.Password;
                _viewModel.EnableSsl = SslCheckBox.IsChecked ?? true;
                
                // Actualizar estado de configuración
                _viewModel.IsSmtpConfigured = true;

                _logger.LogInformation("Configuración SMTP guardada exitosamente para Envío de Catálogo");
                
                System.Windows.MessageBox.Show("Configuración SMTP guardada correctamente.", "Éxito", 
                               System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar configuración SMTP");
                System.Windows.MessageBox.Show($"Error al guardar la configuración: {ex.Message}", "Error", 
                               System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cancela la configuración
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Actualiza el estado visual de la configuración
        /// </summary>
        private void UpdateStatus()
        {
            bool isConfigured = !string.IsNullOrWhiteSpace(ServerTextBox.Text) &&
                               !string.IsNullOrWhiteSpace(EmailTextBox.Text) &&
                               !string.IsNullOrWhiteSpace(PasswordBox.Password) &&
                               int.TryParse(PortTextBox.Text, out int port) && port > 0;

            if (isConfigured)
            {
                StatusIndicator.Fill = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#118938"));
                StatusText.Text = "Configuración completa";
                StatusText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#118938"));
            }
            else
            {
                StatusIndicator.Fill = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#DC3545"));
                StatusText.Text = "Configuración incompleta";
                StatusText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#DC3545"));
            }
        }

        /// <summary>
        /// Manejadores de eventos para actualizar el estado en tiempo real
        /// </summary>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            
            // Agregar manejadores para actualizar estado en tiempo real
            ServerTextBox.TextChanged += (s, e) => UpdateStatus();
            PortTextBox.TextChanged += (s, e) => UpdateStatus();
            EmailTextBox.TextChanged += (s, e) => UpdateStatus();
            PasswordBox.PasswordChanged += (s, e) => UpdateStatus();
        }
    }
}
