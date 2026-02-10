using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.Interfaces;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Services.Core.Logging;
using System.Linq;
using GestLog.Utilities;
using System.Threading;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionVehiculos.Messages.Documents;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Vehicles
{
    /// <summary>
    /// ViewModel para gestión de documentos de vehículos
    /// </summary>
    public class VehicleDocumentsViewModel : ViewModelBase
    {
        private readonly IVehicleDocumentService _documentService;
        private readonly IGestLogLogger _logger;
        private ObservableCollection<VehicleDocumentDto> _documents;
        private VehicleDocumentDto? _selectedDocument;
        private bool _isLoading;
        private string _filterText;
        private DocumentStatisticsDto _statistics;
        private Guid _vehicleId;
        private double _loadingProgress;
        private CancellationTokenSource? _loadingCts;

        public VehicleDocumentsViewModel(IVehicleDocumentService documentService, IGestLogLogger logger)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documents = new ObservableCollection<VehicleDocumentDto>();
            _statistics = new DocumentStatisticsDto();
            _filterText = string.Empty;
            _loadingProgress = 0.0;

            // Comandos
            LoadDocumentsCommand = new AsyncRelayCommand(LoadDocumentsAsync);
            DeleteDocumentCommand = new AsyncRelayCommand<VehicleDocumentDto>(DeleteDocumentAsync);
            RefreshCommand = new AsyncRelayCommand(LoadDocumentsAsync);

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
                foreach (var doc in documents.OrderByDescending(d => d.ExpirationDate))
                {
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

        /// <summary>
        /// Elimina un documento
        /// </summary>
        private async Task DeleteDocumentAsync(VehicleDocumentDto? document)
        {
            if (document == null) return;

            try
            {
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
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T?, Task> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting;

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
    }

    /// <summary>
    /// Comando asincrónico sin parámetros
    /// </summary>
    public class AsyncRelayCommand : IAsyncRelayCommand
    {
        private readonly Func<Task> _execute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting;

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
        protected bool SetProperty<T>(ref T field, T value, string? propertyName = null)
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
