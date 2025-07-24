using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para acceso a datos de cargos.
    /// </summary>
    public interface ICargoRepository
    {
        Task<Cargo> AgregarAsync(Cargo cargo);
        Task<Cargo> ActualizarAsync(Cargo cargo);
        Task EliminarAsync(Guid idCargo);
        Task<Cargo?> ObtenerPorIdAsync(Guid idCargo);
        Task<IEnumerable<Cargo>> ObtenerTodosAsync();
        Task<bool> ExisteNombreAsync(string nombre);
    }
}
