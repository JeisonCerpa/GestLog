using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces.Autocomplete
{
    /// <summary>
    /// Interfaz para servicio de autocompletado de marcas
    /// </summary>
    public interface IMarcaAutocompletadoService
    {
        /// <summary>
        /// Obtiene todas las marcas únicas ordenadas por frecuencia de uso
        /// </summary>
        Task<List<string>> ObtenerTodosAsync();

        /// <summary>
        /// Obtiene marcas que coinciden con el filtro especificado
        /// </summary>
        Task<List<string>> BuscarAsync(string filtro);
    }
}
