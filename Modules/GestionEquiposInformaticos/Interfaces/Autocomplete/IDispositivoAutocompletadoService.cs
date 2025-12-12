using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces.Autocomplete
{
    /// <summary>
    /// Interfaz para servicio de autocompletado de dispositivos
    /// </summary>
    public interface IDispositivoAutocompletadoService
    {
        /// <summary>
        /// Obtiene todos los dispositivos únicos ordenados por frecuencia de uso
        /// </summary>
        Task<List<string>> ObtenerTodosAsync();

        /// <summary>
        /// Obtiene dispositivos que coinciden con el filtro especificado
        /// </summary>
        Task<List<string>> BuscarAsync(string filtro);
    }
}
