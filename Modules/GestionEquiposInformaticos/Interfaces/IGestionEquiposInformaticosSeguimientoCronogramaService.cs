using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces
{
    /// <summary>
    /// Servicio del módulo GestionEquiposInformaticos responsable de: 
    /// - desactivar (soft-disable) planes cronograma asociados a un equipo (Activa = false)
    /// - eliminar seguimientos futuros pendientes relacionados con el equipo
    /// Implementación localizada en el módulo para no tocar la lógica de GestionMantenimientos.
    /// </summary>
    public interface IGestionEquiposInformaticosSeguimientoCronogramaService
    {
        /// <summary>
        /// Desactiva planes y elimina seguimientos PENDIENTES/FUTUROS para un equipo.
        /// Operación idempotente y asíncrona.
        /// </summary>
        Task DeletePendientesFuturasByEquipoCodigoAsync(string codigoEquipo, CancellationToken cancellationToken = default);
    }
}
