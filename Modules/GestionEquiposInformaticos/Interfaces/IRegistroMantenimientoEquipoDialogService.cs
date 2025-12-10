namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces;

using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;

/// <summary>
/// Servicio de presentaciÃ³n para registrar la ejecuciÃ³n de un mantenimiento de equipo
/// (independiente del diÃ¡logo de Seguimiento de GestiÃ³n de Mantenimientos).
/// </summary>
public interface IRegistroMantenimientoEquipoDialogService
{
    /// <summary>
    /// Muestra el diÃ¡logo de registro y devuelve true si el usuario confirmÃ³ los datos.
    /// </summary>
    /// <param name="seguimientoBase">DTO base con datos prellenados.</param>
    /// <param name="resultado">Resultado final listo para persistir (o null si cancelado).</param>
    /// <returns>True si se confirmÃ³ el registro.</returns>
    bool TryShowRegistroDialog(SeguimientoMantenimientoDto seguimientoBase, out SeguimientoMantenimientoDto? resultado);
}
