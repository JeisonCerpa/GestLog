using System;
using System.IO;

namespace GestLog.Modules.GestionCartera.Services;

/// <summary>
/// Utilidades para saber, sí o no, si un archivo está abierto/bloqueado por otra aplicación.
/// ClosedXML e iText solo lanzan IOException cruda (en inglés); esto da una respuesta clara.
/// </summary>
internal static class FileAccessHelper
{
    /// <summary>
    /// True si el archivo existe pero está bloqueado por otro proceso
    /// (p. ej. el Excel abierto en Excel, o el PDF abierto en un visor).
    /// </summary>
    public static bool IsLocked(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;
        try
        {
            // FileShare.None exige acceso exclusivo: si otra app lo tiene abierto, falla.
            using var _ = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    /// <summary>
    /// True si la excepción (o alguna interna) corresponde a un archivo en uso/bloqueado por otro proceso.
    /// Úsese como respaldo cuando el bloqueo ocurre durante la escritura (no en un chequeo previo).
    /// </summary>
    public static bool IsLockException(Exception? ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is UnauthorizedAccessException)
                return true;
            if (e is IOException io)
            {
                int code = io.HResult & 0xFFFF;
                if (code == 32 || code == 33) // ERROR_SHARING_VIOLATION / ERROR_LOCK_VIOLATION
                    return true;
                if (io.Message.IndexOf("another process", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    io.Message.IndexOf("being used", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }
        return false;
    }
}
