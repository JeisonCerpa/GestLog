using System.IO;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionVehiculos.Interfaces
{
    public interface IPhotoStorageService
    {
        Task<string> SaveOriginalAsync(Stream fileStream, string fileName);
        Task<string> SaveThumbnailAsync(Stream thumbnailStream, string fileName);
        Task<bool> DeleteAsync(string path);
        /// <summary>
        /// Retorna una URI pública o ruta adecuada para uso en Image Source
        /// </summary>
        Task<string> GetUriAsync(string path);
    }
}