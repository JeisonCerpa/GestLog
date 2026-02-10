using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionVehiculos.Interfaces
{
    public interface IPhotoStorageService
    {
        Task<string> SaveOriginalAsync(Stream fileStream, string fileName, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
        Task<string> SaveThumbnailAsync(Stream thumbnailStream, string fileName, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string path);
        /// <summary>
        /// Retorna una URI pública o ruta adecuada para uso en Image Source
        /// </summary>
        Task<string> GetUriAsync(string path);
    }
}