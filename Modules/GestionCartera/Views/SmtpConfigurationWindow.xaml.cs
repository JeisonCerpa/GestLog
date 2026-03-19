using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GestLog.Models.Configuration;
using GestLog.Modules.GestionCartera.Models;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Services.Configuration;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.Security;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using TextBox = System.Windows.Controls.TextBox;

namespace GestLog.Modules.GestionCartera.Views
{
    /// <summary>
    /// Ventana de configuración SMTP.
    /// </summary>
    public partial class SmtpConfigurationWindow : Window
    {
        private readonly IEmailService _emailService;
        private readonly ICredentialService _credentialService;
        private readonly IConfigurationService _configurationService;
        private readonly ISmtpPersistenceService _smtpPersistenceService;
        private readonly IGestLogLogger _logger;

        private SmtpSettings _currentSettings;
        private bool _isTestSuccessful;
        private bool _isLoadingConfiguration;

        public ObservableCollection<string> BccEmails { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> CcEmails { get; } = new ObservableCollection<string>();

        public SmtpSettings Settings => _currentSettings;

        public SmtpConfigurationWindow(
            IEmailService emailService,
            ICredentialService credentialService,
            IConfigurationService configurationService,
            ISmtpPersistenceService smtpPersistenceService,
            IGestLogLogger logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _smtpPersistenceService = smtpPersistenceService ?? throw new ArgumentNullException(nameof(smtpPersistenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponent();

            DataContext = this;
            _currentSettings = _configurationService.Current.Modules.GestionCartera.Smtp ?? new SmtpSettings();

            BccEmails.CollectionChanged += (_, _) =>
            {
                UpdateCopyCounters();
                UpdatePlaceholderVisibility();
                UpdateUI();
            };

            CcEmails.CollectionChanged += (_, _) =>
            {
                UpdateCopyCounters();
                UpdatePlaceholderVisibility();
                UpdateUI();
            };

            Loaded += (_, _) =>
            {
                LoadConfigurationToUI();
                UpdatePlaceholderVisibility();
                UpdateUI();
            };
        }

        public SmtpConfigurationWindow(
            SmtpSettings settings,
            IEmailService emailService,
            ICredentialService credentialService,
            IConfigurationService configurationService,
            ISmtpPersistenceService smtpPersistenceService,
            IGestLogLogger logger)
            : this(emailService, credentialService, configurationService, smtpPersistenceService, logger)
        {
            _currentSettings = settings ?? new SmtpSettings();
            LoadConfigurationToUI();
        }

        #region Event Handlers

        private void OnFieldChanged(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
            if (IsLoaded && _currentSettings != null)
            {
                UpdateUI();
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded && _currentSettings != null)
            {
                UpdateUI();
            }
        }

        private void GmailPreset_Click(object sender, RoutedEventArgs e)
        {
            ApplyPreset("smtp.gmail.com", "587", true, "Configuración de Gmail aplicada");
        }

        private void ZohoPreset_Click(object sender, RoutedEventArgs e)
        {
            ApplyPreset("smtppro.zoho.com", "587", true, "Configuración de Zoho aplicada");
        }

        private void Office365Preset_Click(object sender, RoutedEventArgs e)
        {
            ApplyPreset("smtp.office365.com", "587", true, "Configuración de Office 365 aplicada");
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
            DialogResult = false;
            Close();
        }

        private void BccEmailInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddBccEmail_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void CcEmailInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddCcEmail_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void AddBccEmail_Click(object sender, RoutedEventArgs e)
        {
            var input = FindName("BccEmailInputTextBox") as TextBox;
            HandleAddEmailsFromInput(input, BccEmails, "BCC");
        }

        private void AddCcEmail_Click(object sender, RoutedEventArgs e)
        {
            var input = FindName("CcEmailInputTextBox") as TextBox;
            HandleAddEmailsFromInput(input, CcEmails, "CC");
        }

        private void RemoveBccEmail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string email)
            {
                BccEmails.Remove(email);
            }
        }

        private void RemoveCcEmail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string email)
            {
                CcEmails.Remove(email);
            }
        }

        #endregion

        #region Private Methods

