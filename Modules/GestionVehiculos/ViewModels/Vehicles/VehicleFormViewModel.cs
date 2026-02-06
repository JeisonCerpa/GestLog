using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Enums;
using GestLog.Modules.GestionVehiculos.Views.Vehicles;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionVehiculos.Interfaces;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Vehicles
{
    /// <summary>
    /// ViewModel para el formulario de agregar/editar vehículos
    /// </summary>
    public partial class VehicleFormViewModel : ObservableObject
    {
        private readonly IVehicleService _vehicleService;
        private readonly IGestLogLogger _logger;
        private readonly IPhotoStorageService _photoStorageService;

        [ObservableProperty]
        private string tituloDialog = "Agregar Vehículo";

        [ObservableProperty]
        private string textoBotonPrincipal = "Guardar";

        [ObservableProperty]
        private string plate = string.Empty;

        [ObservableProperty]
        private string vin = string.Empty;

        [ObservableProperty]
        private string brand = string.Empty;

        [ObservableProperty]
        private string model = string.Empty;

        [ObservableProperty]
        private string? version;

        [ObservableProperty]
        private int year = DateTime.Now.Year;

        [ObservableProperty]
        private string? color;

        [ObservableProperty]
        private long mileage = 0;

        [ObservableProperty]
        private VehicleType selectedType = VehicleType.Particular;

        [ObservableProperty]
        private VehicleState selectedState = VehicleState.Activo;

        [ObservableProperty]
        private string? photoPath;

        [ObservableProperty]
        private string? photoThumbPath;

        [ObservableProperty]
        private bool isProcessing;

        [ObservableProperty]
        private bool isEditing = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        public ObservableCollection<VehicleType> VehicleTypes { get; }
        public ObservableCollection<VehicleState> VehicleStates { get; }

        public VehicleFormViewModel(IVehicleService vehicleService, IGestLogLogger logger, IPhotoStorageService photoStorageService)
        {
            _vehicleService = vehicleService;
            _logger = logger;
            _photoStorageService = photoStorageService;

            // Cargar tipos de vehículos y estados
            VehicleTypes = new ObservableCollection<VehicleType>(
                Enum.GetValues(typeof(VehicleType)) as VehicleType[] ?? Array.Empty<VehicleType>());

            VehicleStates = new ObservableCollection<VehicleState>(
                Enum.GetValues(typeof(VehicleState)) as VehicleState[] ?? Array.Empty<VehicleState>());
        }

        [RelayCommand]
        private async Task SaveAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsProcessing = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(Plate))
                {
                    ErrorMessage = "La placa del vehículo es obligatoria";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Vin))
                {
                    ErrorMessage = "El VIN es obligatorio";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Brand))
                {
                    ErrorMessage = "La marca del vehículo es obligatoria";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Model))
                {
                    ErrorMessage = "El modelo del vehículo es obligatorio";
                    return;
                }

                // Si es nuevo vehículo, validar que la placa no exista
                if (!IsEditing)
                {
                    var existingVehicle = await _vehicleService.ExistsByPlateAsync(Plate.Trim(), cancellationToken);
                    if (existingVehicle)
                    {
                        ErrorMessage = $"Ya existe un vehículo con la placa '{Plate}'";
                        return;
                    }
                }

                // Crear DTO
                var vehicleDto = new VehicleDto
                {
                    Id = Guid.NewGuid(),
                    Plate = Plate.Trim().ToUpper(),
                    Vin = Vin.Trim().ToUpper(),
                    Brand = Brand.Trim(),
                    Model = Model.Trim(),
                    Version = Version?.Trim(),
                    Year = Year,
                    Color = Color?.Trim(),
                    Mileage = Mileage,
                    Type = SelectedType,
                    State = SelectedState,
                    PhotoPath = PhotoPath,
                    PhotoThumbPath = PhotoThumbPath,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                };

                // Guardar en BD
                var savedVehicle = await _vehicleService.CreateAsync(vehicleDto, cancellationToken);

                SuccessMessage = $"Vehículo '{savedVehicle.Brand} {savedVehicle.Model}' registrado exitosamente";
                _logger.LogInformation($"Vehículo creado: {savedVehicle.Plate} - {savedVehicle.Brand} {savedVehicle.Model} | Kilometraje: {savedVehicle.Mileage}");

                // Mostrar mensaje de éxito y cerrar
                await Task.Delay(1500);
                
                // Establecer DialogResult = true antes de cerrar para que HomeViewModel sepa que fue exitoso
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Application.Current.Windows.Cast<Window>()
                        .FirstOrDefault(w => w.DataContext == this) is VehicleFormDialog dialog)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando vehículo");
                ErrorMessage = "Error al guardar el vehículo. Verifique los datos e intente nuevamente.";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task SelectPhotoAsync()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp";
                dlg.Multiselect = false;

                var result = dlg.ShowDialog();
                if (result != true) return;

                var file = dlg.FileName;
                var ext = Path.GetExtension(file)?.ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
                if (!allowed.Contains(ext))
                {
                    ErrorMessage = "Formato de imagen no permitido. Use JPG o PNG.";
                    return;
                }

                var fileInfo = new FileInfo(file);
                const long maxBytes = 5 * 1024 * 1024; // 5 MB
                if (fileInfo.Length > maxBytes)
                {
                    ErrorMessage = "La imagen excede el tamaño máximo (5 MB).";
                    return;
                }

                // Generar nombres seguros
                var guid = Guid.NewGuid().ToString("N");
                var originalFileName = guid + ext;
                var thumbFileName = guid + "_thumb.jpg";

                // Guardar original al storage
                using (var fs = File.OpenRead(file))
                {
                    var savedPath = await _photoStorageService.SaveOriginalAsync(fs, originalFileName);
                    PhotoPath = savedPath;
                }

                // Generar thumbnail en memoria y guardar
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(file);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // Target thumbnail size and aspect (5:4)
                int targetThumbWidth = 320;
                int targetThumbHeight = 256; // 320 * 4/5
                double targetAspect = 5.0 / 4.0;

                int srcW = bitmap.PixelWidth;
                int srcH = bitmap.PixelHeight;

                // Calcular region de recorte centrada para mantener 5:4
                int cropW, cropH, cropX, cropY;
                double srcAspect = (double)srcW / srcH;
                if (srcAspect > targetAspect)
                {
                    // Imagen más ancha -> recortar los lados
                    cropH = srcH;
                    cropW = (int)Math.Round(srcH * targetAspect);
                    cropX = (srcW - cropW) / 2;
                    cropY = 0;
                }
                else
                {
                    // Imagen más alta -> recortar arriba/abajo
                    cropW = srcW;
                    cropH = (int)Math.Round(srcW / targetAspect);
                    cropX = 0;
                    cropY = (srcH - cropH) / 2;
                }

                // Asegurar valores válidos
                cropW = Math.Max(1, Math.Min(cropW, srcW));
                cropH = Math.Max(1, Math.Min(cropH, srcH));
                cropX = Math.Max(0, Math.Min(cropX, srcW - cropW));
                cropY = Math.Max(0, Math.Min(cropY, srcH - cropH));

                var cropped = new CroppedBitmap(bitmap, new System.Windows.Int32Rect(cropX, cropY, cropW, cropH));

                // Escalar al tamaño objetivo
                double scaleX = (double)targetThumbWidth / cropW;
                double scaleY = (double)targetThumbHeight / cropH;
                var transform = new System.Windows.Media.ScaleTransform(scaleX, scaleY);
                var resized = new TransformedBitmap(cropped, transform);

                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 85;
                encoder.Frames.Add(BitmapFrame.Create(resized));

                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    var thumbSaved = await _photoStorageService.SaveThumbnailAsync(ms, thumbFileName);
                    PhotoThumbPath = thumbSaved;
                }

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting photo");
                ErrorMessage = "Error al procesar la imagen. Intente con otra imagen.";
            }
        }

        /// <summary>
        /// Configura el ViewModel para crear un nuevo vehículo
        /// </summary>
        public void ConfigureForNew()
        {
            TituloDialog = "Agregar Vehículo";
            TextoBotonPrincipal = "Guardar";
            IsEditing = false;
            ClearForm();
        }

        /// <summary>
        /// Configura el ViewModel para editar un vehículo existente
        /// </summary>
        public void ConfigureForEdit(VehicleDto vehicle)
        {
            Plate = vehicle.Plate ?? string.Empty;
            Vin = vehicle.Vin ?? string.Empty;
            Brand = vehicle.Brand ?? string.Empty;
            Model = vehicle.Model ?? string.Empty;
            Version = vehicle.Version;
            Year = vehicle.Year;
            Color = vehicle.Color;
            Mileage = vehicle.Mileage;
            SelectedType = vehicle.Type;
            SelectedState = vehicle.State;
            PhotoPath = vehicle.PhotoPath;
            PhotoThumbPath = vehicle.PhotoThumbPath;

            TituloDialog = "Editar Vehículo";
            TextoBotonPrincipal = "Actualizar";
            IsEditing = true;
        }

        private void ClearForm()
        {
            Plate = string.Empty;
            Vin = string.Empty;
            Brand = string.Empty;
            Model = string.Empty;
            Version = null;
            Year = DateTime.Now.Year;
            Color = null;
            Mileage = 0;
            SelectedType = VehicleType.Particular;
            SelectedState = VehicleState.Activo;
            PhotoPath = null;
            PhotoThumbPath = null;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }
    }
}
