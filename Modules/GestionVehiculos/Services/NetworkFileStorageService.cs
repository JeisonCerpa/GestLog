using GestLog.Modules.GestionVehiculos.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionVehiculos.Services
{
    public class NetworkFileStorageService : IPhotoStorageService
    {
        private readonly string _basePath;

        public NetworkFileStorageService(IConfiguration configuration)
        {
            // Leer ruta desde configuración, fallback a una ruta UNC por defecto
            _basePath = configuration["VehiclePhotos:Path"] ?? "\\\\mi-servidor\\GestLog\\VehiclePhotos";
            if (string.IsNullOrWhiteSpace(_basePath))
                throw new InvalidOperationException("VehiclePhotos:Path no está configurado");

            // Asegurar que las carpetas base existen: Vehiculos/photos/... y Vehiculos/documents/...
            try
            {
                var photosRoot = Path.Combine(_basePath, "Vehiculos", "photos");
                var documentsRoot = Path.Combine(_basePath, "Vehiculos", "documents");
                Directory.CreateDirectory(photosRoot);
                Directory.CreateDirectory(documentsRoot);
                Directory.CreateDirectory(GetPhotoOriginalsFolder());
                Directory.CreateDirectory(GetPhotoThumbFolder());
                Directory.CreateDirectory(GetDocumentOriginalsFolder());
                Directory.CreateDirectory(GetDocumentThumbnailFolder());
            }
            catch
            {
                // No lanzar aquí; se manejará en tiempo de escritura. Evitar fallos en el arranque por permisos temporales.
            }
        }

        private string GetPhotoOriginalsFolder()
        {
            return Path.Combine(_basePath, "Vehiculos", "photos", "originals");
        }

        private string GetPhotoThumbFolder()
        {
            return Path.Combine(_basePath, "Vehiculos", "photos", "photothumb");
        }

        private string GetDocumentOriginalsFolder()
        {
            return Path.Combine(_basePath, "Vehiculos", "documents", "originals");
        }

        private string GetDocumentThumbnailFolder()
        {
            return Path.Combine(_basePath, "Vehiculos", "documents", "thumbnails");
        }

        private bool IsImageFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif";
        }

        public async Task<string> SaveOriginalAsync(Stream fileStream, string fileName, IProgress<double>? progress = null, System.Threading.CancellationToken cancellationToken = default)
        {
            // Elegir carpeta según tipo de archivo: imágenes -> photos, documentos -> documents
            var originals = IsImageFile(fileName) ? GetPhotoOriginalsFolder() : GetDocumentOriginalsFolder();
            Directory.CreateDirectory(originals);

            var dest = Path.Combine(originals, fileName);
            const int bufferSize = 81920;
            long total = 0;
            try
            {
                if (fileStream.CanSeek) total = fileStream.Length;
            }
            catch { }

            using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var buffer = new byte[bufferSize];
                int read;
                long written = 0;
                while ((read = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, read, cancellationToken);
                    written += read;
                    if (total > 0 && progress != null)
                    {
                        var pct = Math.Min(100.0, (written / (double)total) * 100.0);
                        progress.Report(pct);
                    }
                }
            }

            return dest;
        }

        public async Task<string> SaveThumbnailAsync(Stream thumbnailStream, string fileName, IProgress<double>? progress = null, System.Threading.CancellationToken cancellationToken = default)
        {
            // Para imágenes guardamos en la carpeta de thumbnails de photos; para documentos usamos thumbnails de documents
            var thumbs = IsImageFile(fileName) ? GetPhotoThumbFolder() : GetDocumentThumbnailFolder();
            Directory.CreateDirectory(thumbs);

            var dest = Path.Combine(thumbs, fileName);
            const int bufferSize = 81920;
            long total = 0;
            try { if (thumbnailStream.CanSeek) total = thumbnailStream.Length; } catch { }

            using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var buffer = new byte[bufferSize];
                int read;
                long written = 0;
                while ((read = await thumbnailStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, read, cancellationToken);
                    written += read;
                    if (total > 0 && progress != null)
                    {
                        var pct = Math.Min(100.0, (written / (double)total) * 100.0);
                        progress.Report(pct);
                    }
                }
            }

            return dest;
        }

        public Task<bool> DeleteAsync(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(false);
                if (File.Exists(path)) File.Delete(path);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<string> GetUriAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(string.Empty);
            return Task.FromResult(path);
        }

        public async Task<bool> MoveAsync(string sourcePath, string destPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destPath)) return false;

            try
            {
                var destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrWhiteSpace(destDir)) Directory.CreateDirectory(destDir);

                // Intentar copy+delete con backoff
                const int maxRetries = 3;
                int attempt = 0;
                while (attempt < maxRetries)
                {
                    try
                    {
                        File.Copy(sourcePath, destPath, true);
                        File.Delete(sourcePath);
                        return await Task.FromResult(true);
                    }
                    catch (IOException)
                    {
                        attempt++;
                        await Task.Delay(200 * attempt);
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}