using GestLog.Modules.GestionMantenimientos.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionMantenimientos.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de caché de equipos.
    /// </summary>
    public interface IEquipoCacheService
    {
        /// <summary>
        /// Obtiene todos los equipos del caché o de la base de datos si el caché está expirado.
        /// </summary>
        Task<IEnumerable<EquipoDto>> GetEquiposAsync(bool forzarRecarga = false);

        /// <summary>
        /// Obtiene un equipo específico por su código del caché de forma asíncrona.
        /// </summary>
        Task<EquipoDto?> GetEquipoPorCodigoAsync(string codigo);

        /// <summary>
        /// Invalida el caché para forzar una recarga desde la base de datos.
        /// </summary>
        void InvalidarCache();
    }
}
