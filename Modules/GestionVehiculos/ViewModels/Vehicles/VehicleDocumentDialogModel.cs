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
        public ICommand CancelUploadCommand { get; }        private void SelectFile()
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
            _logger.LogDebug("SaveAsync iniciado");

            // Validar VehicleId
            if (VehicleId == Guid.Empty)
            {
                ErrorMessage = "⚠️ VehicleId inválido.";
                _logger.LogWarning("SaveAsync: VehicleId vacío");
                return;
            }

            // Validar DocumentType
            if (string.IsNullOrWhiteSpace(DocumentType))
            {
                ErrorMessage = "⚠️ Seleccione el tipo de documento.";
                _logger.LogWarning("SaveAsync: DocumentType vacío");
                return;
            }

            // Validar SelectedFilePath
            if (SelectedFilePath == null || !File.Exists(SelectedFilePath))
            {
                ErrorMessage = "⚠️ Seleccione un archivo válido.";
                _logger.LogWarning($"SaveAsync: SelectedFilePath no válido: {SelectedFilePath}");
                return;
            }

            // Validar extensión
            var allowed = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
            var ext = Path.GetExtension(SelectedFilePath).ToLowerInvariant();
            if (Array.IndexOf(allowed, ext) < 0)
            {
                ErrorMessage = $"⚠️ Extensión no permitida: {ext}";
                _logger.LogWarning($"SaveAsync: Extensión no permitida: {ext}");
                return;
            }

            // Validar tamaño
            var fi = new FileInfo(SelectedFilePath);
            const long maxBytes = 10 * 1024 * 1024;
            if (fi.Length > maxBytes)
            {
                ErrorMessage = $"⚠️ Archivo muy grande: {(fi.Length / 1024 / 1024)}MB. Máximo: 10MB";
                _logger.LogWarning($"SaveAsync: Archivo muy grande: {fi.Length} bytes");
                return;
            }

            try
            {
                IsUploading = true;
                UploadProgress = 0;
                _uploadCts = new System.Threading.CancellationTokenSource();
                var token = _uploadCts.Token;

                var progress = new Progress<double>(p => UploadProgress = p);

                // Verificar si existe documento vigente del mismo tipo (para SOAT/Tecno)
                var isSoatOrTecno = DocumentType.Equals("SOAT", StringComparison.OrdinalIgnoreCase) || 
                                   DocumentType.Equals("Tecno-Mecánica", StringComparison.OrdinalIgnoreCase);
                
                var existingActive = (await _documentService.GetByVehicleIdAsync(VehicleId))
                    .FirstOrDefault(d => d.DocumentType.Equals(DocumentType, StringComparison.OrdinalIgnoreCase) && d.IsActive);

                if (existingActive != null && isSoatOrTecno)
                {
                    var confirmMsg = $"Ya existe un {DocumentType} vigente. ¿Desea reemplazarlo?";
                    var ok = System.Windows.MessageBox.Show(Owner, confirmMsg, "Reemplazar documento", 
                        System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;
                    
                    if (!ok)
                    {
                        IsUploading = false;
                        return;
                    }
                }

                // Generar Id localmente (sin tocar BD aún)
                var documentId = Guid.NewGuid();
                var fileExt = Path.GetExtension(SelectedFileName) ?? string.Empty;
                var storageFileName = SanitizeFilePart($"{DocumentType}_{VehicleId}_{documentId}{fileExt}");

                // Subir archivo
                using var fileStream = File.OpenRead(SelectedFilePath);
                var storagePath = await _photoStorage.SaveOriginalAsync(fileStream, storageFileName, progress, token);

                // Crear DTO con información completa
                var dto = new VehicleDocumentDto
                {
                    Id = documentId,
                    VehicleId = VehicleId,
                    DocumentType = DocumentType,
                    DocumentNumber = DocumentNumber,
                    IssuedDate = IssuedDate.HasValue ? new DateTimeOffset(IssuedDate.Value) : DateTimeOffset.UtcNow,
                    ExpirationDate = ExpirationDate.HasValue ? new DateTimeOffset(ExpirationDate.Value) : DateTimeOffset.UtcNow.AddYears(1),
                    FileName = storageFileName,
                    FilePath = storagePath,
                    Notes = Notes
                };                // Para SOAT/Tecno: usar AddWithReplaceAsync (operación atómica)
                if (isSoatOrTecno)
                {
                    var replaceResult = await _documentService.AddWithReplaceAsync(dto);
                    var newDocId = replaceResult.NewDocumentId;

                    // Intentar mover archivo antiguo a carpeta 'archivo'
                    if (replaceResult.ArchivedDocumentId.HasValue && !string.IsNullOrWhiteSpace(replaceResult.ArchivedFilePath))
                    {
                        try
                        {
                            var archivedFolder = Path.Combine(Path.GetDirectoryName(replaceResult.ArchivedFilePath) ?? string.Empty, "archivo");
                            var archivedFileName = Path.GetFileName(replaceResult.ArchivedFilePath);
                            var archivedPath = Path.Combine(archivedFolder, archivedFileName);
                            
                            var moved = await _photoStorage.MoveAsync(replaceResult.ArchivedFilePath, archivedPath);
                            
                            if (moved)
                            {
                                // Crear DTO para actualizar la ruta del archivo archivado
                                var archivedDoc = await _documentService.GetByIdAsync(replaceResult.ArchivedDocumentId.Value);
                                if (archivedDoc != null)
                                {
                                    archivedDoc.FilePath = archivedPath;
                                    archivedDoc.UpdatedAt = DateTimeOffset.UtcNow;
                                    await _documentService.UpdateAsync(archivedDoc);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("No se pudo mover archivo antiguo a carpeta archivo para documento {DocumentId}", replaceResult.ArchivedDocumentId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error moviendo archivo antiguo para documento {DocumentId}", replaceResult.ArchivedDocumentId);
                        }
                    }
                }
                else
                {
                    // Para otros tipos: crear nuevo registro
                    await _documentService.AddAsync(dto);
                }

                // Cerrar diálogo
                CloseDialog();

                // Enviar mensajes
                try { WeakReferenceMessenger.Default.Send(new VehicleDocumentUploadProgressMessage(VehicleId, 100, false)); } catch { }
                try { WeakReferenceMessenger.Default.Send(new VehicleDocumentCreatedMessage(VehicleId, documentId)); } catch { }
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada por el usuario.";
                _logger.LogWarning("SaveAsync: Upload cancelado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveAsync: Error al guardar documento");
                ErrorMessage = $"❌ Error: {ex.Message}";
            }
            finally
            {
                IsUploading = false;
                UploadProgress = 0;
                _uploadCts?.Dispose();
                _uploadCts = null;
            }
        }

        private void CloseDialog()
        {
            if (Owner != null)
            {
                Owner.DialogResult = true;
                Owner.Close();
            }
            else
            {
                var wnd = System.Windows.Application.Current.Windows[System.Windows.Application.Current.Windows.Count - 1] as System.Windows.Window;
                if (wnd != null)
                {
                    wnd.DialogResult = true;
                    wnd.Close();
                }
            }
        }

        private static string SanitizeFilePart(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            
            var sanitized = input.Trim().Replace(' ', '_');
            var invalid = Path.GetInvalidFileNameChars();
            
            foreach (var c in invalid)
                sanitized = sanitized.Replace(c.ToString(), string.Empty);
            
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
    }    public class RelayCommand : ICommand
    {
        private readonly Action _action;
        public RelayCommand(Action action) => _action = action ?? throw new ArgumentNullException(nameof(action));
        
#pragma warning disable CS0067 // El evento nunca se usa
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _action();
    }
}
