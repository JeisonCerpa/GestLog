namespace GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

/// <summary>
/// Estados posibles para mantenimientos correctivos (reactivos).
/// </summary>
public enum EstadoMantenimientoCorrectivo
{
    /// <summary>Falla reportada, esperando asignación a proveedor.</summary>
    Pendiente = 0,

    /// <summary>Equipo en reparación con proveedor tercero.</summary>
    EnReparacion = 1,

    /// <summary>Reparación completada exitosamente.</summary>
    Completado = 2,

    /// <summary>Reparación cancelada (equipo dado de baja).</summary>
    Cancelado = 3
}
