using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Interfaces.Storage;
using GestLog.Modules.GestionVehiculos.Interfaces.Dialog;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Services.Core.Logging;
using System.Linq;
using GestLog.Utilities;
using System.Threading;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionVehiculos.Messages.Documents;
using System.Runtime.CompilerServices;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Vehicles
{
    /// <summary>
    /// ViewModel para gestión de documentos de vehículos
    /// </summary>
    public class VehicleDocumentsViewModel : ViewModelBase
    {
        private readonly IVehicleDocumentService _documentService;
        private readonly IGestLogLogger _logger;
        private readonly IPhotoStorageService? _photoStorage;
        private readonly GestLog.Modules.GestionVehiculos.Interfaces.Dialog.IAppDialogService? _dialogService;
        private ObservableCollection<VehicleDocumentDto> _documents;
        private VehicleDocumentDto? _selectedDocument;
        private bool _isLoading;
        private string _filterText;
        private DocumentStatisticsDto _statistics;
        private Guid _vehicleId;
        private double _loadingProgress;
        private CancellationTokenSource? _loadingCts;

        public VehicleDocumentsViewModel(IVehicleDocumentService documentService, IGestLogLogger logger, IPhotoStorageService? photoStorage = null, GestLog.Modules.GestionVehiculos.Interfaces.Dialog.IAppDialogService? dialogService = null)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _photoStorage = photoStorage;
            _dialogService = dialogService;
            _documents = new ObservableCollection<VehicleDocumentDto>();
            _statistics = new DocumentStatisticsDto();
            _filterText = string.Empty;
            _loadingProgress = 0.0;

            // Comandos
            LoadDocumentsCommand = new AsyncRelayCommand(LoadDocumentsAsync);
            DeleteDocumentCommand = new AsyncRelayCommand<VehicleDocumentDto>(DeleteDocumentAsync);
            RefreshCommand = new AsyncRelayCommand(LoadDocumentsAsync);

            PreviewCommand = new AsyncRelayCommand<VehicleDocumentDto>(PreviewDocumentAsync);
            DownloadCommand = new AsyncRelayCommand<VehicleDocumentDto>(DownloadDocumentAsync);

            // Registrar listener de progreso
            RegisterUploadProgressListener();
            
            // Registrar listener para creación de documentos y forzar recarga
            try
            {
                WeakReferenceMessenger.Default.Register<VehicleDocumentCreatedMessage>(this, async (r, m) =>
                {
                    if (m == null) return;
                    if (m.VehicleId != _vehicleId) return;
                    await LoadDocumentsAsync();
                });
            }
            catch { }
        }

        #region Propiedades

        /// <summary>
        /// Colección de documentos del vehículo
        /// </summary>
        public ObservableCollection<VehicleDocumentDto> Documents
        {
            get => _documents;
            set => SetProperty(ref _documents, value);
        }

        /// <summary>
        /// Documento seleccionado
        /// </summary>
        public VehicleDocumentDto? SelectedDocument
        {
            get => _selectedDocument;
            set => SetProperty(ref _selectedDocument, value);
        }

        /// <summary>
        /// Indicador de carga
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    if (value) StartLoadingAnimation();
                    else StopLoadingAnimation();
                }
            }
        }

        /// <summary>
        /// Progreso de carga (0-100) mostrado en la barra
        /// </summary>
        public double LoadingProgress
        {
            get => _loadingProgress;
            private set => SetProperty(ref _loadingProgress, value);
        }

        /// <summary>
        /// Filtro de búsqueda
        /// </summary>
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Estadísticas de documentos
        /// </summary>
        public DocumentStatisticsDto Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        #endregion

        #region Comandos

        public IAsyncRelayCommand LoadDocumentsCommand { get; }
        public IAsyncRelayCommand<VehicleDocumentDto> DeleteDocumentCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand<VehicleDocumentDto> PreviewCommand { get; private set; }
        public IAsyncRelayCommand<VehicleDocumentDto> DownloadCommand { get; private set; }

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Inicializa el ViewModel con el ID del vehículo
        /// </summary>
        public async Task InitializeAsync(Guid vehicleId)
        {
            _vehicleId = vehicleId;
            _logger.LogDebug($"🔍 VehicleDocumentsViewModel.InitializeAsync: vehicleId={vehicleId}");
            await LoadDocumentsAsync();
        }

        /// <summary>
        /// Registra un nuevo documento en la colección
        /// </summary>
        public void AddDocument(VehicleDocumentDto document)
        {
            if (document == null) return;

            Documents.Add(document);
            _ = RefreshStatistics();
        }

        #endregion

        #region Métodos Privados

        /// <summary>
        /// Carga los documentos del vehículo desde el servicio
        /// </summary>
        private async Task LoadDocumentsAsync()
        {
            if (_vehicleId == Guid.Empty)
            {
                _logger.LogWarning("🚫 LoadDocumentsAsync: VehicleId vacío, abortando");
                return;
            }

            try
            {
                _logger.LogDebug($"⏳ LoadDocumentsAsync: Iniciando para VehicleId={_vehicleId}");
                IsLoading = true;
                var documents = await _documentService.GetByVehicleIdAsync(_vehicleId);
                
                _logger.LogDebug($"✅ LoadDocumentsAsync: {documents.Count} documentos obtenidos de BD");
                
                Documents.Clear();
                foreach (var doc in documents
                    .OrderBy(d => GetDocumentPriority(d.DocumentType))
                    .ThenByDescending(d => d.IssuedDate)
                    .ThenByDescending(d => d.CreatedAt))
                {
                    // Si el storage service está disponible y el documento es imagen, solicitar URI para thumbnail/previsualización
                    try
                    {
                        if (_photoStorage != null && doc.IsImage && !string.IsNullOrWhiteSpace(doc.FilePath))
                        {
                            try
                            {
                                doc.PreviewUri = await _photoStorage.GetUriAsync(doc.FilePath);
                            }
                            catch (Exception exUri)
                            {
                                _logger.LogWarning(exUri, "No se pudo obtener PreviewUri para documento {DocumentId}", doc.Id);
                            }
                        }
                    }
                    catch { }

                    Documents.Add(doc);
                    _logger.LogDebug($"   📄 Documento añadido: {doc.DocumentType} - {doc.FileName}");
                }

                await RefreshStatistics();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ LoadDocumentsAsync: Error al cargar documentos para VehicleId={_vehicleId}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static int GetDocumentPriority(string? documentType)
        {
            if (string.IsNullOrWhiteSpace(documentType)) return 99;

            if (documentType.Equals("SOAT", StringComparison.OrdinalIgnoreCase)) return 0;
            if (documentType.Equals("Tecno-Mecánica", StringComparison.OrdinalIgnoreCase)) return 1;
            if (documentType.Equals("Factura", StringComparison.OrdinalIgnoreCase)) return 2;

            return 10;
        }

        /// <summary>
        /// Elimina un documento
        /// </summary>
        private async Task DeleteDocumentAsync(VehicleDocumentDto? document)
        {
            if (document == null) return;

            try
            {
                // Confirmaciones via DialogService
                if (_dialogService == null)
                {
                    _logger.LogWarning("DialogService no disponible: operación de eliminación cancelada");
                    return;
                }

                if (!_dialogService.Confirm($"¿Desea eliminar el documento '{document.DocumentType} - {document.DocumentNumber}'?", "Confirmar eliminación"))
                    return;

                if (!_dialogService.ConfirmWarning("Esta acción también borrará el archivo físico del storage. ¿Desea continuar?", "Eliminar archivo físico"))
                    return;

                if (_photoStorage == null)
                {
                    _dialogService?.ShowError("Servicio de almacenamiento no disponible. No se puede eliminar el archivo físico.");
                    _logger.LogWarning("IPhotoStorageService no disponible al intentar eliminar documento");
                    return;
                }

                // Si no hay FilePath, preguntar si se desea marcar como eliminado en BD
                if (string.IsNullOrWhiteSpace(document.FilePath))
                {
                    var proceed = _dialogService != null ? _dialogService.Confirm("No se encontró la ruta del archivo en el registro. ¿Desea marcar el documento como eliminado en la base de datos de todas formas?", "Eliminar sin archivo") : false;
                    if (!proceed) return;

                    var dbResult = await _documentService.DeleteAsync(document.Id);
                    if (dbResult)
                    {
                        Documents.Remove(document);
                        await RefreshStatistics();
                    }

                    return;
                }

                // Intentar borrar el archivo físico con reintentos y backoff exponencial
                bool fileDeleted = false;
                int attempts = 0;
                const int maxAttempts = 3;
                var delay = TimeSpan.FromSeconds(1);

                while (attempts < maxAttempts && !fileDeleted)
                {
                    attempts++;
                    try
                    {
                        _logger.LogDebug($"Intentando eliminar archivo físico (intento {attempts}) para documento {document.Id} -> {document.FilePath}");
                        var deleted = await _photoStorage.DeleteAsync(document.FilePath!);
                        if (deleted)
                        {
                            fileDeleted = true;
                            break;
                        }

                        _logger.LogWarning("IPhotoStorageService.DeleteAsync devolvió false para ruta {FilePath}", document.FilePath);
                        // si devolvió false consideramos fallo y reintentos
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Excepción al intentar eliminar archivo físico para documento {DocumentId}", document.Id);
                    }

                    if (!fileDeleted && attempts < maxAttempts)
                    {
                        try { await Task.Delay(delay); } catch { }
                        delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // backoff
                    }
                }

                if (!fileDeleted)
                {
                    // Preguntar si se desea forzar borrado lógico
                    var force = _dialogService != null ? _dialogService.ConfirmWarning("No se pudo eliminar el archivo físico tras varios intentos. ¿Desea marcar el documento como eliminado en la base de datos de todas formas? (Esto puede dejar el archivo en el storage)", "No se pudo eliminar archivo") : false;

                    if (!force)
                    {
                        return;
                    }

                    // Intentar borrado lógico de BD
                    var dbForceResult = await _documentService.DeleteAsync(document.Id);
                    if (dbForceResult)
                    {
                        Documents.Remove(document);
                        await RefreshStatistics();
                    }

                    return;
                }

                // Si llegamos aquí, el archivo físico fue eliminado correctamente: proceder a borrar en BD
                var result = await _documentService.DeleteAsync(document.Id);
                if (result)
                {
                    Documents.Remove(document);
                    await RefreshStatistics();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar documento");
                _dialogService?.ShowError($"Error al eliminar documento: {ex.Message}");
            }
        }

        /// <summary>
        /// Descarga un documento
        /// </summary>
        private async Task DownloadDocumentAsync(VehicleDocumentDto? document)
        {
            if (document == null) return;

            try
            {
                if (_photoStorage == null)
                {
                    _logger.LogWarning("DownloadDocumentAsync: IPhotoStorageService no está disponible");
                    return;
                }

                var uri = await _photoStorage.GetUriAsync(document.FilePath ?? string.Empty);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar/abrir documento");
            }
        }

        /// <summary>
        /// Previsualiza un documento
        /// </summary>
        private async Task PreviewDocumentAsync(VehicleDocumentDto? document)
        {
            if (document == null) return;

            try
            {
                if (_photoStorage == null)
                {
                    _logger.LogWarning("PreviewDocumentAsync: IPhotoStorageService no está disponible");
                    return;
                }

                // Obtener URI y abrir siempre con el handler del SO
                var uri = await _photoStorage.GetUriAsync(document.FilePath ?? string.Empty);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al previsualizar/abrir documento");
            }
        }

        /// <summary>
        /// Actualiza las estadísticas de documentos
        /// </summary>
        private async Task RefreshStatistics()
        {
            if (_vehicleId == Guid.Empty) return;

            try
            {
                Statistics = await _documentService.GetStatisticsAsync(_vehicleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de documentos");
            }
        }

        /// <summary>
        /// Aplica el filtro de búsqueda
        /// </summary>
        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_filterText))
            {
                // Recargar sin bloqueo (fire-and-forget intencional, registrado si hay excepción)
                LoadDocumentsAsync().FireAndForgetSafeAsync(_logger, "VehicleDocumentsViewModel.ApplyFilter");
                return;
            }

            // Implementar filtro local
            var filtered = Documents
                .Where(d => d.DocumentType.Contains(_filterText, StringComparison.OrdinalIgnoreCase)
                    || (d.DocumentNumber?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (d.Notes?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            Documents.Clear();
            foreach (var doc in filtered)
            {
                Documents.Add(doc);
            }
        }

        private void StartLoadingAnimation()
        {
            try
            {
                _loadingCts?.Cancel();
            }
            catch { }

            _loadingCts = new CancellationTokenSource();
            var token = _loadingCts.Token;

            LoadingProgress = 0;

            // Animación simple en background que incrementa progresivamente hasta 95% repetidamente
            Task.Run(async () =>
            {
                double progress = 0;
                while (!token.IsCancellationRequested)
                {
                    progress += 7;
                    if (progress > 95) progress = 10; // ciclo

                    // Actualizar en UI thread
                    try
                    {
                        System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() => LoadingProgress = progress));
                    }
                    catch { }

                    try { await Task.Delay(150, token); } catch { break; }
                }
            }, token);
        }

        private void StopLoadingAnimation()
        {
            try
            {
                _loadingCts?.Cancel();
            }
            catch { }

            // Completar al 100% rápidamente y luego ocultar
            try
            {
                System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() => LoadingProgress = 100));
            }
            catch { }

            Task.Run(async () =>
            {
                try { await Task.Delay(300); } catch { }
                try { System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() => LoadingProgress = 0)); } catch { }
            });
        }

        /// <summary>
        /// Suscribirse a mensajes de progreso de upload
        /// </summary>
        private void RegisterUploadProgressListener()
        {
            try
            {
                WeakReferenceMessenger.Default.Register<VehicleDocumentUploadProgressMessage>(this, (r, m) =>
                {
                    if (m == null) return;
                    if (m.VehicleId != _vehicleId) return; // sólo interesan los del vehículo actual

                    // Si isUploading == true, mostrar y asignar LoadingProgress; si false, asegurar que se oculte después
                    if (m.IsUploading)
                    {
                        IsLoading = true;
                        LoadingProgress = m.Progress;
                    }
                    else
                    {
                        // completar al 100% visualmente y luego ocultar
                        LoadingProgress = m.Progress >= 100 ? 100 : m.Progress;
                        IsLoading = false;
                    }
                });
            }
            catch { }
        }

        #endregion
    }

    /// <summary>
    /// Comando asincrónico genérico
    /// </summary>
    public class AsyncRelayCommand<T> : IAsyncRelayCommand<T>
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T?, Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                await _execute((T?)parameter);
            }
            finally
            {
                _isExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Comando asincrónico sin parámetros
    /// </summary>
    public class AsyncRelayCommand : IAsyncRelayCommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Interfaz para comandos asincronicos
    /// </summary>
    public interface IAsyncRelayCommand : ICommand
    {
    }

    /// <summary>
    /// Interfaz para comandos asincronicos genéricos
    /// </summary>
    public interface IAsyncRelayCommand<T> : ICommand
    {
    }

    /// <summary>
    /// Clase base para ViewModels
    /// </summary>
    public abstract class ViewModelBase
    {
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
