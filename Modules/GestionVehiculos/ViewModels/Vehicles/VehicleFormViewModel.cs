using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Interfaces.Storage;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Enums;
using GestLog.Modules.GestionVehiculos.Views.Vehicles;
using GestLog.Services.Core.Logging;
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
        private ObservableCollection<string> marcasFiltradas = new();

        private List<string> _marcasDisponibles = new();
        private Guid? _editingVehicleId;

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
        private string? fuelType;

        public System.Collections.ObjectModel.ObservableCollection<string> FuelTypes { get; }

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

            // Fuel types: no incluir 'Hibrido' según requerimiento
            FuelTypes = new System.Collections.ObjectModel.ObservableCollection<string>(new[]
            {
                "No especificado",
                "Gasolina",
                "Diésel",
                "Eléctrico",
                "GNC",
                "GLP"
            });

            _ = CargarMarcasSugeridasAsync();
        }

        partial void OnBrandChanged(string value)
        {
            ActualizarMarcasFiltradas(value);
        }

        private async Task CargarMarcasSugeridasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _marcasDisponibles = await _vehicleService.GetSuggestedBrandsAsync(limit: 100, cancellationToken: cancellationToken);
                // Mostrar TODAS las marcas inicialmente
                MarcasFiltradas = new ObservableCollection<string>(_marcasDisponibles);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron cargar sugerencias de marcas de vehículos");
            }
        }

        private void ActualizarMarcasFiltradas(string? filtro)
        {
            var term = (filtro ?? string.Empty).Trim();
            var items = string.IsNullOrWhiteSpace(term)
                ? _marcasDisponibles
                : _marcasDisponibles.Where(m => m.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

            MarcasFiltradas = new ObservableCollection<string>(items);
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

                // Validar placa duplicada (permitir misma placa cuando se edita el mismo vehículo)
                var plateNormalized = Plate.Trim().ToUpper();
                var existingByPlate = await _vehicleService.GetByPlateAsync(plateNormalized, cancellationToken);
                if (existingByPlate != null && (!IsEditing || existingByPlate.Id != _editingVehicleId))
                {
                    ErrorMessage = $"Ya existe un vehículo con la placa '{Plate}'";
                    return;
                }

                // Crear DTO
                var vehicleDto = new VehicleDto
                {
                    Id = _editingVehicleId ?? Guid.NewGuid(),
                    Plate = plateNormalized,
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
                    FuelType = FuelType?.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                };

                // Guardar en BD
                VehicleDto savedVehicle;
                if (IsEditing && _editingVehicleId.HasValue)
                {
                    savedVehicle = await _vehicleService.UpdateAsync(_editingVehicleId.Value, vehicleDto, cancellationToken);
                    SuccessMessage = $"Vehículo '{savedVehicle.Brand} {savedVehicle.Model}' actualizado exitosamente";
                    _logger.LogDebug($"Vehículo actualizado: {savedVehicle.Plate} - {savedVehicle.Brand} {savedVehicle.Model} | Kilometraje: {savedVehicle.Mileage}");
                }
                else
                {
                    savedVehicle = await _vehicleService.CreateAsync(vehicleDto, cancellationToken);
                    SuccessMessage = $"Vehículo '{savedVehicle.Brand} {savedVehicle.Model}' registrado exitosamente";
                    _logger.LogDebug($"Vehículo creado: {savedVehicle.Plate} - {savedVehicle.Brand} {savedVehicle.Model} | Kilometraje: {savedVehicle.Mileage}");
                }

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
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validación de negocio al guardar vehículo");
                ErrorMessage = ex.Message;
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

                // Cargar imagen original
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(file);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 2000; // Limitar tamaño de carga para evitar consumo excesivo
                bitmap.EndInit();
                bitmap.Freeze();

                // Generar nombres seguros
                var guid = Guid.NewGuid().ToString("N");
                var originalFileName = guid + ".png";
                var thumbFileName = guid + "_thumb.png";

                // ========== VERSIÓN OPTIMIZADA PARA "ORIGINAL" (1200x960) ==========
                // Sin recorte: se ajusta manteniendo proporción y se centra en canvas
                var optimized = CreateFittedBitmap(bitmap, 1200, 960);

                // Guardar versión optimizada como PNG para preservar transparencia (evita fondo negro en PNG)
                var encoderOptimized = new PngBitmapEncoder();
                encoderOptimized.Frames.Add(BitmapFrame.Create(optimized));

                using (var ms = new MemoryStream())
                {
                    encoderOptimized.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    var savedPath = await _photoStorageService.SaveOriginalAsync(ms, originalFileName);
                    PhotoPath = savedPath;
                }

                // ========== VERSIÓN THUMBNAIL (320x256) ==========
                var thumb = CreateFittedBitmap(bitmap, 320, 256);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(thumb));

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

        private static BitmapSource CreateFittedBitmap(BitmapSource source, int targetWidth, int targetHeight)
        {
            double scale = Math.Min((double)targetWidth / source.PixelWidth, (double)targetHeight / source.PixelHeight);
            int drawWidth = Math.Max(1, (int)Math.Round(source.PixelWidth * scale));
            int drawHeight = Math.Max(1, (int)Math.Round(source.PixelHeight * scale));
            double offsetX = (targetWidth - drawWidth) / 2.0;
            double offsetY = (targetHeight - drawHeight) / 2.0;

            var visual = new System.Windows.Media.DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(System.Windows.Media.Brushes.Transparent, null, new Rect(0, 0, targetWidth, targetHeight));
                context.DrawImage(source, new Rect(offsetX, offsetY, drawWidth, drawHeight));
            }

            var render = new RenderTargetBitmap(targetWidth, targetHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            render.Render(visual);
            render.Freeze();
            return render;
        }

        /// <summary>
        /// Configura el ViewModel para crear un nuevo vehículo
        /// </summary>
        public void ConfigureForNew()
        {
            TituloDialog = "Agregar Vehículo";
            TextoBotonPrincipal = "Guardar";
            IsEditing = false;
            _editingVehicleId = null;
            ClearForm();
        }

        /// <summary>
        /// Configura el ViewModel para editar un vehículo existente
        /// </summary>
        public void ConfigureForEdit(VehicleDto vehicle)
        {
            _editingVehicleId = vehicle.Id;
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
            FuelType = vehicle.FuelType;

            _ = CargarMarcasSugeridasAsync();

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
            _editingVehicleId = null;
            FuelType = null;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            _ = CargarMarcasSugeridasAsync();
        }
    }
}
