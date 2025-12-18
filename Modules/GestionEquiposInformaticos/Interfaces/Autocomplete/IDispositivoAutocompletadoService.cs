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

        /// <summary>
        /// Obtiene pares (Codigo, Dispositivo) para los periféricos que coinciden con el filtro.
        /// Útil cuando se necesita mostrar tanto el código como el nombre del periférico.
        /// </summary>
        Task<List<(string Code, string Dispositivo)>> BuscarConCodigoAsync(string filtro);
    }
}
