using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.EnvioCatalogo.Models;
using GestLog.Modules.EnvioCatalogo.Services;
using GestLog.Modules.GestionCartera.Models; // Para SmtpConfiguration
using GestLog.Services.Core.Logging;
using GestLog.Services.Configuration;
using GestLog.Services.Core.Security;
using GestLog.Modules.Usuarios.Interfaces;

namespace GestLog.Modules.EnvioCatalogo.ViewModels
{
    /// <summary>
    /// ViewModel para el módulo de Envío de Catálogo
    /// </summary>
    public partial class EnvioCatalogoViewModel : ObservableObject
    {
        private readonly IEnvioCatalogoService _catalogoService;
        private readonly IConfigurationService _configurationService;
        private readonly ICredentialService _credentialService;
        private readonly IGestLogLogger _logger;
        private readonly ICurrentUserService _currentUserService;
        private CancellationTokenSource? _cancellationTokenSource;
        private GestLog.Modules.Usuarios.Models.Authentication.CurrentUserInfo _currentUser;

        #region Propiedades Observables

        [ObservableProperty] private string _selectedExcelFilePath = string.Empty;
        [ObservableProperty] private string _catalogoFilePath = string.Empty;
        [ObservableProperty] private bool _isProcessing = false;
        [ObservableProperty] private double _progressValue = 0.0;
        [ObservableProperty] private string _statusMessage = "Listo para enviar catálogo";
        [ObservableProperty] private string _emailSubject = "Importadores y Comercializadores de Aceros y Servicios - Simics Group SAS";
        [ObservableProperty] private bool _useHtmlEmail = true;
        
        // Información del cliente
        [ObservableProperty] private string _clientName = string.Empty;
        [ObservableProperty] private string _clientNIT = string.Empty;
        
        [ObservableProperty] private int _totalEmails = 0;
        [ObservableProperty] private int _processedEmails = 0;
        [ObservableProperty] private int _successfulSends = 0;
        [ObservableProperty] private int _failedSends = 0;

        // Configuración SMTP independiente
        [ObservableProperty] private string _smtpServer = string.Empty;
        [ObservableProperty] private int _smtpPort = 587;
        [ObservableProperty] private string _smtpUsername = string.Empty;
        [ObservableProperty] private string _smtpPassword = string.Empty;
        [ObservableProperty] private bool _enableSsl = true;        [ObservableProperty] private bool _isSmtpConfigured = false;

        #endregion

        #region Propiedades Calculadas

        public bool HasExcelFile => !string.IsNullOrWhiteSpace(SelectedExcelFilePath) && File.Exists(SelectedExcelFilePath);
        public bool HasCatalogFile => !string.IsNullOrWhiteSpace(CatalogoFilePath) && File.Exists(CatalogoFilePath);
        public bool CanSendCatalogo => HasExcelFile && HasCatalogFile && IsSmtpConfigured && !IsProcessing && CanSendCatalogPermission;
        public string ExcelFileName => HasExcelFile ? Path.GetFileName(SelectedExcelFilePath) : "Ningún archivo seleccionado";
        public string CatalogoFileName => HasCatalogFile ? Path.GetFileName(CatalogoFilePath) : "Archivo no encontrado";

        #endregion

