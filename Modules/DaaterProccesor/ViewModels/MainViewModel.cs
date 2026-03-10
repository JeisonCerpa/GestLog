using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ClosedXML.Excel;
using Ookii.Dialogs.Wpf;
using Microsoft.Win32;
using System.Diagnostics;
using GestLog.Modules.DaaterProccesor.Services;
using GestLog.Modules.DaaterProccesor.Exceptions;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.Modules.DaaterProccesor.ViewModels;

public partial class MainViewModel : ObservableObject
{    [ObservableProperty]
    private double progress = 0.0; // ✅ Inicializar explícitamente en 0    // ✅ Método para actualizar el progreso con validación
    public void UpdateProgress(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            _logger.LogWarning("⚠️ Intento de asignar valor inválido al progreso: {Value}. Usando 0.0", value);
            Progress = 0.0;
        }
        else
        {
            Progress = Math.Max(0.0, Math.Min(100.0, value)); // Asegurar rango [0, 100]
        }
    }

    // ✅ Método para resetear el progreso de manera segura
    public void ResetProgress()
    {
        Progress = 0.0;
        StatusMessage = "Listo para procesar archivos.";
        _logger.LogDebug("🔄 Progreso reseteado a 0%");
    }

    [ObservableProperty]
    private string executablePath = string.Empty;

    [ObservableProperty]
    private bool isProcessing = false; // ✅ Inicializar explícitamente
    
    [ObservableProperty]
    private string? statusMessage;

    public string LogoPath { get; private set; } = string.Empty;
    
    private readonly IExcelProcessingService _excelService;
    private readonly IGestLogLogger _logger;
    private readonly CurrentUserInfo _currentUser;
    private CancellationTokenSource? _cancellationTokenSource;
    
    // Servicio de progreso suavizado para animación fluida
    private SmoothProgressService _smoothProgress = null!; // Será inicializado en InitializeViewModel

    // Propiedades de acceso
    public bool CanAccessDaaterProcessor => _currentUser.HasPermission("Herramientas.AccederDaaterProccesor");
    public bool CanProcessFiles => _currentUser.HasPermission("DaaterProccesor.ProcesarArchivos");

    // Constructor para usar desde DI
    public MainViewModel()
    {
        // Obtener servicios del contenedor de DI
        var serviceProvider = LoggingService.GetServiceProvider();
        _excelService = serviceProvider.GetRequiredService<IExcelProcessingService>();
        _logger = serviceProvider.GetRequiredService<IGestLogLogger>();
        // Inicializar usuario actual por defecto (solo para evitar null)
        _currentUser = new CurrentUserInfo
        {
            UserId = Guid.Empty,
            Username = "",
            FullName = "",
            Permissions = new List<string>()
        };
        InitializeViewModel();
    }
    
    // Constructor para pruebas o instanciación manual
    public MainViewModel(IExcelProcessingService excelService, IGestLogLogger logger, CurrentUserInfo currentUser)
    {
        _excelService = excelService;
        _logger = logger;
        _currentUser = currentUser;
        
        // Inicializar propiedades comunes
        InitializeViewModel();
    }
    
    // Método común de inicialización
    private void InitializeViewModel()
    {
        // Ruta base del proyecto
        var basePath = AppDomain.CurrentDomain.BaseDirectory;

        // Ruta del logo
        LogoPath = Path.Combine(basePath, "Assets", "logo.png");

        // Ruta real del ejecutable
        string rutaReal = Process.GetCurrentProcess().MainModule?.FileName 
            ?? throw new InvalidOperationException("No se pudo obtener la ruta del ejecutable.");
        var directorioReal = Path.GetDirectoryName(rutaReal);
        if (directorioReal == null)
        {
            throw new InvalidOperationException("No se pudo obtener el directorio real del ejecutable.");
        }

        // Crear la carpeta Output en la ruta real del ejecutable
        var outputFolder = Path.Combine(directorioReal, "Output");
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        
        // Asignar la ruta del directorio real al ExecutablePath para mostrarla en la interfaz
        ExecutablePath = directorioReal;
        StatusMessage = "Listo para procesar archivos.";
          // Inicializar el servicio de progreso suavizado con validación
        _smoothProgress = new SmoothProgressService(value => UpdateProgress(value));
    }

    [RelayCommand(CanExecute = nameof(CanProcessExcelFiles))]
    public async Task ProcessExcelFilesAsync()
    {
        if (IsProcessing) return;
        
        var stopwatch = Stopwatch.StartNew();
        IsProcessing = true;
        
        // Establecer directamente el valor 0 para reiniciar la barra sin animación
        _smoothProgress.SetValueDirectly(0);
        StatusMessage = "Iniciando procesamiento...";
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Notificar cambios en los comandos
        ProcessExcelFilesCommand.NotifyCanExecuteChanged();
        CancelProcessingCommand.NotifyCanExecuteChanged();
        
        _logger.Logger.LogInformation("🎯 Usuario inició procesamiento de archivos Excel");
        
        try
        {
            var folderDialog = new VistaFolderBrowserDialog
            {
                Description = "Selecciona una carpeta que contenga archivos Excel",
                UseDescriptionForTitle = true
            };

            if (folderDialog.ShowDialog() == true)
            {
                var folderPath = folderDialog.SelectedPath;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    // Contar archivos Excel en la carpeta
                    var excelFiles = Directory.GetFiles(folderPath, "*.xlsx", SearchOption.AllDirectories)
                        .Where(f => !Path.GetFileName(f).StartsWith("~$")).ToArray();
                    
                    _logger.LogExcelProcessingStarted(folderPath, excelFiles.Length);
                    StatusMessage = "Procesando archivos Excel...";
                    
                    // Usar el servicio de progreso suavizado para animaciones fluidas
                    var progress = new System.Progress<double>(p => 
                    {
                        // Reportar al servicio suavizado en lugar de directamente a la propiedad Progress
                        _smoothProgress.Report(p);
                        StatusMessage = $"Procesando archivos... {p:F1}%";
                        _logger.Logger.LogDebug("📊 Progreso de procesamiento: {Progress:F1}%", p);
                    });

                    // Procesamiento asíncrono de archivos Excel con logging
                    var resultado = await _logger.LoggedOperationAsync("ProcesarArchivosExcel", 
                        () => _excelService.ProcesarArchivosExcelAsync(folderPath, progress, _cancellationTokenSource.Token),
                        new Dictionary<string, object>
                        {
                            ["FolderPath"] = folderPath,
                            ["FileCount"] = excelFiles.Length
                        });
                    
                    // Verificar cancelación inmediatamente después del procesamiento
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    
                    StatusMessage = "Generando archivo consolidado...";
                    _logger.Logger.LogInformation("📝 Generando archivo consolidado con {RowCount} filas", resultado.Rows.Count);

                    string rutaReal = Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("No se pudo obtener la ruta del ejecutable.");
                    var directorioReal = Path.GetDirectoryName(rutaReal) ?? AppDomain.CurrentDomain.BaseDirectory;
                    var outputFolderDefault = Path.Combine(directorioReal, "Output");
                    if (!Directory.Exists(outputFolderDefault))
                    {
                        Directory.CreateDirectory(outputFolderDefault);
                        _logger.Logger.LogDebug("📁 Carpeta de salida por defecto creada: {OutputFolder}", outputFolderDefault);
                    }

                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                        FileName = $"Consolidado_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                        InitialDirectory = outputFolderDefault,
                        Title = "Guardar archivo consolidado"
                    };

                    var ownerWindow = System.Windows.Application.Current?.MainWindow;
                    var saveResult = ownerWindow != null
                        ? saveFileDialog.ShowDialog(ownerWindow)
                        : saveFileDialog.ShowDialog();

                    if (saveResult != true)
                    {
                        _logger.Logger.LogInformation("👤 Usuario canceló la selección de ubicación para el archivo consolidado");
                        StatusMessage = "Operación cancelada por el usuario.";
                        System.Windows.MessageBox.Show("No se generó el archivo porque se canceló la selección de ubicación.", "Cancelado", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var outputFilePath = saveFileDialog.FileName;
                    
                    // Generar archivo consolidado de forma asíncrona con logging
                    await _logger.LoggedOperationAsync("GenerarArchivoConsolidado",
                        () => _excelService.GenerarArchivoConsolidadoAsync(resultado, outputFilePath, _cancellationTokenSource.Token),
                        new Dictionary<string, object>
                        {
                            ["OutputPath"] = outputFilePath,
                            ["RowCount"] = resultado.Rows.Count
                        });
                    
                    // Verificar cancelación después de la generación
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                      stopwatch.Stop();
                    _logger.LogExcelProcessingCompleted(outputFilePath, stopwatch.Elapsed, excelFiles.Length);
                    
                    // Secuencia de finalización con animación suave para una experiencia satisfactoria
                    await Task.Run(async () => {
                        // Pausa breve para asegurar que el usuario vea que el proceso ha finalizado
                        await Task.Delay(200);
                        
                        // Primero llevamos la barra al 99% si aún no lo está
                        if (_smoothProgress != null)
                        {
                            // Si el progreso actual es menor al 90%, hacemos una animación rápida al 99%
                            if (Progress < 90)
                            {
                                _smoothProgress.Report(99);
                                await Task.Delay(300);
                            }
                            // Si ya está por encima del 90%, solo lo llevamos suavemente al 99%
                            else if (Progress < 99)
                            {
                                _smoothProgress.Report(99);
                                await Task.Delay(200);
                            }
                            
                            // Finalmente completamos al 100% con una pausa para efecto visual
                            _smoothProgress.Report(100);
                        }
                        
                        // Actualizar mensaje final
                        var dispatcher = System.Windows.Application.Current?.Dispatcher;
                        if (dispatcher != null)
                        {
                            await dispatcher.InvokeAsync(() =>
                            {
                                StatusMessage = "Procesamiento completado exitosamente.";
                            });
                        }
                        else
                        {
                            StatusMessage = "Procesamiento completado exitosamente.";
                        }
                    });
                    
                    System.Windows.MessageBox.Show($"Archivo consolidado generado exitosamente en: {outputFilePath}", "Éxito");
                }
            }
            else
            {
                _logger.Logger.LogInformation("👤 Usuario canceló la selección de carpeta");
                StatusMessage = "Operación cancelada por el usuario.";
            }
        }        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogOperationCancelled("ProcessExcelFiles", $"Tiempo transcurrido: {stopwatch.Elapsed:mm\\:ss}");
            
            // No reiniciar el progreso para que el usuario pueda ver dónde se detuvo
            StatusMessage = "Operación cancelada por el usuario.";
            System.Windows.MessageBox.Show("Operación cancelada por el usuario.", "Cancelado");
        }
        catch (ExcelFormatException ex)
        {
            stopwatch.Stop();
            _logger.LogExcelProcessingError("Excel_Format_Error", ex);
            _logger.LogPerformance("ProcessExcelFiles_Error", stopwatch.Elapsed);
            
            ResetProgress();
            StatusMessage = $"Error de formato Excel: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Error de formato en archivo Excel: {ex.Message}\n\nVerifique que el archivo tenga todas las columnas requeridas.", 
                "Error de formato");
        }
        catch (FileValidationException ex)
        {
            stopwatch.Stop();
            _logger.LogExcelProcessingError("File_Validation_Error", ex);
            _logger.LogPerformance("ProcessExcelFiles_Error", stopwatch.Elapsed);
            
            ResetProgress();
            StatusMessage = $"Error de archivo: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Error en el archivo: {ex.Message}\n\nVerifique que los archivos existan y tengan el formato correcto.", 
                "Error de archivo");
        }
        catch (ResourceException ex)
        {
            stopwatch.Stop();
            _logger.LogExcelProcessingError("Resource_Error", ex);
            _logger.LogPerformance("ProcessExcelFiles_Error", stopwatch.Elapsed);
            
            ResetProgress();
            StatusMessage = $"Error de recursos: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Error en archivo de recursos: {ex.Message}\n\nVerifique que los archivos de recursos estén correctamente instalados.", 
                "Error de recursos");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogExcelProcessingError("ProcessExcelFilesAsync", ex);
            _logger.LogPerformance("ProcessExcelFiles_Error", stopwatch.Elapsed);
            
            // ✅ Reset seguro del progreso en caso de error
            ResetProgress();
            StatusMessage = $"Error: {ex.Message}";
            System.Windows.MessageBox.Show($"Ocurrió un error inesperado: {ex.Message}\n\nPor favor contacte al soporte técnico.", "Error");
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            ProcessExcelFilesCommand.NotifyCanExecuteChanged();
            CancelProcessingCommand.NotifyCanExecuteChanged();
            
            if (string.IsNullOrEmpty(StatusMessage) || StatusMessage.Contains("Error"))
                StatusMessage = "Listo para procesar archivos.";
                
            _logger.Logger.LogDebug("🏁 Finalizando ProcessExcelFilesAsync después de {Duration:mm\\:ss}", stopwatch.Elapsed);
        }
    }    [RelayCommand(CanExecute = nameof(CanCancelProcessing))]
    public void CancelProcessing()
    {
        _logger.Logger.LogInformation("⏹️ Usuario solicitó cancelación de operación");
        
        // Cancelar la operación
        _cancellationTokenSource?.Cancel();
        
        // Actualizar la UI
        StatusMessage = "Cancelando operación...";
        
        // Dejar que la barra de progreso se quede donde está para mostrar 
        // visualmente dónde se detuvo el proceso
        
        _logger.Logger.LogDebug("🔄 Token de cancelación activado");
    }

    private bool CanProcessExcelFiles() => !IsProcessing && CanProcessFiles;

    private bool CanCancelProcessing() 
    {
        return IsProcessing && _cancellationTokenSource != null;
    }
}
