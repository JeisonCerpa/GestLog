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
        Task<IEnumerable<Auditoria>> ObtenerHistorialPorEntidadAsync(string entidadAfectada, Guid idEntidad);
        Task<IEnumerable<Auditoria>> ObtenerHistorialPorUsuarioAsync(string usuarioResponsable);
    }
}