        public EnvioCatalogoViewModel(
            IEnvioCatalogoService catalogoService, 
            IConfigurationService configurationService,
            ICredentialService credentialService,
            IGestLogLogger logger,
            ICurrentUserService currentUserService)
        {
            _catalogoService = catalogoService ?? throw new ArgumentNullException(nameof(catalogoService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new GestLog.Modules.Usuarios.Models.Authentication.CurrentUserInfo { Username = string.Empty, FullName = string.Empty };

            // Suscribirse a cambios de configuración
            _configurationService.ConfigurationChanged += OnConfigurationChanged;
            
            // Configurar permisos reactivos
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

            InitializeDefaults();
            LoadSmtpConfiguration();
        }

        private void OnCurrentUserChanged(object? sender, GestLog.Modules.Usuarios.Models.Authentication.CurrentUserInfo? user)
        {
            _currentUser = user ?? new GestLog.Modules.Usuarios.Models.Authentication.CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }        private void RecalcularPermisos()
        {
            var hasPermission = _currentUser.HasPermission("EnvioCatalogo.EnviarCatalogo");
            CanSendCatalogPermission = hasPermission;
            OnPropertyChanged(nameof(CanSendCatalogo)); // Notificar que CanSendCatalogo también cambió
            
            _logger.LogInformation("🔐 Permisos recalculados - Usuario: {User}, EnvioCatalogo.EnviarCatalogo: {HasPermission}", 
                _currentUser.Username ?? "Sin usuario", hasPermission);
        }

        #region Inicialización

        private void InitializeDefaults()
        {
            // Establecer ruta por defecto del catálogo
            CatalogoFilePath = _catalogoService.GetDefaultCatalogPath();

            _logger.LogInformation("📧 EnvioCatalogoViewModel inicializado");
        }        /// <summary>
        /// Carga la configuración SMTP desde el servicio de configuración
        /// </summary>
        private void LoadSmtpConfiguration()
        {
            try
            {
                _logger.LogInformation("🔄 Cargando configuración SMTP para Envío de Catálogo...");
                
                // Usar la configuración SMTP específica del módulo EnvioCatalogo
                var smtpConfig = _configurationService.Current.Modules.EnvioCatalogo.Smtp;
                SmtpServer = smtpConfig.Server ?? string.Empty;
                SmtpPort = smtpConfig.Port;
                SmtpUsername = smtpConfig.Username ?? string.Empty;
                EnableSsl = smtpConfig.UseSSL;
                IsSmtpConfigured = smtpConfig.IsConfigured;                // Cargar contraseña desde Windows Credential Manager con target específico para EnvioCatalogo
                if (!string.IsNullOrWhiteSpace(smtpConfig.Username))
                {
                    var credentialTarget = $"EnvioCatalogo_SMTP_{smtpConfig.Server}_{smtpConfig.Username}";
                    
                    if (_credentialService.CredentialsExist(credentialTarget))
                    {
                        var (username, password) = _credentialService.GetCredentials(credentialTarget);
                        SmtpPassword = password;
                        
                        _logger.LogInformation("🔐 ✅ Credenciales SMTP de EnvioCatalogo cargadas desde Windows Credential Manager");
                        
                        // Configurar también en el servicio
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var serviceConfig = new SmtpConfiguration
                                {
                                    SmtpServer = SmtpServer,
                                    Port = SmtpPort,
                                    Username = SmtpUsername,
                                    Password = SmtpPassword,
                                    EnableSsl = EnableSsl,
                                    Timeout = 30000
                                };
                                await _catalogoService.ConfigureSmtpAsync(serviceConfig);
                                _logger.LogInformation("✅ Servicio SMTP configurado automáticamente al cargar");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error configurando servicio SMTP al cargar");
                            }
                        });
                        
                        IsSmtpConfigured = true;
                    }
                    else
                    {
                        SmtpPassword = string.Empty;
                        IsSmtpConfigured = false;
                        _logger.LogInformation("⚠️ No se encontraron credenciales guardadas para SMTP de EnvioCatalogo");
                    }
                }
                
                _logger.LogInformation("🔄 ✅ SMTP de EnvioCatalogo configurado: Server='{Server}', Username='{Username}', IsConfigured={IsConfigured}", 
                    SmtpServer, SmtpUsername, IsSmtpConfigured);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar configuración SMTP de EnvioCatalogo");
                IsSmtpConfigured = false;
                SmtpPassword = string.Empty;
            }
        }        /// <summary>
        /// Guarda la configuración SMTP actual
        /// </summary>
        private async Task SaveSmtpConfigurationAsync()
        {
            try
            {
                // Usar la configuración SMTP específica del módulo EnvioCatalogo
                var smtpConfig = _configurationService.Current.Modules.EnvioCatalogo.Smtp;
                
                // Actualizar configuración (sin contraseña)
                smtpConfig.Server = SmtpServer;
                smtpConfig.Port = SmtpPort;
                smtpConfig.Username = SmtpUsername;
                smtpConfig.FromEmail = SmtpUsername;
                smtpConfig.FromName = SmtpUsername;
                smtpConfig.UseSSL = EnableSsl;
                smtpConfig.UseAuthentication = !string.IsNullOrWhiteSpace(SmtpUsername);
                smtpConfig.IsConfigured = IsSmtpConfigured;

                // Guardar contraseña de forma segura con target específico para EnvioCatalogo
                if (!string.IsNullOrWhiteSpace(SmtpUsername) && !string.IsNullOrWhiteSpace(SmtpPassword))
                {
                    var credentialTarget = $"EnvioCatalogo_SMTP_{SmtpServer}_{SmtpUsername}";
                    _credentialService.SaveCredentials(credentialTarget, SmtpUsername, SmtpPassword);
                    
                    _logger.LogInformation("🔐 ✅ Credenciales SMTP de EnvioCatalogo guardadas en Windows Credential Manager");
                }

                await _configurationService.SaveAsync();
                _logger.LogInformation("✅ Configuración SMTP de EnvioCatalogo guardada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar configuración SMTP de EnvioCatalogo");
                throw;
            }
        }