        private void ApplyPreset(string host, string port, bool useSsl, string status)
        {
            var hostTextBox = FindName("HostTextBox") as TextBox;
            var portTextBox = FindName("PortTextBox") as TextBox;
            var sslCheckBox = FindName("SslCheckBox") as CheckBox;

            if (hostTextBox != null) hostTextBox.Text = host;
            if (portTextBox != null) portTextBox.Text = port;
            if (sslCheckBox != null) sslCheckBox.IsChecked = useSsl;

            UpdateStatus(status, Colors.Orange);
            UpdateUI();
        }

        private void LoadConfigurationToUI()
        {
            if (_isLoadingConfiguration)
                return;

            try
            {
                _isLoadingConfiguration = true;

                var latestConfig = _configurationService?.Current?.Modules?.GestionCartera?.Smtp;
                if (latestConfig != null)
                {
                    _currentSettings = latestConfig;
                }

                if (_currentSettings == null)
                {
                    _logger.LogWarning("No se encontró configuración SMTP actual para cargar en UI");
                    return;
                }

                var hostTextBox = FindName("HostTextBox") as TextBox;
                var portTextBox = FindName("PortTextBox") as TextBox;
                var sslCheckBox = FindName("SslCheckBox") as CheckBox;
                var emailTextBox = FindName("EmailTextBox") as TextBox;
                var passwordBox = FindName("PasswordBox") as PasswordBox;
                var saveCredentialsCheckBox = FindName("SaveCredentialsCheckBox") as CheckBox;

                if (hostTextBox != null) hostTextBox.Text = _currentSettings.Server ?? string.Empty;
                if (portTextBox != null) portTextBox.Text = _currentSettings.Port.ToString();
                if (sslCheckBox != null) sslCheckBox.IsChecked = _currentSettings.UseSSL;
                if (emailTextBox != null) emailTextBox.Text = _currentSettings.Username ?? string.Empty;

                BccEmails.Clear();
                foreach (var email in ParseEmailList(_currentSettings.BccEmail))
                {
                    BccEmails.Add(email);
                }

                CcEmails.Clear();
                foreach (var email in ParseEmailList(_currentSettings.CcEmail))
                {
                    CcEmails.Add(email);
                }

                if (_currentSettings.UseAuthentication && !string.IsNullOrEmpty(_currentSettings.Username))
                {
                    var credentialTarget = $"SMTP_{_currentSettings.Server}_{_currentSettings.Username}";
                    var credentials = _credentialService.GetCredentials(credentialTarget);

                    if (!string.IsNullOrEmpty(credentials.username) &&
                        !string.IsNullOrEmpty(credentials.password))
                    {
                        if (saveCredentialsCheckBox != null)
                            saveCredentialsCheckBox.IsChecked = true;

                        if (passwordBox != null)
                            passwordBox.Password = credentials.password;

                        _currentSettings.Password = credentials.password;
                    }
                }

                UpdateCopyCounters();
                UpdatePlaceholderVisibility();
                UpdateStatus(_currentSettings.IsConfigured ? "Configuración cargada" : "No configurado",
                    _currentSettings.IsConfigured ? Colors.Green : Colors.Red);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar configuración a la UI");
                UpdateStatus("Error al cargar configuración", Colors.Red);
            }
            finally
            {
                _isLoadingConfiguration = false;
            }
        }

        private void UpdateUI()
        {
            var testButton = FindName("TestButton") as Button;
            var saveButton = FindName("SaveButton") as Button;

            if (testButton == null || saveButton == null)
                return;

            var isValidForTest = ValidateForTest();
            var isValidForSave = ValidateForSave();

            testButton.IsEnabled = isValidForTest;
            saveButton.IsEnabled = isValidForSave && _isTestSuccessful;
        }

        private bool ValidateForTest()
        {
            var hostTextBox = FindName("HostTextBox") as TextBox;
            var portTextBox = FindName("PortTextBox") as TextBox;
            var emailTextBox = FindName("EmailTextBox") as TextBox;
            var passwordBox = FindName("PasswordBox") as PasswordBox;

            if (hostTextBox == null || portTextBox == null || emailTextBox == null || passwordBox == null)
                return false;

            return !string.IsNullOrWhiteSpace(hostTextBox.Text)
                   && int.TryParse(portTextBox.Text, out var port) && port > 0 && port <= 65535
                   && !string.IsNullOrWhiteSpace(emailTextBox.Text)
                   && !string.IsNullOrWhiteSpace(passwordBox.Password);
        }

        private bool ValidateForSave()
        {
            return ValidateForTest();
        }

