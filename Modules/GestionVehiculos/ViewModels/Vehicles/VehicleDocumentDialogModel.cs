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
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Vehicles
{
    public class VehicleDocumentDialogModel : ViewModelBase, INotifyDataErrorInfo
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
        private string? _selectedFileName = null;
        private string? _selectedFileSize = null;
        private string? _selectedFileMimeType = null;
        private ImageSource? _selectedFilePreview;
        private bool _isImagePreview;
        private string _errorMessage = string.Empty;
        private double _uploadProgress;
        private bool _isUploading;
        private System.Threading.CancellationTokenSource? _uploadCts;
        private readonly Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();
        private System.Threading.CancellationTokenSource? _validationCts;

        // Comando para remover archivo seleccionado
        private RelayCommand? _removeSelectedFileCommand;
        public ICommand? RemoveSelectedFileCommand => _removeSelectedFileCommand;

        public System.Windows.Window? Owner { get; set; }

        // Evento para notificar éxito y permitir que la vista cierre el diálogo
        public event EventHandler? OnExito;        public VehicleDocumentDialogModel(IVehicleDocumentService documentService, IPhotoStorageService photoStorage, IGestLogLogger logger, IVehicleService vehicleService)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _photoStorage = photoStorage ?? throw new ArgumentNullException(nameof(photoStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));

            DocumentTypes = new ObservableCollection<string>(new[] { "SOAT", "Tecno-Mecánica", "Carta de propiedad", "Otros" });            SelectFileCommand = new RelayCommand(SelectFile);
            // Inicializar comando Remove y exponerlo
            _removeSelectedFileCommand = new RelayCommand(RemoveSelectedFile, () => HasSelectedFile);
            _removeSelectedFileCommand.SetDebugLogger(logger);
            
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);
            CancelUploadCommand = new RelayCommand(() => _uploadCts?.Cancel());
            var openCmd = new RelayCommand(OpenSelectedFile, () => HasSelectedFile);
            openCmd.SetDebugLogger(logger);
            OpenSelectedFileCommand = openCmd;
            
            _logger.LogInformation("[DEBUG-CTOR] VehicleDocumentDialogModel inicializado. RemoveSelectedFileCommand creado con canExecute predicate. HasSelectedFile={HasSelectedFile}", HasSelectedFile);
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
            set
            {
                if (SetProperty(ref _documentType, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                    NotifyCanSaveChanged();
                }
            }
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
        }        public string? SelectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                if (SetProperty(ref _selectedFilePath, value))
                {                    try
                    {
                        _logger.LogInformation("[DEBUG-SETTER] SelectedFilePath ANTES: SetProperty={SetProp} valor={Value}", true, value ?? "(null)");
                        OnPropertyChanged(nameof(HasSelectedFile));
                        _logger.LogInformation("[DEBUG-SETTER] SelectedFilePath PropertyChanged(HasSelectedFile) invocado");
                        _removeSelectedFileCommand?.RaiseCanExecuteChanged();
                        if (OpenSelectedFileCommand is RelayCommand openCmd)
                            openCmd.RaiseCanExecuteChanged();
                        _logger.LogInformation("[DEBUG-SETTER] SelectedFilePath RaiseCanExecuteChanged invocado. HasSelectedFile={HasSelectedFile}, CanExecute={CanExecute}", HasSelectedFile, _removeSelectedFileCommand?.CanExecute(null) ?? false);
                        NotifyCanSaveChanged();
                        _logger.LogInformation("[DEBUG-SETTER] SelectedFilePath NotifyCanSaveChanged invocado. CanSave={CanSave}", CanSave);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[DEBUG-SETTER] Error notifying SelectedFilePath change");
                    }
                }
            }
        }

        public string? SelectedFileName
        {
            get => _selectedFileName;
            set
            {                if (SetProperty(ref _selectedFileName, value))
                {
                    try
                    {
                        OnPropertyChanged(nameof(HasSelectedFile));
                        _removeSelectedFileCommand?.RaiseCanExecuteChanged();
                        if (OpenSelectedFileCommand is RelayCommand openCmd)
                            openCmd.RaiseCanExecuteChanged();
                        NotifyCanSaveChanged();
                        _logger.LogInformation("[DEBUG] SelectedFileName changed to {Value}, HasSelectedFile={HasSelectedFile}", value ?? "(null)", HasSelectedFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[DEBUG] Error notifying SelectedFileName change");
                    }
                }
            }
        }

        public string? SelectedFileSize
        {
            get => _selectedFileSize;
            set
            {
                if (SetProperty(ref _selectedFileSize, value))
                {
                    // tamaño no afecta a HasSelectedFile, pero mantener coherencia
                    OnPropertyChanged(nameof(SelectedFileSize));
                }
            }
        }

        public string? SelectedFileMimeType
        {
            get => _selectedFileMimeType;
            set
            {
                if (SetProperty(ref _selectedFileMimeType, value))
                {
                    // MIME no afecta directamente a HasSelectedFile
                    OnPropertyChanged(nameof(SelectedFileMimeType));
                    // Notificar propiedad auxiliar para mostrar icono PDF
                    OnPropertyChanged(nameof(IsPdfSelected));
                    _removeSelectedFileCommand?.RaiseCanExecuteChanged();
                    NotifyCanSaveChanged();
                }
            }
        }          public ImageSource? SelectedFilePreview
        {
            get => _selectedFilePreview;
            set
            {                if (SetProperty(ref _selectedFilePreview, value))
                {
                    try
                    {
                        // Actualizar IsImagePreview cuando cambia SelectedFilePreview
                        IsImagePreview = value != null;
                        OnPropertyChanged(nameof(HasSelectedFile));
                        _removeSelectedFileCommand?.RaiseCanExecuteChanged();
                        if (OpenSelectedFileCommand is RelayCommand openCmd)
                            openCmd.RaiseCanExecuteChanged();
                        NotifyCanSaveChanged();
                        _logger.LogInformation("[DEBUG] SelectedFilePreview changed, HasSelectedFile={HasSelectedFile}", HasSelectedFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[DEBUG] Error notifying SelectedFilePreview change");
                    }
                }
            }
        }

        public bool IsImagePreview
        {
            get => _isImagePreview;
            private set => SetProperty(ref _isImagePreview, value);
        }

        public bool IsPdfSelected => string.Equals(SelectedFileMimeType, "application/pdf", StringComparison.OrdinalIgnoreCase);

        // Propiedad calculada para determinar si hay archivo seleccionado
        public bool HasSelectedFile => !string.IsNullOrWhiteSpace(SelectedFilePath) || !string.IsNullOrWhiteSpace(SelectedFileName) || SelectedFilePreview != null;

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
            {                if (SetProperty(ref _isUploading, value))
                {
                    try { WeakReferenceMessenger.Default.Send(new VehicleDocumentUploadProgressMessage(VehicleId, UploadProgress, value)); } catch { }
                    NotifyCanSaveChanged();
                }
            }
        }

        public ICommand SelectFileCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }
        public ICommand CancelUploadCommand { get; }
        public ICommand OpenSelectedFileCommand { get; }

        // Nueva propiedad para exponer error específico del archivo seleccionado (inline)
        public string? SelectedFileError => GetFirstErrorForProperty(nameof(SelectedFilePath));

        // Propiedad calculada para habilitar guardar
        public bool CanSave => !HasErrors && !IsUploading && !string.IsNullOrWhiteSpace(DocumentType) && HasSelectedFile;

        // Texto explicativo cuando Guardar está deshabilitado
        public string DisabledReason
        {
            get
            {
                if (IsUploading) return "Subiendo archivo...";
                if (string.IsNullOrWhiteSpace(DocumentType)) return "Seleccione el tipo de documento.";
                if (!HasSelectedFile) return "Seleccione un archivo válido.";
                if (HasErrors) return SelectedFileError ?? "Hay errores en el archivo seleccionado.";
                return string.Empty;
            }
        }

        // Implementación INotifyDataErrorInfo
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public System.Collections.IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return Enumerable.Empty<string>();
            if (_errors.TryGetValue(propertyName, out var list)) return list;
            return Enumerable.Empty<string>();
        }

        public bool HasErrors => _errors.Any(kv => kv.Value != null && kv.Value.Count > 0);

        private void AddError(string propertyName, string error)
        {
            if (!_errors.TryGetValue(propertyName, out var list))
            {
                list = new List<string>();
                _errors[propertyName] = list;
            }

            if (!list.Contains(error))
            {
                list.Add(error);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(SelectedFileError));
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(DisabledReason));
                NotifyCanSaveChanged();
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(SelectedFileError));
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(DisabledReason));
                NotifyCanSaveChanged();
            }
        }

        private string? GetFirstErrorForProperty(string propertyName)
        {
            if (_errors.TryGetValue(propertyName, out var list) && list.Any())
                return list.First();
            return null;
        }

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
                try
                {
                    SelectedFilePath = ofd.FileName;
                    SelectedFileName = Path.GetFileName(SelectedFilePath);

                    var fi = new FileInfo(SelectedFilePath);
                    SelectedFileSize = FormatFileSize(fi.Length);

                    var ext = Path.GetExtension(SelectedFilePath).ToLowerInvariant();
                    SelectedFileMimeType = ext switch
                    {
                        ".png" => "image/png",
                        ".jpg" => "image/jpeg",
                        ".jpeg" => "image/jpeg",
                        ".pdf" => "application/pdf",
                        _ => "application/octet-stream"
                    };

                    // Preview for images: cargar desde FileStream con OnLoad para evitar locks y problemas de acceso
                    if (SelectedFileMimeType != null && SelectedFileMimeType.StartsWith("image/"))
                    {
                        try
                        {
                            using (var fs = File.OpenRead(SelectedFilePath))
                            {
                                var img = new BitmapImage();
                                img.BeginInit();
                                img.CacheOption = BitmapCacheOption.OnLoad;
                                img.StreamSource = fs;
                                img.EndInit();
                                img.Freeze();
                                SelectedFilePreview = img;
                            }
                        }
                        catch (Exception ex)
                        {
                            SelectedFilePreview = null;
                            try { _logger.LogWarning(ex, "SelectFile: fallo preview para {File}", SelectedFileName ?? "(null)"); } catch { }
                        }

                        // Fallback using UriSource
                        if (SelectedFilePreview == null)
                        {
                            try
                            {
                                var img2 = new BitmapImage();
                                img2.BeginInit();
                                img2.CacheOption = BitmapCacheOption.OnLoad;
                                img2.UriSource = new Uri(SelectedFilePath);
                                img2.EndInit();
                                img2.Freeze();
                                SelectedFilePreview = img2;
                            }
                            catch (Exception ex)
                            {
                                SelectedFilePreview = null;
                                try { _logger.LogWarning(ex, "SelectFile: fallback UriSource fallo para {File}", SelectedFileName ?? "(null)"); } catch { }
                            }
                        }
                    }
                    else
                    {
                        SelectedFilePreview = null;
                    }

                    // Log selection metadata at Information level with [DEBUG] tag
                    try
                    {
                        _logger.LogInformation("[DEBUG] SelectFile: seleccionado {FilePath} Name={Name} Size={Size} Mime={Mime}",
                            SelectedFilePath ?? "(null)",
                            SelectedFileName ?? "(null)",
                            SelectedFileSize ?? "(null)",
                            SelectedFileMimeType ?? "(null)");
                    }
                    catch { }

                    // Start async validation (cancel previous)
                    _validationCts?.Cancel();
                    _validationCts = new System.Threading.CancellationTokenSource();
                    _ = ValidateSelectedFileAsync(_validationCts.Token).ContinueWith(t => {
                        if (t.IsFaulted) try { _logger.LogWarning(t.Exception, "ValidateSelectedFileAsync error"); } catch { }
                    });
                    NotifyCanSaveChanged();
                }
                catch (Exception ex)
                {
                    SelectedFileSize = null; SelectedFileMimeType = null; SelectedFilePreview = null;
                    try { _logger.LogWarning(ex, "SelectFile: error leyendo archivo {FilePath}", ofd.FileName ?? "(null)"); } catch { }
                }
            }
        }

        private void RemoveSelectedFile()
        {
            try
            {
                // Limpiar todos los metadatos del archivo seleccionado
                SelectedFilePath = null;
                SelectedFileName = null;
                SelectedFileSize = null;
                SelectedFileMimeType = null;
                SelectedFilePreview = null;

                // Limpiar mensajes de error relacionados
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                try { _logger.LogWarning(ex, "RemoveSelectedFile: error al remover archivo"); } catch { }
            }
            finally            {
                // Notificar explícitamente propiedades por si algún binding quedó en caché
                OnPropertyChanged(nameof(SelectedFilePath));
                OnPropertyChanged(nameof(SelectedFileName));
                OnPropertyChanged(nameof(SelectedFileSize));
                OnPropertyChanged(nameof(SelectedFileMimeType));
                OnPropertyChanged(nameof(SelectedFilePreview));
                OnPropertyChanged(nameof(HasSelectedFile));
                _removeSelectedFileCommand?.RaiseCanExecuteChanged();
                if (OpenSelectedFileCommand is RelayCommand openCmd)
                    openCmd.RaiseCanExecuteChanged();
                NotifyCanSaveChanged();
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
            return (bytes / (1024.0 * 1024.0)).ToString("0.0") + " MB";
        }

        private void OpenSelectedFile()
        {
            if (string.IsNullOrWhiteSpace(SelectedFilePath) || !File.Exists(SelectedFilePath)) return;
            try
            {
                var psi = new ProcessStartInfo(SelectedFilePath) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenSelectedFile: no se pudo abrir archivo {File}", SelectedFilePath ?? "(null)");
            }
        }

        private async Task ValidateSelectedFileAsync(System.Threading.CancellationToken cancellationToken)
        {
            const long maxBytes = 10 * 1024 * 1024; // 10MB
            var prop = nameof(SelectedFilePath);
            ClearErrors(prop);

            try
            {
                if (string.IsNullOrWhiteSpace(SelectedFilePath))
                {
                    AddError(prop, "Seleccione un archivo válido.");
                    return;
                }

                if (!File.Exists(SelectedFilePath))
                {
                    AddError(prop, "El archivo seleccionado no existe.");
                    return;
                }

                var fi = new FileInfo(SelectedFilePath);
                if (fi.Length > maxBytes)
                {
                    AddError(prop, $"Archivo muy grande: {(fi.Length / 1024 / 1024)}MB. Máximo: 10MB.");
                }

                var ext = Path.GetExtension(SelectedFilePath).ToLowerInvariant();
                var allowed = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
                if (Array.IndexOf(allowed, ext) < 0)
                {
                    AddError(prop, $"Extensión no permitida: {ext}. Permitidos: .pdf, .png, .jpg, .jpeg.");
                }

                if (cancellationToken.IsCancellationRequested) return;

                // Content validation
                if (ext == ".pdf")
                {
                    try
                    {
                        using var fs = File.OpenRead(SelectedFilePath);
                        var header = new byte[4];
                        var read = await fs.ReadAsync(header, 0, header.Length, cancellationToken);
                        var headerStr = System.Text.Encoding.ASCII.GetString(header, 0, read);
                        if (!headerStr.StartsWith("%PDF"))
                        {
                            AddError(prop, "El archivo no es un PDF válido.");
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        AddError(prop, "No se pudo validar el PDF (archivo posiblemente corrupto)." );
                        try { _logger.LogWarning(ex, "ValidateSelectedFileAsync: PDF validation failed for {File}", SelectedFilePath); } catch { }
                    }
                }
                else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                {
                    // Try to decode image asynchronously to ensure it's not corrupted
                    try
                    {
                        await Task.Run(() =>
                        {
                            using var fs = File.OpenRead(SelectedFilePath);
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = fs;
                            bmp.EndInit();
                            bmp.Freeze();
                        }, cancellationToken);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        AddError(prop, "La imagen está dañada o no se puede leer.");
                        try { _logger.LogWarning(ex, "ValidateSelectedFileAsync: image decode failed for {File}", SelectedFilePath); } catch { }
                    }
                }
            }
            finally
            {
                // Notificar cambios en propiedades dependientes
                OnPropertyChanged(nameof(SelectedFileError));
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(DisabledReason));
                NotifyCanSaveChanged();
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
                _logger.LogWarning($"SaveAsync: SelectedFilePath no válido: {SelectedFilePath ?? "(null)"}");
                return;
            }

            // Antes de continuar, asegurarse de que no haya errores de validación
            if (HasErrors)
            {
                ErrorMessage = SelectedFileError ?? "Hay errores con el archivo seleccionado.";
                _logger.LogWarning("SaveAsync: validación previa falló: {Error}", ErrorMessage);
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
                };

                // Variable para almacenar el Id real del documento creado
                Guid createdDocumentId = Guid.Empty;

                // Para SOAT/Tecno: usar AddWithReplaceAsync (operación atómica)
                if (isSoatOrTecno)
                {
                    // Llamar a la nueva firma que acepta uploadedStoragePath y CancellationToken
                    var replaceResult = await _documentService.AddWithReplaceAsync(dto, storagePath, token);

                    if (replaceResult == null || replaceResult.NewDocumentId == Guid.Empty)
                    {
                        // Si el servicio devolvió un resultado inválido, informar al usuario y no cerrar el diálogo
                        ErrorMessage = "❌ No se pudo reemplazar el documento. Operación revertida.";
                        _logger.LogWarning("SaveAsync: AddWithReplaceAsync falló para VehicleId={VehicleId}", VehicleId);
                        return;
                    }

                    createdDocumentId = replaceResult.NewDocumentId;
                }
                else
                {
                    // Para otros tipos: crear nuevo registro y capturar el Id devuelto
                    createdDocumentId = await _documentService.AddAsync(dto);
                }

                // Cerrar diálogo
                CloseDialog();

                // Enviar mensajes
                try { WeakReferenceMessenger.Default.Send(new VehicleDocumentUploadProgressMessage(VehicleId, 100, false)); } catch { }
                try { WeakReferenceMessenger.Default.Send(new VehicleDocumentCreatedMessage(VehicleId, createdDocumentId)); } catch { }
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
            // Disparar el evento para que la vista cierre el diálogo de forma consistente
            try
            {
                OnExito?.Invoke(this, EventArgs.Empty);
            }
            catch { }
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
        }        private void NotifyCanSaveChanged()
        {
            try
            {
                // Notificar cambio de propiedad CanSave al binding
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(DisabledReason));
                
                // Notificar al comando
                if (SaveCommand is AsyncRelayCommand arc)
                    arc.RaiseCanExecuteChanged();
                else if (SaveCommand is IAsyncRelayCommand cmd)
                {
                    // try reflectively
                    var raise = cmd.GetType().GetMethod("RaiseCanExecuteChanged");
                    raise?.Invoke(cmd, null);
                }
            }
            catch { }
        }
    }    public class RelayCommand : ICommand
    {
        private readonly Action _action;
        private readonly Func<bool>? _canExecute;
        private IGestLogLogger? _debugLogger;

        public RelayCommand(Action action) : this(action, null) { }
        public RelayCommand(Action action, Func<bool>? canExecute)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            var result = _canExecute?.Invoke() ?? true;
            try { _debugLogger?.LogInformation("[DEBUG-CMD] CanExecute() invocado. canExecute={CanExecute}, result={Result}", _canExecute != null, result); } catch { }
            return result;
        }

        public void Execute(object? parameter) => _action();

        public void RaiseCanExecuteChanged()
        {
            try { _debugLogger?.LogInformation("[DEBUG-CMD] RaiseCanExecuteChanged() invocado. Subscriptores: {Count}", CanExecuteChanged?.GetInvocationList().Length ?? 0); } catch { }
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        // Setter interno para logging (será seteado desde el ViewModel)
        internal void SetDebugLogger(IGestLogLogger logger) => _debugLogger = logger;
    }
}
