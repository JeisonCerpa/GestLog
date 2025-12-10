using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionMantenimientos.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de cachÃ© de equipos.
    /// </summary>
    public interface IEquipoCacheService
    {
        /// <summary>
        /// Obtiene todos los equipos del cachÃ© o de la base de datos si el cachÃ© estÃ¡ expirado.
        /// </summary>
        Task<IEnumerable<EquipoDto>> GetEquiposAsync(bool forzarRecarga = false);

        /// <summary>
        /// Obtiene un equipo especÃ­fico por su cÃ³digo del cachÃ© de forma asÃ­ncrona.
        /// </summary>
        Task<EquipoDto?> GetEquipoPorCodigoAsync(string codigo);

        /// <summary>
        /// Invalida el cachÃ© para forzar una recarga desde la base de datos.
        /// </summary>
        void InvalidarCache();
    }
}

