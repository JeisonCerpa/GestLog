using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace GestLog.Services.Core.Logging;

/// <summary>
/// Implementación del servicio de logging específico para GestLog
/// Proporciona logging estructurado y contextual para todas las operaciones
/// </summary>
public class GestLogLogger : IGestLogLogger
{
    public ILogger Logger { get; }

    public GestLogLogger(ILogger<GestLogLogger> logger)
    {
        Logger = logger;
    }

    public void LogExcelProcessingStarted(string folderPath, int fileCount)
    {
        Logger.LogInformation("📂 Iniciando procesamiento de archivos Excel: {FolderPath} ({FileCount} archivos)",
            folderPath, fileCount);
    }

    public void LogExcelProcessingProgress(string fileName, int currentFile, int totalFiles, double percentage)
    {
        Logger.LogDebug("📊 Procesando archivo {CurrentFile}/{TotalFiles} ({Percentage:F1}%): {FileName}",
            currentFile, totalFiles, percentage, fileName);
    }

    public void LogExcelProcessingCompleted(string outputPath, TimeSpan duration, int processedFiles)
    {
        Logger.LogInformation("✅ Procesamiento completado exitosamente: {OutputPath} " +
            "({ProcessedFiles} archivos en {Duration:mm\\:ss})",
            outputPath, processedFiles, duration);
    }

    public void LogExcelProcessingError(string fileName, Exception exception)
    {
        Logger.LogError(exception, "❌ Error procesando archivo Excel: {FileName}", fileName);
    }

    public void LogOperationCancelled(string operationType, string reason = "")
    {
        if (string.IsNullOrEmpty(reason))
        {
            Logger.LogWarning("⏹️ Operación cancelada: {OperationType}", operationType);
        }
        else
        {
            Logger.LogWarning("⏹️ Operación cancelada: {OperationType} - Razón: {Reason}", operationType, reason);
        }
    }

    public void LogNavigation(string fromView, string toView, string userId = "")
    {
        if (string.IsNullOrEmpty(userId))
        {
            Logger.LogDebug("🧭 Navegación: {FromView} → {ToView}", fromView, toView);
        }
        else
        {
            Logger.LogDebug("🧭 Navegación: {FromView} → {ToView} (Usuario: {UserId})", fromView, toView, userId);
        }
    }

    public void LogPerformance(string operation, TimeSpan duration, Dictionary<string, object>? properties = null)
    {
        if (properties == null || properties.Count == 0)
        {
            Logger.LogInformation("⚡ Rendimiento: {Operation} completada en {Duration:mm\\:ss\\.fff}",
                operation, duration);
        }
        else
        {
            Logger.LogInformation("⚡ Rendimiento: {Operation} completada en {Duration:mm\\:ss\\.fff} - Propiedades: {@Properties}",
                operation, duration, properties);
        }
    }

    public void LogConfiguration(string setting, object value, string source = "")
    {
        if (string.IsNullOrEmpty(source))
        {
            Logger.LogDebug("⚙️ Configuración cargada: {Setting} = {Value}", setting, value);
        }
        else
        {
            Logger.LogDebug("⚙️ Configuración cargada: {Setting} = {Value} (Fuente: {Source})", setting, value, source);
        }
    }

    public void LogUnhandledException(Exception exception, string context = "")
    {
        if (string.IsNullOrEmpty(context))
        {
            Logger.LogCritical(exception, "💥 Excepción no manejada detectada");
        }
        else
        {
            Logger.LogCritical(exception, "💥 Excepción no manejada detectada en: {Context}", context);
        }
    }
}