        private async Task TestConfigurationAsync()
        {
            try
            {
                var testButton = FindName("TestButton") as Button;
                var hostTextBox = FindName("HostTextBox") as TextBox;
                var portTextBox = FindName("PortTextBox") as TextBox;
                var sslCheckBox = FindName("SslCheckBox") as CheckBox;
                var emailTextBox = FindName("EmailTextBox") as TextBox;
                var passwordBox = FindName("PasswordBox") as PasswordBox;

                UpdateStatus("Probando configuración...", Colors.Orange);
                if (testButton != null)
                    testButton.IsEnabled = false;

                var testConfig = new SmtpConfiguration
                {
                    SmtpServer = hostTextBox?.Text?.Trim() ?? string.Empty,
                    Port = int.TryParse(portTextBox?.Text?.Trim(), out var port) ? port : 587,
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
                }
                else
                {
                    _isTestSuccessful = false;
                    UpdateStatus("❌ Error en la configuración", Colors.Red);
                }
            }
            catch (Exception ex)
            {
                _isTestSuccessful = false;
                UpdateStatus($"❌ Error: {ex.Message}", Colors.Red);
                _logger.LogError(ex, "Error al probar configuración SMTP");
            }
            finally
            {
                var testButton = FindName("TestButton") as Button;
                if (testButton != null)
                    testButton.IsEnabled = true;

                UpdateUI();
            }
        }

