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
        Task<IEnumerable<Auditoria>> ObtenerPorUsuarioAsync(string usuarioResponsable);
    }
}
