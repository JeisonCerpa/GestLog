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
        private readonly IConfigurationService _configurationService;
        private readonly ISmtpPersistenceService _smtpPersistenceService;
        private readonly IGestLogLogger _logger;

        private SmtpSettings _currentSettings;
        private bool _isTestSuccessful;
        private bool _isLoadingConfiguration;
        private bool _connectionDirty;
        private bool _copyDirty;

        public ObservableCollection<string> BccEmails { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> CcEmails { get; } = new ObservableCollection<string>();

        public SmtpSettings Settings => _currentSettings;

        public SmtpConfigurationWindow(
            IEmailService emailService,
            IConfigurationService configurationService,
            ISmtpPersistenceService smtpPersistenceService,
            IGestLogLogger logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
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
            IConfigurationService configurationService,
            ISmtpPersistenceService smtpPersistenceService,
            IGestLogLogger logger)
            : this(emailService, configurationService, smtpPersistenceService, logger)
        {
            _currentSettings = settings ?? new SmtpSettings();
            LoadConfigurationToUI();
        }

        #region Event Handlers

        private void OnFieldChanged(object sender, RoutedEventArgs e)
        {
            _connectionDirty = true;
            _isTestSuccessful = false;
            UpdatePlaceholderVisibility();
            if (IsLoaded && _currentSettings != null)
            {
                UpdateUI();
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            _connectionDirty = true;
            _isTestSuccessful = false;
            if (IsLoaded && _currentSettings != null)
            {
                UpdateUI();
            }
        }

        private void OnCopyFieldChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded && _currentSettings != null)
            {
                UpdatePlaceholderVisibility();
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
                _copyDirty = true;
                UpdateUI();
            }
        }

        private void RemoveCcEmail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string email)
            {
                CcEmails.Remove(email);
                _copyDirty = true;
                UpdateUI();
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

            UpdateStatus(status, ResolveStatusBrush("WarningBrush"));
            UpdateUI();
        }

        private void LoadConfigurationToUI()
        {
            if (_isLoadingConfiguration)
                return;

            try
            {
                _isLoadingConfiguration = true;

                var latestConfig = _smtpPersistenceService.LoadSmtpConfigurationAsync().GetAwaiter().GetResult();
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

                if (_currentSettings.UseAuthentication && !string.IsNullOrEmpty(_currentSettings.Password))
                {
                    if (saveCredentialsCheckBox != null)
                        saveCredentialsCheckBox.IsChecked = true;

                    if (passwordBox != null)
                        passwordBox.Password = _currentSettings.Password;
                }

                UpdateCopyCounters();
                UpdatePlaceholderVisibility();
                _connectionDirty = false;
                _copyDirty = false;
                _isTestSuccessful = _currentSettings.IsConfigured;
                UpdateStatus(_currentSettings.IsConfigured ? "Configuración cargada" : "No configurado",
                    _currentSettings.IsConfigured ? ResolveStatusBrush("SuccessBrush") : ResolveStatusBrush("DangerBrush"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar configuración a la UI");
                UpdateStatus("Error al cargar configuración", ResolveStatusBrush("DangerBrush"));
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
            saveButton.IsEnabled = isValidForSave;
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
            if (_connectionDirty)
            {
                return ValidateForTest() && _isTestSuccessful;
            }

            if (_copyDirty)
            {
                return true;
            }

            return _isTestSuccessful || _currentSettings?.IsConfigured == true;
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

                UpdateStatus("Probando configuración...", ResolveStatusBrush("WarningBrush"));
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

                // Prueba real: abre conexión y autentica contra el servidor SIN enviar correo.
                var result = await _emailService.TestConnectionAsync(testConfig);

                if (result.IsSuccess)
                {
                    _isTestSuccessful = true;
                    UpdateStatus("✅ " + result.Message, ResolveStatusBrush("SuccessBrush"));
                }
                else
                {
                    _isTestSuccessful = false;
                    UpdateStatus("❌ " + result.Message, ResolveStatusBrush("DangerBrush"));
                }
            }
            catch (Exception ex)
            {
                _isTestSuccessful = false;
                UpdateStatus($"❌ Error: {ex.Message}", ResolveStatusBrush("DangerBrush"));
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
                if (_connectionDirty && !_isTestSuccessful)
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
                    // La contraseña solo se persiste si el usuario pidió recordarla; el almacén la guarda.
                    Password = shouldSaveCredentials ? password : string.Empty,
                    Timeout = 120000,
                    IsConfigured = true
                };

                var saved = await _smtpPersistenceService.SaveSmtpConfigurationAsync(
                    smtpConfiguration,
                    operationSource: "SmtpConfigurationWindow.SaveConfigurationAsync");

                if (!saved)
                {
                    UpdateStatus("Error al guardar la configuración", ResolveStatusBrush("DangerBrush"));
                    MessageBox.Show("Error al guardar la configuración SMTP",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                UpdateStatus("Configuración guardada exitosamente", ResolveStatusBrush("SuccessBrush"));
                _connectionDirty = false;
                _copyDirty = false;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar configuración SMTP");
                UpdateStatus($"Error al guardar: {ex.Message}", ResolveStatusBrush("DangerBrush"));
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

            if (addedCount > 0)
            {
                _copyDirty = true;
                UpdateUI();
            }

            inputTextBox.Clear();
            UpdatePlaceholderVisibility();

            if (invalidEmails.Count > 0)
            {
                UpdateStatus($"⚠️ Correos inválidos en {listName}: {string.Join(", ", invalidEmails)}", ResolveStatusBrush("WarningBrush"));
                return;
            }

            if (addedCount > 0)
            {
                UpdateStatus($"✅ {addedCount} correo(s) agregado(s) a {listName}", ResolveStatusBrush("SuccessBrush"));
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

        private System.Windows.Media.Brush ResolveStatusBrush(string resourceKey)
        {
            if (TryFindResource(resourceKey) is System.Windows.Media.Brush brush)
            {
                return brush;
            }

            return System.Windows.Media.Brushes.Gray;
        }

        private void UpdateStatus(string message, System.Windows.Media.Brush brush)
        {
            var statusTextBlock = FindName("StatusTextBlock") as TextBlock;
            var statusIndicator = FindName("StatusIndicator") as System.Windows.Shapes.Ellipse;

            if (statusTextBlock != null)
                statusTextBlock.Text = message;

            if (statusIndicator != null)
                statusIndicator.Fill = brush;
        }

        #endregion
    }
}