/// <summary>
/// Extensiones para facilitar el uso del logger
/// </summary>
public static class GestLogLoggerExtensions
{
    /// <summary>
    /// Crea un scope de logging para operaciones específicas
    /// </summary>
    public static IDisposable BeginScope(this IGestLogLogger logger, string operation, Dictionary<string, object>? properties = null)
    {
        var scopeProperties = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["OperationId"] = Guid.NewGuid().ToString("N")[..8],
            ["StartTime"] = DateTime.UtcNow
        };

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                scopeProperties[prop.Key] = prop.Value;
            }
        }

        return logger.Logger.BeginScope(scopeProperties) ?? new EmptyDisposable();
    }

    /// <summary>
    /// Crea un scope de operación para el logger (alias para BeginScope)
    /// </summary>
    public static IDisposable BeginOperationScope(this IGestLogLogger logger, string operation, object? properties = null)
    {
        var dict = properties != null ? 
            new Dictionary<string, object> { ["Properties"] = properties } : 
            null;
        return BeginScope(logger, operation, dict);
    }

    /// <summary>
    /// Log de interacciones de usuario
    /// </summary>
    public static void LogUserInteraction(this IGestLogLogger logger, string icon, string action, string message, params object[] args)
    {
        logger.Logger.LogInformation($"{icon} {message}", args);
    }

    /// <summary>
    /// Log de interacciones de usuario (nivel configurable)
    /// </summary>
    public static void LogUserInteraction(this IGestLogLogger logger, string icon, string action, string message, bool asDebug, params object[] args)
    {
        if (asDebug)
            logger.Logger.LogDebug($"{icon} {message}", args);
        else
            logger.Logger.LogInformation($"{icon} {message}", args);
    }

    /// <summary>
    /// Log de inicio de aplicación
    /// </summary>
    public static void LogApplicationStarted(this IGestLogLogger logger, string message, params object[] args)
    {
        logger.Logger.LogInformation($"🚀 {message}", args);
    }

    /// <summary>
    /// Log de errores
    /// </summary>
    public static void LogError(this IGestLogLogger logger, Exception exception, string message, params object[] args)
    {
        logger.Logger.LogError(exception, $"❌ {message}", args);
    }    /// <summary>
    /// Log de debug
    /// </summary>
    public static void LogDebug(this IGestLogLogger logger, string message, params object[] args)
    {
        logger.Logger.LogDebug(message, args);
    }    /// <summary>
    /// Log de advertencias
    /// </summary>
    public static void LogWarning(this IGestLogLogger logger, string message, params object[] args)
    {
        logger.Logger.LogWarning(message, args);
    }

    /// <summary>
    /// Log de advertencias con excepción
    /// </summary>
    public static void LogWarning(this IGestLogLogger logger, Exception exception, string message, params object[] args)
    {
        logger.Logger.LogWarning(exception, message, args);
    }

    /// <summary>
    /// Log de información
    /// </summary>
    public static void LogInformation(this IGestLogLogger logger, string message, params object[] args)
    {
        logger.Logger.LogInformation(message, args);
    }

    /// <summary>
    /// Ejecuta una operación con logging automático de duración
    /// </summary>
    public static async Task<T> LoggedOperationAsync<T>(this IGestLogLogger logger, string operationName, 
        Func<Task<T>> operation, Dictionary<string, object>? properties = null)
    {
        using var scope = logger.BeginScope(operationName, properties);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.Logger.LogInformation("🚀 Iniciando operación: {OperationName}", operationName);
            var result = await operation();
            stopwatch.Stop();
            logger.LogPerformance(operationName, stopwatch.Elapsed, properties);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.Logger.LogError(ex, "❌ Error en operación {OperationName} después de {Duration:mm\\:ss\\.fff}", 
                operationName, stopwatch.Elapsed);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta una operación sin resultado con logging automático de duración
    /// </summary>
    public static async Task LoggedOperationAsync(this IGestLogLogger logger, string operationName, 
        Func<Task> operation, Dictionary<string, object>? properties = null)
    {
        using var scope = logger.BeginScope(operationName, properties);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.Logger.LogInformation("🚀 Iniciando operación: {OperationName}", operationName);
            await operation();
            stopwatch.Stop();
            logger.LogPerformance(operationName, stopwatch.Elapsed, properties);
        }
        catch (Exception ex)
        {        stopwatch.Stop();
            logger.Logger.LogError(ex, "❌ Error en operación {OperationName} después de {Duration:mm\\:ss\\.fff}", 
                operationName, stopwatch.Elapsed);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta una operación síncrona con logging automático de duración (con resultado)
    /// </summary>
    public static T LoggedOperation<T>(this IGestLogLogger logger, string operationName, 
        Func<T> operation, Dictionary<string, object>? properties = null)
    {
        using var scope = logger.BeginScope(operationName, properties);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.Logger.LogInformation("🚀 Iniciando operación: {OperationName}", operationName);
            var result = operation();
            stopwatch.Stop();
            logger.LogPerformance(operationName, stopwatch.Elapsed, properties);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.Logger.LogError(ex, "❌ Error en operación {OperationName} después de {Duration:mm\\:ss\\.fff}", 
                operationName, stopwatch.Elapsed);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta una operación síncrona sin resultado con logging automático de duración
    /// </summary>
    public static void LoggedOperation(this IGestLogLogger logger, string operationName, 
        Action operation, Dictionary<string, object>? properties = null)
    {
        using var scope = logger.BeginScope(operationName, properties);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.Logger.LogInformation("🚀 Iniciando operación: {OperationName}", operationName);
            operation();
            stopwatch.Stop();
            logger.LogPerformance(operationName, stopwatch.Elapsed, properties);
        }
        catch (Exception ex)        {
            stopwatch.Stop();
            logger.Logger.LogError(ex, "❌ Error en operación {OperationName} después de {Duration:mm\\:ss\\.fff}", 
                operationName, stopwatch.Elapsed);
            throw;
        }
    }
}

/// <summary>
/// Implementación vacía de IDisposable para casos donde BeginScope devuelve null
/// </summary>
internal sealed class EmptyDisposable : IDisposable
{
    public void Dispose() { }
}
