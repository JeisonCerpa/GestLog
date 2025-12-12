using GestLog.Modules.GestionMantenimientos.Models.DTOs;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces.Dialog
{
    /// <summary>
    /// Servicio de presentación para registrar la ejecución de un mantenimiento de equipo
    /// (independiente del diálogo de Seguimiento de Gestión de Mantenimientos).
    /// </summary>
    public interface IRegistroMantenimientoEquipoDialogService
    {
        /// <summary>
        /// Muestra el diálogo de registro y devuelve true si el usuario confirmó los datos.
        /// </summary>
        /// <param name="seguimientoBase">DTO base con datos prellenados.</param>
        /// <param name="resultado">Resultado final listo para persistir (o null si cancelado).</param>
        /// <returns>True si se confirmó el registro.</returns>
        bool TryShowRegistroDialog(SeguimientoMantenimientoDto seguimientoBase, out SeguimientoMantenimientoDto? resultado);
    }
}