        /// <summary>
        /// Maneja cambios en la configuración
        /// </summary>
        private void OnConfigurationChanged(object? sender, EventArgs e)
        {
            LoadSmtpConfiguration();
        }

        #endregion

        #region Comandos

        [RelayCommand]
        private void SelectExcelFile()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Seleccionar archivo Excel con correos",
                    Filter = "Archivos Excel (*.xlsx)|*.xlsx|Todos los archivos (*.*)|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    SelectedExcelFilePath = openFileDialog.FileName;
                    _logger.LogInformation("📊 Archivo Excel seleccionado: {FilePath}", SelectedExcelFilePath);
                    
                    // Actualizar propiedades calculadas
                    OnPropertyChanged(nameof(HasExcelFile));
                    OnPropertyChanged(nameof(ExcelFileName));
                    OnPropertyChanged(nameof(CanSendCatalogo));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error seleccionando archivo Excel");
                StatusMessage = "Error al seleccionar archivo Excel";
            }
        }

        [RelayCommand]
        private void SelectCatalogoFile()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Seleccionar archivo de catálogo PDF",
                    Filter = "Archivos PDF (*.pdf)|*.pdf|Todos los archivos (*.*)|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    CatalogoFilePath = openFileDialog.FileName;
                    _logger.LogInformation("📄 Archivo de catálogo seleccionado: {FilePath}", CatalogoFilePath);
                    
                    // Actualizar propiedades calculadas
                    OnPropertyChanged(nameof(HasCatalogFile));
                    OnPropertyChanged(nameof(CatalogoFileName));
                    OnPropertyChanged(nameof(CanSendCatalogo));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error seleccionando archivo de catálogo");
                StatusMessage = "Error al seleccionar archivo de catálogo";
            }
        }

        [RelayCommand]
        private async Task SendCatalogoAsync()
        {
            if (!CanSendCatalogo)
            {
                StatusMessage = "❌ No se puede enviar: verifique archivos y configuración";
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                IsProcessing = true;
                _logger.LogInformation("🚀 Iniciando envío de catálogo");

                StatusMessage = "Leyendo información de clientes desde Excel...";
                var clients = await _catalogoService.ReadClientsFromExcelAsync(SelectedExcelFilePath);
                
                if (!clients.Any())
                {
                    StatusMessage = "❌ No se encontraron clientes en el archivo Excel";
                    return;
                }

                TotalEmails = clients.Count();
                ProcessedEmails = 0;
                SuccessfulSends = 0;
                FailedSends = 0;

                StatusMessage = $"Enviando catálogo a {TotalEmails} clientes...";

                foreach (var client in clients)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    try
                    {                        var emailInfo = new CatalogoEmailInfo
                        {
                            Recipients = new List<string> { client.Email },
                            Subject = EmailSubject,
                            CatalogFilePath = CatalogoFilePath,
                            IsBodyHtml = UseHtmlEmail,
                            CompanyName = client.Nombre,
                            ClientNIT = client.NIT
                        };

                        await _catalogoService.SendCatalogoEmailAsync(emailInfo, _cancellationTokenSource.Token);
                        SuccessfulSends++;
                        
                        ProcessedEmails++;
                        var globalProgress = (double)ProcessedEmails / TotalEmails * 100;
                        ProgressValue = globalProgress;
                        
                        StatusMessage = $"Enviado a {client.Nombre} ({ProcessedEmails}/{TotalEmails})";
                        
                        _logger.LogInformation("✅ Catálogo enviado a: {Email} - {Company}", client.Email, client.Nombre);
                    }
                    catch (Exception ex)
                    {
                        FailedSends++;
                        ProcessedEmails++;
                        _logger.LogError(ex, "❌ Error enviando catálogo a {Email}", client.Email);
                        
                        // Continuar con el siguiente
                    }
                }

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    StatusMessage = $"⏹️ Envío cancelado - Enviados: {SuccessfulSends}/{TotalEmails}";
                }
                else
                {
                    StatusMessage = $"✅ Envío completado - Exitosos: {SuccessfulSends}, Fallidos: {FailedSends}";
                }

                _logger.LogInformation("✅ Envío de catálogos completado exitosamente");
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "⏹️ Envío cancelado por el usuario";
                _logger.LogWarning("⏹️ Envío de catálogo cancelado");
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error durante envío: {ex.Message}";
                _logger.LogError(ex, "❌ Error inesperado durante envío de catálogo");
            }
            finally
            {
                IsProcessing = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private void CancelSending()
        {
            try
            {
                StatusMessage = "Cancelando envío...";
                _cancellationTokenSource?.Cancel();
                _logger.LogInformation("🛑 Solicitud de cancelación de envío");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cancelando envío");
            }
        }

        [RelayCommand]
        private async Task ConfigureSmtpAsync()
        {
            try
            {
                StatusMessage = "Configurando SMTP...";
                
                var smtpConfig = new SmtpConfiguration
                {
                    SmtpServer = SmtpServer,
                    Port = SmtpPort,
                    Username = SmtpUsername,
                    Password = SmtpPassword,
                    EnableSsl = EnableSsl,
                    Timeout = 30000
                };

                await _catalogoService.ConfigureSmtpAsync(smtpConfig);
                await SaveSmtpConfigurationAsync();
                
                StatusMessage = "✅ SMTP configurado correctamente";
                IsSmtpConfigured = true;
                _logger.LogInformation("✅ SMTP configurado y guardado exitosamente");
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error configurando SMTP: {ex.Message}";
                IsSmtpConfigured = false;
                _logger.LogError(ex, "❌ Error configurando SMTP");
            }
            finally
            {
                OnPropertyChanged(nameof(CanSendCatalogo));
            }
        }

        [RelayCommand]
        private async Task TestSmtpAsync()
        {
            if (!IsSmtpConfigured)
            {
                StatusMessage = "❌ Configure SMTP primero";
                return;
            }

            try
            {
                StatusMessage = "Enviando email de prueba...";
                var testEmail = "Prueba de configuración SMTP - Envío de Catálogo";
                
                var success = await _catalogoService.SendTestEmailAsync(testEmail);
                
                if (success)
                {
                    StatusMessage = "✅ Email de prueba enviado exitosamente";
                    _logger.LogInformation("✅ Email de prueba enviado exitosamente");
                }
                else
                {
                    StatusMessage = "❌ Falló envío de email de prueba";
                    _logger.LogWarning("❌ Falló envío de email de prueba");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error enviando email de prueba: {ex.Message}";
                _logger.LogError(ex, "❌ Error enviando email de prueba");
            }
        }

        [RelayCommand]
        private void LoadTestData()
        {
            try
            {
                // Datos de prueba como se especificó
                ClientName = "CLIENTE 123";
                ClientNIT = "12345698-3";
                
                // Crear archivo Excel de prueba si no existe
                var testExcelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "emails_prueba.xlsx");
                if (!File.Exists(testExcelPath))
                {
                    SelectedExcelFilePath = testExcelPath;
                    OnPropertyChanged(nameof(HasExcelFile));
                    OnPropertyChanged(nameof(ExcelFileName));
                    OnPropertyChanged(nameof(CanSendCatalogo));
                }

                StatusMessage = "Datos de prueba cargados";
                _logger.LogInformation("📋 Datos de prueba cargados: {ClientName} - {ClientNIT}", ClientName, ClientNIT);
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ Error cargando datos de prueba";
                _logger.LogError(ex, "❌ Error cargando datos de prueba");
            }
        }

        [RelayCommand]
        private void ResetProgress()
        {
            try
            {
                // Reiniciar todas las estadísticas de progreso
                ProgressValue = 0.0;
                TotalEmails = 0;
                ProcessedEmails = 0;
                SuccessfulSends = 0;
                FailedSends = 0;
                IsProcessing = false;
                
                StatusMessage = "Progreso reiniciado";
                _logger.LogInformation("🔄 Progreso y estadísticas reiniciadas");
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ Error reiniciando progreso";
                _logger.LogError(ex, "❌ Error reiniciando progreso");
            }
        }        [RelayCommand]
        private async Task SaveSmtpConfiguration()
        {
            try
            {
                StatusMessage = "Guardando configuración SMTP...";
                
                // Configurar en el servicio primero
                var smtpConfig = new SmtpConfiguration
                {
                    SmtpServer = SmtpServer,
                    Port = SmtpPort,
                    Username = SmtpUsername,
                    Password = SmtpPassword,
                    EnableSsl = EnableSsl,
                    Timeout = 30000
                };

                await _catalogoService.ConfigureSmtpAsync(smtpConfig);
                
                // Luego guardar persistentemente
                await SaveSmtpConfigurationAsync();
                
                StatusMessage = "✅ Configuración SMTP guardada correctamente";
                IsSmtpConfigured = true;
                _logger.LogInformation("✅ Configuración SMTP guardada exitosamente");
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error guardando configuración SMTP: {ex.Message}";
                _logger.LogError(ex, "❌ Error guardando configuración SMTP");
            }
            finally
            {
                OnPropertyChanged(nameof(CanSendCatalogo));
            }
        }

        #endregion

        #region Métodos Privados        partial void OnSelectedExcelFilePathChanged(string value) => UpdateCanSendCatalogo();
        partial void OnCatalogoFilePathChanged(string value) => UpdateCanSendCatalogo();
        partial void OnIsSmtpConfiguredChanged(bool value) => UpdateCanSendCatalogo();
        partial void OnIsProcessingChanged(bool value) => UpdateCanSendCatalogo();
        partial void OnCanSendCatalogPermissionChanged(bool value) => UpdateCanSendCatalogo();

        private void UpdateCanSendCatalogo()
        {
            OnPropertyChanged(nameof(CanSendCatalogo));
        }

                #endregion

        #region Propiedades de Permisos

        [ObservableProperty]
        private bool canSendCatalogPermission;

        #endregion
    }
}
