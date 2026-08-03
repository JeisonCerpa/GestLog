using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para la gestión de auditoría.
    /// </summary>
    public interface IAuditoriaService
    {
        Task RegistrarEventoAsync(Auditoria auditoria);

        /// <summary>
        /// Registra un cambio sobre una entidad identificada por clave natural (ej. código de equipo),
        /// tomando automáticamente el usuario autenticado y la fecha actual.
        /// </summary>
        Task RegistrarCambioAsync(string entidadAfectada, string claveEntidad, string accion, string detalle);

        Task<IEnumerable<Auditoria>> ObtenerHistorialPorEntidadAsync(string entidadAfectada, Guid idEntidad);
        Task<IEnumerable<Auditoria>> ObtenerHistorialPorClaveAsync(string entidadAfectada, string claveEntidad);
        Task<IEnumerable<Auditoria>> ObtenerHistorialPorUsuarioAsync(string usuarioResponsable);

        /// <summary>
        /// Búsqueda con filtros combinables; cualquier parámetro nulo o vacío no filtra.
        /// </summary>
        Task<IEnumerable<Auditoria>> BuscarAsync(string? entidadAfectada, string? usuarioResponsable,
            DateTime? desde, DateTime? hasta, string? texto, int maxResultados = 500);

        /// <summary>Entidades presentes en el historial, para poblar filtros.</summary>
        Task<IEnumerable<string>> ObtenerEntidadesAsync();
    }
}
