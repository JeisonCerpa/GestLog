using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para acceso a datos de auditoría.
    /// </summary>
    public interface IAuditoriaRepository
    {
        Task RegistrarAsync(Auditoria auditoria);
        Task<IEnumerable<Auditoria>> ObtenerPorEntidadAsync(string entidadAfectada, Guid idEntidad);
        Task<IEnumerable<Auditoria>> ObtenerPorClaveAsync(string entidadAfectada, string claveEntidad);
        Task<IEnumerable<Auditoria>> ObtenerPorUsuarioAsync(string usuarioResponsable);

        /// <summary>
        /// Búsqueda con filtros combinables; cualquier parámetro nulo o vacío no filtra.
        /// </summary>
        Task<IEnumerable<Auditoria>> BuscarAsync(string? entidadAfectada, string? usuarioResponsable,
            DateTime? desde, DateTime? hasta, string? texto, int maxResultados = 500);

        /// <summary>Entidades presentes en el historial, para poblar filtros.</summary>
        Task<IEnumerable<string>> ObtenerEntidadesAsync();
    }
}