        private async Task SaveConfigurationAsync()
        {
            try
            {
                if (!_isTestSuccessful)
                {
                    MessageBox.Show("Debe probar la configuración antes de guardarla.",
                        "Validación requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!TryCommitPendingEmailInputs())
                {
                    MessageBox.Show("Revise los correos de CC/BCC antes de guardar.",
                        "Correos inválidos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var hostTextBox = FindName("HostTextBox") as TextBox;
                var portTextBox = FindName("PortTextBox") as TextBox;
                var sslCheckBox = FindName("SslCheckBox") as CheckBox;
                var emailTextBox = FindName("EmailTextBox") as TextBox;
                var passwordBox = FindName("PasswordBox") as PasswordBox;
                var saveCredentialsCheckBox = FindName("SaveCredentialsCheckBox") as CheckBox;

                var email = emailTextBox?.Text?.Trim() ?? string.Empty;
                var password = passwordBox?.Password ?? string.Empty;
                var shouldSaveCredentials = saveCredentialsCheckBox?.IsChecked ?? false;

                var bccEmail = JoinEmailList(BccEmails);
                var ccEmail = JoinEmailList(CcEmails);

                _logger.LogInformation("💾 [SmtpConfigurationWindow] Iniciando guardado de configuración SMTP");
                _logger.LogInformation("   📌 Servidor: {Server}", hostTextBox?.Text?.Trim() ?? "(vacío)");
                _logger.LogInformation("   📧 Usuario: {Email}", email);
                _logger.LogInformation("   📨 BCC: {BccEmail}", string.IsNullOrWhiteSpace(bccEmail) ? "(vacío)" : bccEmail);
                _logger.LogInformation("   📋 CC: {CcEmail}", string.IsNullOrWhiteSpace(ccEmail) ? "(vacío)" : ccEmail);

                var smtpConfiguration = new SmtpSettings
                {
                    Server = hostTextBox?.Text?.Trim() ?? string.Empty,
                    Port = int.TryParse(portTextBox?.Text?.Trim(), out var port) ? port : 587,
                    Username = email,
                    FromEmail = email,
                    FromName = email,
                    BccEmail = bccEmail,
                    CcEmail = ccEmail,
                    UseSSL = sslCheckBox?.IsChecked ?? true,
                    Timeout = 120000,
                    IsConfigured = true
                };

                var saved = await _smtpPersistenceService.SaveSmtpConfigurationAsync(
                    smtpConfiguration,
                    operationSource: "SmtpConfigurationWindow.SaveConfigurationAsync");

                if (!saved)
                {
                    UpdateStatus("Error al guardar la configuración", Colors.Red);
                    MessageBox.Show("Error al guardar la configuración SMTP",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (shouldSaveCredentials && !string.IsNullOrEmpty(email))
                {
                    var credentialTarget = $"GestionCartera_SMTP_{smtpConfiguration.Server}_{email}";
                    _credentialService.DeleteCredentials(credentialTarget);

                    var savedCreds = _credentialService.SaveCredentials(credentialTarget, email, password);
                    if (!savedCreds)
                    {
                        _logger.LogWarning("Error guardando credenciales SMTP en Credential Manager");
                    }
                }

                UpdateStatus("Configuración guardada exitosamente", Colors.Green);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar configuración SMTP");
                UpdateStatus($"Error al guardar: {ex.Message}", Colors.Red);
                MessageBox.Show($"Error al guardar la configuración: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleAddEmailsFromInput(TextBox? inputTextBox, ObservableCollection<string> targetCollection, string listName)
        {
            if (inputTextBox == null)
                return;

            var rawValue = inputTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawValue))
                return;

            var parsedEmails = ParseEmailList(rawValue).ToList();
            var invalidEmails = new List<string>();
            var addedCount = 0;

            foreach (var email in parsedEmails)
            {
                if (!IsValidEmail(email))
                {
                    invalidEmails.Add(email);
                    continue;
                }

                var alreadyExists = targetCollection.Any(existing =>
                    existing.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (!alreadyExists)
                {
                    targetCollection.Add(email);
                    addedCount++;
                }
            }

            inputTextBox.Clear();
            UpdatePlaceholderVisibility();

            if (invalidEmails.Count > 0)
            {
                UpdateStatus($"⚠️ Correos inválidos en {listName}: {string.Join(", ", invalidEmails)}", Colors.Orange);
                return;
            }

            if (addedCount > 0)
            {
                UpdateStatus($"✅ {addedCount} correo(s) agregado(s) a {listName}", Colors.Green);
            }
        }

        private bool TryCommitPendingEmailInputs()
        {
            var bccInput = FindName("BccEmailInputTextBox") as TextBox;
            var ccInput = FindName("CcEmailInputTextBox") as TextBox;

            if (bccInput != null && !string.IsNullOrWhiteSpace(bccInput.Text))
            {
                if (HasInvalidEmailInRawInput(bccInput.Text))
                    return false;

                HandleAddEmailsFromInput(bccInput, BccEmails, "BCC");
            }

            if (ccInput != null && !string.IsNullOrWhiteSpace(ccInput.Text))
            {
                if (HasInvalidEmailInRawInput(ccInput.Text))
                    return false;

                HandleAddEmailsFromInput(ccInput, CcEmails, "CC");
            }

            return true;
        }

        private static bool HasInvalidEmailInRawInput(string rawInput)
        {
            var emails = ParseEmailList(rawInput);
            return emails.Any(email => !IsValidEmail(email));
        }

        private static IEnumerable<string> ParseEmailList(string? rawEmailList)
        {
            if (string.IsNullOrWhiteSpace(rawEmailList))
                return Enumerable.Empty<string>();

            return rawEmailList
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(email => email.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email));
        }

        private static string JoinEmailList(IEnumerable<string> emails)
        {
            return string.Join(";", emails
                .Select(email => email.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void UpdateCopyCounters()
        {
            var bccCountTextBlock = FindName("BccCountTextBlock") as TextBlock;
            var ccCountTextBlock = FindName("CcCountTextBlock") as TextBlock;

            if (bccCountTextBlock != null)
            {
                bccCountTextBlock.Text = $"{BccEmails.Count} correo(s) BCC";
            }

            if (ccCountTextBlock != null)
            {
                ccCountTextBlock.Text = $"{CcEmails.Count} correo(s) CC";
            }
        }

        private void UpdatePlaceholderVisibility()
        {
            var bccInput = FindName("BccEmailInputTextBox") as TextBox;
            var ccInput = FindName("CcEmailInputTextBox") as TextBox;
            var bccPlaceholder = FindName("BccPlaceholderTextBlock") as TextBlock;
            var ccPlaceholder = FindName("CcPlaceholderTextBlock") as TextBlock;

            if (bccPlaceholder != null)
            {
                bccPlaceholder.Visibility = string.IsNullOrWhiteSpace(bccInput?.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (ccPlaceholder != null)
            {
                ccPlaceholder.Visibility = string.IsNullOrWhiteSpace(ccInput?.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            var statusTextBlock = FindName("StatusTextBlock") as TextBlock;
            var statusIndicator = FindName("StatusIndicator") as System.Windows.Shapes.Ellipse;

            if (statusTextBlock != null)
                statusTextBlock.Text = message;

            if (statusIndicator != null)
                statusIndicator.Fill = new SolidColorBrush(color);
        }

        #endregion
    }
}