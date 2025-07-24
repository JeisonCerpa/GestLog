using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para la gestión de cargos.
    /// </summary>
    public interface ICargoService
    {
        Task<Cargo> CrearCargoAsync(Cargo cargo);
        Task<Cargo> EditarCargoAsync(Cargo cargo);
        Task EliminarCargoAsync(Guid idCargo);
        Task<Cargo> ObtenerCargoPorIdAsync(Guid idCargo);
        Task<IEnumerable<Cargo>> ObtenerTodosAsync();
        Task<bool> ExisteNombreAsync(string nombre);
    }
}
