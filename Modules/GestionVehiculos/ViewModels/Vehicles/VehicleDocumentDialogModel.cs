using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using GestLog.Modules.GestionVehiculos.Interfaces;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Services.Core.Logging;
using GestLog.Utilities;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionVehiculos.Messages.Documents;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Vehicles
{
    /// <summary>
    /// ViewModel para el diálogo de creación/edición de documentos de vehículo
    /// </summary>
    public class VehicleDocumentDialogModel : ViewModelBase
    {
        private readonly IVehicleDocumentService _documentService;
        private readonly IPhotoStorageService _photoStorage;
        private readonly IGestLogLogger _logger;
        private readonly IVehicleService _vehicleService;

        private Guid _vehicleId;
        private string _documentType = string.Empty;
        private string? _documentNumber;
        private DateTime? _issuedDate = DateTime.Now;
        private DateTime? _expirationDate = DateTime.Now.AddYears(1);
        private string? _notes;
        private string? _selectedFilePath;
        private string? _selectedFileName;
        private string _errorMessage = string.Empty;
        private double _uploadProgress;
        private bool _isUploading;
        private System.Threading.CancellationTokenSource? _uploadCts;
        // Ventana dueña, asignada desde el code-behind para operaciones que necesitan un owner (dialogs)
        public System.Windows.Window? Owner { get; set; }

        public VehicleDocumentDialogModel(IVehicleDocumentService documentService, IPhotoStorageService photoStorage, IGestLogLogger logger, IVehicleService vehicleService)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _photoStorage = photoStorage ?? throw new ArgumentNullException(nameof(photoStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));

            DocumentTypes = new ObservableCollection<string>(new[] { "SOAT", "Tecno-Mecánica", "Carta de propiedad", "Otros" });

            SelectFileCommand = new RelayCommand(SelectFile);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            CancelUploadCommand = new RelayCommand(() => _uploadCts?.Cancel());
        }

        public ObservableCollection<string> DocumentTypes { get; }

        public Guid VehicleId
        {
            get => _vehicleId;
            set => SetProperty(ref _vehicleId, value);
        }

        public string DocumentType
        {
            get => _documentType;
            set => SetProperty(ref _documentType, value);
        }

        public string? DocumentNumber
        {
            get => _documentNumber;
            set => SetProperty(ref _documentNumber, value);
        }

        public DateTime? IssuedDate
        {
            get => _issuedDate;
            set => SetProperty(ref _issuedDate, value);
        }

        public DateTime? ExpirationDate
        {
            get => _expirationDate;
            set => SetProperty(ref _expirationDate, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string? SelectedFilePath
        {
            get => _selectedFilePath;
            set => SetProperty(ref _selectedFilePath, value);
        }

        public string? SelectedFileName
        {
            get => _selectedFileName;
            set => SetProperty(ref _selectedFileName, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public double UploadProgress
        {
            get => _uploadProgress;
            private set
            {
                SetProperty(ref _uploadProgress, value);
                try { WeakReferenceMessenger.Default.Send(new VehicleDocumentUploadProgressMessage(VehicleId, value, true)); } catch { }
            }
        }

        public bool IsUploading
        {
            get => _isUploading;
            private set
            {
                SetProperty(ref _isUploading, value);
                try { WeakReferenceMessenger.Default.Send(new VehicleDocumentUploadProgressMessage(VehicleId, UploadProgress, value)); } catch { }
            }
        }

        public ICommand SelectFileCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }
        public ICommand CancelUploadCommand { get; }

        private void SelectFile()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "PDF Files|*.pdf|Images|*.png;*.jpg;*.jpeg|All files|*.*";
            bool? result;
            try
            {
                result = Owner != null ? ofd.ShowDialog(Owner) : ofd.ShowDialog();
            }
            catch
            {
                result = ofd.ShowDialog();
            }

            if (result == true)
            {
                SelectedFilePath = ofd.FileName;
                SelectedFileName = Path.GetFileName(SelectedFilePath);
            }
        }

        private async Task SaveAsync()
        {
            ErrorMessage = string.Empty;

            // Log de inicio
            _logger.LogDebug("SaveAsync iniciado");

            if (VehicleId == Guid.Empty)
            {
                ErrorMessage = "⚠️ VehicleId inválido.";
                _logger.LogWarning("SaveAsync: VehicleId vacío");
                return;
            }

            _logger.LogDebug($"SaveAsync: VehicleId = {VehicleId}");

            if (string.IsNullOrWhiteSpace(DocumentType))
            {
                ErrorMessage = "⚠️ Seleccione el tipo de documento.";
                _logger.LogWarning("SaveAsync: DocumentType vacío");
                return;
            }

            _logger.LogDebug($"SaveAsync: DocumentType = {DocumentType}");

            if (SelectedFilePath == null || !File.Exists(SelectedFilePath))
            {
                ErrorMessage = "⚠️ Seleccione un archivo válido.";
                _logger.LogWarning($"SaveAsync: SelectedFilePath no válido: {SelectedFilePath}");
                return;
            }

            _logger.LogDebug($"SaveAsync: SelectedFilePath = {SelectedFilePath}");

            // Validaciones básicas
            var allowed = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
            var ext = Path.GetExtension(SelectedFilePath).ToLowerInvariant();
            if (Array.IndexOf(allowed, ext) < 0)
            {
                ErrorMessage = $"⚠️ Extensión no permitida: {ext}. Permitidas: .pdf, .png, .jpg, .jpeg";
                _logger.LogWarning($"SaveAsync: Extensión no permitida: {ext}");
                return;
            }

            var fi = new FileInfo(SelectedFilePath);
            const long maxBytes = 10 * 1024 * 1024; // 10 MB
            if (fi.Length > maxBytes)
            {
                ErrorMessage = $"⚠️ Archivo muy grande: {(fi.Length / 1024 / 1024)}MB. Máximo: 10MB";
                _logger.LogWarning($"SaveAsync: Archivo muy grande: {fi.Length} bytes");
                return;
            }

            _logger.LogDebug("SaveAsync: Archivo válido. Iniciando subida...");

            try
            {
                // Subir archivo al storage con reporte de progreso
                IsUploading = true;
                UploadProgress = 0;
                _uploadCts = new System.Threading.CancellationTokenSource();
                var token = _uploadCts.Token;

                var progress = new Progress<double>(p => {
                    UploadProgress = p;
                    try { WeakReferenceMessenger.Default.Send(new VehicleDocumentUploadProgressMessage(VehicleId, p, true)); } catch { }
                });

                // Primero crear el registro en la BD para obtener el documentId y usarlo en el nombre del archivo
                var initialDto = new VehicleDocumentDto
                {
                    VehicleId = VehicleId,
                    DocumentType = DocumentType,
                    DocumentNumber = DocumentNumber,
                    IssuedDate = IssuedDate.HasValue ? new DateTimeOffset(IssuedDate.Value) : DateTimeOffset.UtcNow,
                    ExpirationDate = ExpirationDate.HasValue ? new DateTimeOffset(ExpirationDate.Value) : DateTimeOffset.UtcNow.AddYears(1),
                    FileName = string.Empty,
                    FilePath = string.Empty,
                    Notes = Notes
                };

                var createdId = await _documentService.AddAsync(initialDto);
                _logger.LogDebug($"SaveAsync: Registro inicial creado con ID: {createdId}");

                // Construir el nombre de archivo usando el documentId en lugar del timestamp
                string plate = string.Empty;
                try
                {
                    var vehicle = await _vehicleService.GetByIdAsync(VehicleId);
                    plate = vehicle?.Plate ?? string.Empty;
                }
                catch { }

                var parts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(DocumentType)) parts.Add(SanitizeFilePart(DocumentType));
                if (!string.IsNullOrWhiteSpace(plate)) parts.Add(SanitizeFilePart(plate));
                if (!string.IsNullOrWhiteSpace(DocumentNumber)) parts.Add(SanitizeFilePart(DocumentNumber));
                parts.Add(createdId.ToString());
                var storageFileName = string.Join("_", parts) + ext;

                using var fs = File.OpenRead(SelectedFilePath);
                var storagePath = await _photoStorage.SaveOriginalAsync(fs, storageFileName, progress, token);

                _logger.LogDebug($"SaveAsync: Archivo subido a {storagePath}");

                // Notify completion to subscribers
                try { WeakReferenceMessenger.Default.Send(new VehicleDocumentUploadProgressMessage(VehicleId, 100, false)); } catch { }

                // Actualizar el registro con la información del archivo
                var updateDto = new VehicleDocumentDto
                {
                    Id = createdId,
                    VehicleId = VehicleId,
                    DocumentType = DocumentType,
                    DocumentNumber = DocumentNumber,
                    IssuedDate = initialDto.IssuedDate,
                    ExpirationDate = initialDto.ExpirationDate,
                    FileName = storageFileName,
                    FilePath = storagePath,
                    Notes = Notes
                };

                var updated = await _documentService.UpdateAsync(updateDto);
                if (!updated)
                {
                    _logger.LogWarning($"SaveAsync: No se pudo actualizar el registro del documento {createdId} con la info del archivo");
                }

                // Enviar mensaje para notificar creación y permitir refrescar listados
                try { WeakReferenceMessenger.Default.Send(new VehicleDocumentCreatedMessage(VehicleId, createdId)); } catch { }

                // cerrar diálogo con resultado OK usando Owner si está disponible
                if (Owner != null)
                {
                    _logger.LogDebug("SaveAsync: Cerrando diálogo con DialogResult=true");
                    Owner.DialogResult = true;
                    Owner.Close();
                }
                else
                {
                    _logger.LogWarning("SaveAsync: Owner es null, intentando cerrar última ventana");
                    var wnd = System.Windows.Application.Current.Windows[System.Windows.Application.Current.Windows.Count - 1] as System.Windows.Window;
                    if (wnd != null)
                    {
                        wnd.DialogResult = true;
                        wnd.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    ErrorMessage = "Operación cancelada por el usuario.";
                    _logger.LogWarning("SaveAsync: Upload cancelado por usuario");
                    try { WeakReferenceMessenger.Default.Send(new VehicleDocumentUploadProgressMessage(VehicleId, UploadProgress, false)); } catch { }
                }
                else
                {
                    _logger.LogError(ex, "SaveAsync: Error al guardar documento");
                    ErrorMessage = $"❌ Error al guardar: {ex.Message}";
                }
            }
            finally
            {
                IsUploading = false;
                UploadProgress = 0;
                _uploadCts?.Dispose();
                _uploadCts = null;
                try { WeakReferenceMessenger.Default.Send(new VehicleDocumentUploadProgressMessage(VehicleId, 0, false)); } catch { }
            }
        }

        private static string SanitizeFilePart(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // Reemplazar espacios por '_' y eliminar caracteres inválidos para nombre de archivo
            var sanitized = input.Trim().Replace(' ', '_');
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                sanitized = sanitized.Replace(c.ToString(), string.Empty);
            }
            // También quitar tildes básicos y caracteres especiales comunes
            sanitized = sanitized.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in sanitized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString();
        }
    }

    // RelayCommand simple
    public class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action _action;
        public RelayCommand(Action action) => _action = action ?? throw new ArgumentNullException(nameof(action));
#pragma warning disable CS0067 // El evento CanExecuteChanged puede no usarse en esta implementación simplificada
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _action();
    }
}
