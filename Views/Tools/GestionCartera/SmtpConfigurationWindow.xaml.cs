using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GestLog.Modules.GestionCartera.Models;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Services.Core.Security;
using GestLog.Services.Core.Logging;
using GestLog.Services.Configuration;
using GestLog.Models.Configuration;

namespace GestLog.Views.Tools.GestionCartera
{
    /// <summary>
    /// Ventana de configuración SMTP - Versión corregida
    /// </summary>
    public partial class SmtpConfigurationWindow : Window
    {        private readonly IEmailService _emailService;
        private readonly ICredentialService _credentialService;
        private readonly IConfigurationService _configurationService;
        private readonly IGestLogLogger _logger = null!;
        private SmtpSettings _currentSettings;
        private bool _isTestSuccessful = false;
        
        public SmtpSettings Settings => _currentSettings;        public SmtpConfigurationWindow(
            IEmailService emailService, 
            ICredentialService credentialService, 
            IConfigurationService configurationService,
            IGestLogLogger logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));            try
            {
                InitializeComponent();
                
                _currentSettings = _configurationService.Current.Smtp ?? new SmtpSettings();
                
                _logger?.LogInformation("Configuración SMTP encontrada: Server={Server}, IsConfigured={IsConfigured}", 
                    _currentSettings.Server ?? "", _currentSettings.IsConfigured);
                
                // Suscribirse al evento Loaded SOLO para cargar la configuración cuando la ventana esté completamente inicializada
                this.Loaded += (sender, e) => {                LoadConfigurationToUI();
                    UpdateUI();
                };
                
                // NO cargar inmediatamente - esperar a que la ventana se cargue completamente
                _logger?.LogInformation("Constructor SmtpConfigurationWindow completado - esperando evento Loaded");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error inicializando SmtpConfigurationWindow");
                throw;
            }
        }

        public SmtpConfigurationWindow(
            SmtpSettings settings, 
            IEmailService emailService, 
            ICredentialService credentialService, 
            IConfigurationService configurationService,
            IGestLogLogger logger)
            : this(emailService, credentialService, configurationService, logger)
        {
            _currentSettings = settings ?? new SmtpSettings();
            LoadConfigurationToUI();
        }

        #region Event Handlers

        private void OnFieldChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.IsLoaded && _currentSettings != null)
                {
                    UpdateUI();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Error en OnFieldChanged: {ErrorMessage}", ex.Message);
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.IsLoaded && _currentSettings != null)
                {
                    UpdateUI();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Error en OnPasswordChanged: {ErrorMessage}", ex.Message);
            }
        }

        private void GmailPreset_Click(object sender, RoutedEventArgs e)
        {
            ApplyGmailPreset();
            UpdateUI();
        }        private void ZohoPreset_Click(object sender, RoutedEventArgs e)
        {
            ApplyZohoPreset();
            UpdateUI();
        }

        private void Office365Preset_Click(object sender, RoutedEventArgs e)
        {
            ApplyOffice365Preset();
            UpdateUI();
        }

        private async void TestConfiguration_Click(object sender, RoutedEventArgs e)
        {
            await TestConfigurationAsync();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await SaveConfigurationAsync();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        #endregion

        #region Private Methods

        private void ApplyGmailPreset()
        {
            var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
            var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
            var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;

            if (hostTextBox != null && portTextBox != null && sslCheckBox != null)
            {
                hostTextBox.Text = "smtp.gmail.com";
                portTextBox.Text = "587";
                sslCheckBox.IsChecked = true;
                UpdateStatus("Configuración de Gmail aplicada", Colors.Orange);
                _logger?.LogInformation("Aplicada configuración predefinida: Gmail");
            }
        }        private void ApplyZohoPreset()
        {
            var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
            var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
            var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;

            if (hostTextBox != null && portTextBox != null && sslCheckBox != null)
            {
                hostTextBox.Text = "smtppro.zoho.com";
                portTextBox.Text = "587";
                sslCheckBox.IsChecked = true;
                UpdateStatus("Configuración de Zoho aplicada", Colors.Orange);
                _logger?.LogInformation("Aplicada configuración predefinida: Zoho");
            }
        }

        private void ApplyOffice365Preset()
        {
            var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
            var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
            var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;

            if (hostTextBox != null && portTextBox != null && sslCheckBox != null)
            {
                hostTextBox.Text = "smtp.office365.com";
                portTextBox.Text = "587";
                sslCheckBox.IsChecked = true;
                UpdateStatus("Configuración de Office 365 aplicada", Colors.Orange);
                _logger?.LogInformation("Aplicada configuración predefinida: Office 365");
            }        }

        private bool _isLoadingConfiguration = false;
        
        private void LoadConfigurationToUI()
        {
            try
            {
            // Protección contra múltiples ejecuciones simultáneas
            if (_isLoadingConfiguration)
            {
                return;
            }
            
            _isLoadingConfiguration = true;

                // Obtener la configuración más actualizada desde el servicio
                var latestConfig = _configurationService?.Current?.Smtp;
                if (latestConfig != null)
                {
                    _currentSettings = latestConfig;
                }
                    
                if (_currentSettings == null) 
                {
                    _logger?.LogWarning("⚠️ SALIENDO: CurrentSettings es null");
                    return;
                }var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
                var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
                var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;
                var emailTextBox = this.FindName("EmailTextBox") as System.Windows.Controls.TextBox;
                var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
                var saveCredentialsCheckBox = this.FindName("SaveCredentialsCheckBox") as System.Windows.Controls.CheckBox;
                var bccEmailTextBox = this.FindName("BccEmailTextBox") as System.Windows.Controls.TextBox;
                var ccEmailTextBox = this.FindName("CcEmailTextBox") as System.Windows.Controls.TextBox;

                // Verificar que los controles críticos existan antes de continuar
                if (hostTextBox == null || emailTextBox == null)
                {
                    _logger?.LogWarning("⚠️ SALIENDO: Controles críticos no encontrados (HostTextBox={HasHost}, EmailTextBox={HasEmail})", 
                        hostTextBox != null, emailTextBox != null);
                    return;
                }

                _logger?.LogInformation("🔍 CONTROLES ENCONTRADOS: Host={HasHost}, Port={HasPort}, SSL={HasSSL}, Email={HasEmail}, Password={HasPassword}",
                    hostTextBox != null, portTextBox != null, sslCheckBox != null, emailTextBox != null, passwordBox != null);                if (hostTextBox != null)
                {
                    hostTextBox.Text = _currentSettings.Server ?? string.Empty;
                }
                  if (portTextBox != null)
                {
                    portTextBox.Text = _currentSettings.Port.ToString();
                }
                  if (sslCheckBox != null)
                {
                    sslCheckBox.IsChecked = _currentSettings.UseSSL;
                }                if (emailTextBox != null)
                {
                    emailTextBox.Text = _currentSettings.Username ?? string.Empty;
                }                // Cargar campos BCC y CC desde la configuración
                if (bccEmailTextBox != null)
                {
                    var bccValue = _currentSettings.BccEmail ?? string.Empty;
                    bccEmailTextBox.Text = bccValue;
                    
                    // Verificar que la asignación fue exitosa
                    var verifyBcc = bccEmailTextBox.Text;
                }
                else
                {
                    _logger?.LogWarning("⚠️ BccEmailTextBox es NULL - no se puede asignar valor");
                }                if (ccEmailTextBox != null)
                {
                    var ccValue = _currentSettings.CcEmail ?? string.Empty;
                    ccEmailTextBox.Text = ccValue;
                }// Cargar credenciales guardadas si existen
                if (_currentSettings.UseAuthentication && !string.IsNullOrEmpty(_currentSettings.Username))
                {
                    var credentialTarget = $"SMTP_{_currentSettings.Server}_{_currentSettings.Username}";
                    _logger?.LogInformation("🔍 Buscando credenciales con target: '{CredentialTarget}'", credentialTarget);
                    
                    var credentials = _credentialService?.GetCredentials(credentialTarget);
                    
                    if (credentials.HasValue && !string.IsNullOrEmpty(credentials.Value.username) && !string.IsNullOrEmpty(credentials.Value.password))
                    {
                        if (saveCredentialsCheckBox != null)
                            saveCredentialsCheckBox.IsChecked = true;
                        
                        if (passwordBox != null)
                            passwordBox.Password = credentials.Value.password;
                        
                        // Asignar la contraseña recuperada al objeto de configuración                        _currentSettings.Password = credentials.Value.password;
                        
                        _logger?.LogInformation("Credenciales SMTP cargadas desde Windows Credential Manager");
                    }
                    else
                    {
                        _logger?.LogInformation("No se encontraron credenciales guardadas");
                    }
                }
                else
                {
                    if (_currentSettings.UseAuthentication && string.IsNullOrEmpty(_currentSettings.Username))
                    {
                        _logger?.LogWarning("⚠️ No se cargarán credenciales: UseAuthentication={UseAuth}, Username vacio={IsEmpty}", 
                            _currentSettings.UseAuthentication, string.IsNullOrEmpty(_currentSettings.Username));
                    }
                }                UpdateStatus(_currentSettings.IsConfigured ? "Configuración cargada" : "No configurado",
                           _currentSettings.IsConfigured ? Colors.Green : Colors.Red);
                
                _logger?.LogInformation("Configuración SMTP cargada en UI - Server: {Server}, Username: {Username}, Configurado: {IsConfigured}", 
                    _currentSettings.Server ?? "", _currentSettings.Username ?? "", _currentSettings.IsConfigured);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al cargar configuración a la UI");
                UpdateStatus("Error al cargar configuración", Colors.Red);
            }            finally
            {
                _isLoadingConfiguration = false;
            }
        }

        private void UpdateUI()
        {
            try
            {
                var testButton = this.FindName("TestButton") as System.Windows.Controls.Button;
                var saveButton = this.FindName("SaveButton") as System.Windows.Controls.Button;

                if (testButton == null || saveButton == null)
                {
                    return;
                }

                var isValidForTest = ValidateForTest();
                var isValidForSave = ValidateForSave();
                
                testButton.IsEnabled = isValidForTest;
                saveButton.IsEnabled = isValidForSave && _isTestSuccessful;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Error en UpdateUI: {ErrorMessage}", ex.Message);
            }
        }

        private bool ValidateForTest()
        {
            try
            {
                var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
                var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
                var emailTextBox = this.FindName("EmailTextBox") as System.Windows.Controls.TextBox;
                var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;

                if (hostTextBox == null || portTextBox == null || emailTextBox == null || passwordBox == null)
                {
                    return false;
                }

                return !string.IsNullOrWhiteSpace(hostTextBox.Text) &&
                       int.TryParse(portTextBox.Text, out int port) && port > 0 && port <= 65535 &&
                       !string.IsNullOrWhiteSpace(emailTextBox.Text) &&
                       !string.IsNullOrWhiteSpace(passwordBox.Password);
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateForSave()
        {
            return ValidateForTest();
        }

        private async Task TestConfigurationAsync()
        {
            try
            {
                var testButton = this.FindName("TestButton") as System.Windows.Controls.Button;
                var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
                var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
                var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;
                var emailTextBox = this.FindName("EmailTextBox") as System.Windows.Controls.TextBox;
                var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;

                UpdateStatus("Probando configuración...", Colors.Orange);
                if (testButton != null)
                    testButton.IsEnabled = false;

                var testConfig = new SmtpConfiguration
                {
                    SmtpServer = hostTextBox?.Text?.Trim() ?? string.Empty,
                    Port = int.TryParse(portTextBox?.Text?.Trim(), out int port) ? port : 587,
                    EnableSsl = sslCheckBox?.IsChecked ?? true,
                    Username = emailTextBox?.Text?.Trim() ?? string.Empty,
                    Password = passwordBox?.Password ?? string.Empty
                };

                await _emailService.ConfigureSmtpAsync(testConfig);
                var isValid = await _emailService.ValidateConfigurationAsync();

                if (isValid)
                {
                    _isTestSuccessful = true;
                    UpdateStatus("✅ Configuración válida", Colors.Green);
                    _logger?.LogInformation("Configuración SMTP validada exitosamente");
                }
                else
                {
                    _isTestSuccessful = false;
                    UpdateStatus("❌ Error en la configuración", Colors.Red);
                    _logger?.LogWarning("Error en validación SMTP");
                }
            }
            catch (Exception ex)
            {
                _isTestSuccessful = false;
                UpdateStatus($"❌ Error: {ex.Message}", Colors.Red);
                _logger?.LogError(ex, "Error al probar configuración SMTP");
            }
            finally
            {
                var testButton = this.FindName("TestButton") as System.Windows.Controls.Button;
                if (testButton != null)
                    testButton.IsEnabled = true;
                UpdateUI();
            }
        }        private async Task SaveConfigurationAsync()
        {
            try
            {
                if (!_isTestSuccessful)
                {
                    System.Windows.MessageBox.Show("Debe probar la configuración antes de guardarla.", 
                                  "Validación requerida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
                var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
                var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;                var emailTextBox = this.FindName("EmailTextBox") as System.Windows.Controls.TextBox;
                var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
                var saveCredentialsCheckBox = this.FindName("SaveCredentialsCheckBox") as System.Windows.Controls.CheckBox;                var bccEmailTextBox = this.FindName("BccEmailTextBox") as System.Windows.Controls.TextBox;
                var ccEmailTextBox = this.FindName("CcEmailTextBox") as System.Windows.Controls.TextBox;

                // Obtener email una vez
                var email = emailTextBox?.Text?.Trim() ?? string.Empty;
                var password = passwordBox?.Password ?? string.Empty;
                var shouldSaveCredentials = saveCredentialsCheckBox?.IsChecked ?? false;
                var bccEmail = bccEmailTextBox?.Text?.Trim() ?? string.Empty;
                var ccEmail = ccEmailTextBox?.Text?.Trim() ?? string.Empty;// Actualizar configuración persistente
                var oldServer = _currentSettings.Server;
                var oldUsername = _currentSettings.Username;
                
                _currentSettings.Server = hostTextBox?.Text?.Trim() ?? string.Empty;
                _currentSettings.Port = int.TryParse(portTextBox?.Text?.Trim(), out int port) ? port : 587;
                _currentSettings.UseSSL = sslCheckBox?.IsChecked ?? true;
                _currentSettings.UseAuthentication = !string.IsNullOrEmpty(email);                _currentSettings.Username = email;     // Email principal
                _currentSettings.FromEmail = email;    // Mismo email (REQUERIDO para validación)
                _currentSettings.FromName = email;     // Nombre del remitente
                _currentSettings.BccEmail = bccEmail;  // Email para copia oculta                _currentSettings.CcEmail = ccEmail;    // Email para copia
                _currentSettings.IsConfigured = true;  // Marcar como configurado

                // Actualizar la configuración del servicio de configuración
                var serviceSmtpConfig = _configurationService.Current.Smtp;
                serviceSmtpConfig.Server = _currentSettings.Server;
                serviceSmtpConfig.Port = _currentSettings.Port;
                serviceSmtpConfig.UseSSL = _currentSettings.UseSSL;
                serviceSmtpConfig.UseAuthentication = _currentSettings.UseAuthentication;                serviceSmtpConfig.Username = _currentSettings.Username;
                serviceSmtpConfig.FromEmail = _currentSettings.FromEmail;
                serviceSmtpConfig.FromName = _currentSettings.FromName;
                serviceSmtpConfig.BccEmail = _currentSettings.BccEmail;                serviceSmtpConfig.CcEmail = _currentSettings.CcEmail;
                serviceSmtpConfig.IsConfigured = _currentSettings.IsConfigured;

                // Guardar credenciales si se solicita
                if (shouldSaveCredentials && !string.IsNullOrEmpty(email))
                {
                    var credentialTarget = $"SMTP_{_currentSettings.Server}_{email}";

                    // Eliminar cualquier credencial previa para este target antes de guardar la nueva
                    // Esto previene duplicados en el Windows Credential Manager
                    _credentialService?.DeleteCredentials(credentialTarget);

                    var saved = _credentialService?.SaveCredentials(
                        credentialTarget,
                        email,
                        password) ?? false;
                    if (saved)
                    {
                        // Asignar la contraseña guardada al objeto de configuración
                        _currentSettings.Password = password;
                        _logger?.LogInformation("Credenciales SMTP guardadas exitosamente");
                    }
                    else
                    {
                        _logger?.LogWarning("No se pudieron guardar las credenciales SMTP");
                        System.Windows.MessageBox.Show("Error al guardar las credenciales de forma segura. Inténtelo nuevamente.", 
                                      "Error de Credenciales", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    }
                }                // Guardar configuración
                await _configurationService.SaveAsync();

                this.DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error en SaveConfigurationAsync: {ErrorMessage}", ex.Message);
                UpdateStatus($"Error al guardar: {ex.Message}", Colors.Red);
                System.Windows.MessageBox.Show($"Error al guardar la configuración: {ex.Message}", 
                              "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(string message, System.Windows.Media.Color color)
        {
            try
            {
                var statusTextBlock = this.FindName("StatusTextBlock") as TextBlock;
                var statusIndicator = this.FindName("StatusIndicator") as System.Windows.Shapes.Ellipse;

                if (statusTextBlock != null)
                    statusTextBlock.Text = message;
                
                if (statusIndicator != null)
                    statusIndicator.Fill = new SolidColorBrush(color);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Error actualizando status: {ErrorMessage}", ex.Message);
            }
        }

        #endregion
    }
}
