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
                this.Loaded += (sender, e) => {
                    _logger?.LogInformation("🔄 Ventana cargada, ejecutando LoadConfigurationToUI()");
                    LoadConfigurationToUI();
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
                    _logger?.LogInformation("🔄 LoadConfigurationToUI() ya está en ejecución, saltando...");
                    return;
                }

                _isLoadingConfiguration = true;

                // Obtener la configuración más actualizada desde el servicio
                var latestConfig = _configurationService?.Current?.Smtp;
                if (latestConfig != null)
                {
                    _currentSettings = latestConfig;
                }

                _logger?.LogInformation("🔄 INICIO LoadConfigurationToUI() - StackTrace: {StackTrace}", 
                    new System.Diagnostics.StackTrace().ToString().Split('\n')[1].Trim());
                _logger?.LogInformation("🔍 DATOS RECIBIDOS: Server='{Server}', Username='{Username}', Port={Port}, UseSSL={UseSSL}, IsConfigured={IsConfigured}", 
                    _currentSettings?.Server ?? "[NULL]", _currentSettings?.Username ?? "[NULL]", _currentSettings?.Port ?? 0, _currentSettings?.UseSSL ?? false, _currentSettings?.IsConfigured ?? false);
                    
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
                    hostTextBox != null, portTextBox != null, sslCheckBox != null, emailTextBox != null, passwordBox != null);

                if (hostTextBox != null)
                {
                    hostTextBox.Text = _currentSettings.Server ?? string.Empty;
                    _logger?.LogInformation("🔄 HostTextBox asignado: '{Value}'", hostTextBox.Text);
                }
                
                if (portTextBox != null)
                {
                    portTextBox.Text = _currentSettings.Port.ToString();
                    _logger?.LogInformation("🔄 PortTextBox asignado: '{Value}'", portTextBox.Text);
                }
                
                if (sslCheckBox != null)
                {
                    sslCheckBox.IsChecked = _currentSettings.UseSSL;
                    _logger?.LogInformation("🔄 SslCheckBox asignado: {Value}", sslCheckBox.IsChecked);
                }
                  if (emailTextBox != null)
                {
                    emailTextBox.Text = _currentSettings.Username ?? string.Empty;
                    _logger?.LogInformation("🔄 EmailTextBox asignado: '{Value}'", emailTextBox.Text);
                }                // Cargar campos BCC y CC desde la configuración
                _logger?.LogInformation("📧 CARGANDO CAMPOS BCC/CC:");
                _logger?.LogInformation("   💭 BccEmail en configuración: '{BccValue}'", _currentSettings.BccEmail ?? "[NULL]");
                _logger?.LogInformation("   💭 CcEmail en configuración: '{CcValue}'", _currentSettings.CcEmail ?? "[NULL]");
                _logger?.LogInformation("   🔍 BccEmailTextBox encontrado: {BccFound}", bccEmailTextBox != null);
                _logger?.LogInformation("   🔍 CcEmailTextBox encontrado: {CcFound}", ccEmailTextBox != null);
                
                if (bccEmailTextBox != null)
                {
                    var bccValue = _currentSettings.BccEmail ?? string.Empty;
                    bccEmailTextBox.Text = bccValue;
                    _logger?.LogInformation("🔄 BccEmailTextBox asignado: '{AssignedValue}' (length: {Length})", bccValue, bccValue.Length);
                    
                    // Verificar que la asignación fue exitosa
                    var verifyBcc = bccEmailTextBox.Text;
                    _logger?.LogInformation("🔍 BccEmailTextBox verificación: '{VerifyValue}' - Match: {IsMatch}", verifyBcc, verifyBcc == bccValue);
                }
                else
                {
                    _logger?.LogWarning("⚠️ BccEmailTextBox es NULL - no se puede asignar valor");
                }

                if (ccEmailTextBox != null)
                {
                    var ccValue = _currentSettings.CcEmail ?? string.Empty;
                    ccEmailTextBox.Text = ccValue;
                    _logger?.LogInformation("🔄 CcEmailTextBox asignado: '{Value}'", ccEmailTextBox.Text);
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
                        
                        _logger?.LogInformation("🔐 ✅ Credenciales SMTP cargadas desde Windows Credential Manager con target: '{Target}'", credentialTarget);
                    }
                    else
                    {
                        _logger?.LogInformation("⚠️ No se encontraron credenciales guardadas para target: '{Target}'", credentialTarget);
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
            }
            finally
            {
                _isLoadingConfiguration = false;
                _logger?.LogInformation("🔄 LoadConfigurationToUI() completado");
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
                _logger?.LogInformation("🔄 INICIO SaveConfigurationAsync()");
                
                if (!_isTestSuccessful)
                {
                    _logger?.LogWarning("⚠️ SALIENDO: Test no exitoso");
                    System.Windows.MessageBox.Show("Debe probar la configuración antes de guardarla.", 
                                  "Validación requerida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
                var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
                var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;                var emailTextBox = this.FindName("EmailTextBox") as System.Windows.Controls.TextBox;
                var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
                var saveCredentialsCheckBox = this.FindName("SaveCredentialsCheckBox") as System.Windows.Controls.CheckBox;
                var bccEmailTextBox = this.FindName("BccEmailTextBox") as System.Windows.Controls.TextBox;
                var ccEmailTextBox = this.FindName("CcEmailTextBox") as System.Windows.Controls.TextBox;

                _logger?.LogInformation("🔍 CONTROLES GUARDADO: Host={HasHost}, Email={HasEmail}, Password={HasPassword}, SaveCredentials={HasSaveCredentials}, BCC={HasBcc}, CC={HasCc}",
                    hostTextBox != null, emailTextBox != null, passwordBox != null, saveCredentialsCheckBox != null, bccEmailTextBox != null, ccEmailTextBox != null);

                // Obtener email una vez
                var email = emailTextBox?.Text?.Trim() ?? string.Empty;
                var password = passwordBox?.Password ?? string.Empty;
                var shouldSaveCredentials = saveCredentialsCheckBox?.IsChecked ?? false;
                var bccEmail = bccEmailTextBox?.Text?.Trim() ?? string.Empty;
                var ccEmail = ccEmailTextBox?.Text?.Trim() ?? string.Empty;
                
                _logger?.LogInformation("🔍 VALORES OBTENIDOS: Email='{Email}', HasPassword={HasPassword}, ShouldSaveCredentials={ShouldSave}, BCC='{BccEmail}', CC='{CcEmail}'",
                    email, !string.IsNullOrEmpty(password), shouldSaveCredentials, bccEmail, ccEmail);// Actualizar configuración persistente
                var oldServer = _currentSettings.Server;
                var oldUsername = _currentSettings.Username;
                
                _currentSettings.Server = hostTextBox?.Text?.Trim() ?? string.Empty;
                _currentSettings.Port = int.TryParse(portTextBox?.Text?.Trim(), out int port) ? port : 587;
                _currentSettings.UseSSL = sslCheckBox?.IsChecked ?? true;
                _currentSettings.UseAuthentication = !string.IsNullOrEmpty(email);                _currentSettings.Username = email;     // Email principal
                _currentSettings.FromEmail = email;    // Mismo email (REQUERIDO para validación)
                _currentSettings.FromName = email;     // Nombre del remitente
                _currentSettings.BccEmail = bccEmail;  // Email para copia oculta
                _currentSettings.CcEmail = ccEmail;    // Email para copia
                _currentSettings.IsConfigured = true;  // Marcar como configurado

                _logger?.LogInformation("🔄 CONFIGURACIÓN ACTUALIZADA: Server='{NewServer}' (antes:'{OldServer}'), Username='{NewUsername}' (antes:'{OldUsername}'), Port={Port}, UseSSL={UseSSL}, BCC='{BccEmail}', CC='{CcEmail}'",
                    _currentSettings.Server, oldServer, _currentSettings.Username, oldUsername, _currentSettings.Port, _currentSettings.UseSSL, _currentSettings.BccEmail, _currentSettings.CcEmail);

                // ✅ CORRECCIÓN: Actualizar la configuración del servicio de configuración
                var serviceSmtpConfig = _configurationService.Current.Smtp;
                serviceSmtpConfig.Server = _currentSettings.Server;
                serviceSmtpConfig.Port = _currentSettings.Port;
                serviceSmtpConfig.UseSSL = _currentSettings.UseSSL;
                serviceSmtpConfig.UseAuthentication = _currentSettings.UseAuthentication;                serviceSmtpConfig.Username = _currentSettings.Username;
                serviceSmtpConfig.FromEmail = _currentSettings.FromEmail;
                serviceSmtpConfig.FromName = _currentSettings.FromName;
                serviceSmtpConfig.BccEmail = _currentSettings.BccEmail;
                serviceSmtpConfig.CcEmail = _currentSettings.CcEmail;
                serviceSmtpConfig.IsConfigured = _currentSettings.IsConfigured;

                _logger?.LogInformation("🔄 ✅ Configuración del servicio actualizada: Server='{Server}', Username='{Username}', IsConfigured={IsConfigured}, BCC='{BccEmail}', CC='{CcEmail}'",
                    serviceSmtpConfig.Server, serviceSmtpConfig.Username, serviceSmtpConfig.IsConfigured, serviceSmtpConfig.BccEmail, serviceSmtpConfig.CcEmail);

                // Guardar credenciales si se solicita
                if (shouldSaveCredentials && !string.IsNullOrEmpty(email))
                {
                    _logger?.LogInformation("🔐 GUARDANDO CREDENCIALES...");
                    var credentialTarget = $"SMTP_{_currentSettings.Server}_{email}";
                    _logger?.LogInformation("🔐 Target calculado: '{CredentialTarget}'", credentialTarget);
                    
                    var saved = _credentialService?.SaveCredentials(
                        credentialTarget,
                        email,
                        password) ?? false;

                    if (saved)
                    {
                        _logger?.LogInformation("🔐 ✅ Credenciales SMTP guardadas exitosamente en Windows Credential Manager con target: '{Target}'", credentialTarget);
                    }
                    else
                    {
                        _logger?.LogWarning("🔐 ❌ ERROR: No se pudieron guardar las credenciales SMTP en Windows Credential Manager");
                        System.Windows.MessageBox.Show("Error al guardar las credenciales de forma segura. Inténtelo nuevamente.", 
                                      "Error de Credenciales", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    }
                }
                else
                {
                    _logger?.LogInformation("🔐 ⏭️ NO se guardarán credenciales: ShouldSave={ShouldSave}, HasEmail={HasEmail}",
                        shouldSaveCredentials, !string.IsNullOrEmpty(email));
                }                // Guardar configuración
                _logger?.LogInformation("💾 Guardando configuración en archivo...");
                await _configurationService.SaveAsync();
                _logger?.LogInformation("💾 ✅ Configuración guardada exitosamente en archivo");
                _logger?.LogInformation("💾 ✅ Configuración guardada exitosamente en archivo");

                this.DialogResult = true;
                _logger?.LogInformation("✅ SaveConfigurationAsync completado exitosamente");
                Close();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ ERROR en SaveConfigurationAsync: {ErrorMessage}", ex.Message);
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
