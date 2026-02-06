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

            // Asegurar que las carpetas base existen: Vehiculos/photos/originals y Vehiculos/photos/photothumb
            try
            {
                var photosRoot = Path.Combine(_basePath, "Vehiculos", "photos");
                Directory.CreateDirectory(photosRoot);
                Directory.CreateDirectory(GetOriginalsFolder());
                Directory.CreateDirectory(GetPhotoThumbFolder());
            }
            catch
            {
                // No lanzar aquí; se manejará en tiempo de escritura. Evitar fallos en el arranque por permisos temporales.
            }
        }

        private string GetOriginalsFolder()
        {
            return Path.Combine(_basePath, "Vehiculos", "photos", "originals");
        }

        private string GetPhotoThumbFolder()
        {
            return Path.Combine(_basePath, "Vehiculos", "photos", "photothumb");
        }

        public async Task<string> SaveOriginalAsync(Stream fileStream, string fileName)
        {
            var originals = GetOriginalsFolder();
            Directory.CreateDirectory(originals);

            var dest = Path.Combine(originals, fileName);
            using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await fileStream.CopyToAsync(fs);
            }

            return dest;
        }

        public async Task<string> SaveThumbnailAsync(Stream thumbnailStream, string fileName)
        {
            var thumbs = GetPhotoThumbFolder();
            Directory.CreateDirectory(thumbs);

            var dest = Path.Combine(thumbs, fileName);
            using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await thumbnailStream.CopyToAsync(fs);
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
    }
}