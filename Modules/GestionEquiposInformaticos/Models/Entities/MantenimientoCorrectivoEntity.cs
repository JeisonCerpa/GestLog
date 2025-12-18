using System;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Entities;

/// <summary>
/// Entidad para mantenimientos correctivos (reactivos) de equipos informáticos y periféricos.
/// Representa fallos no programados que requieren reparación inmediata.
/// Polimórfica: puede asociarse a Equipos O Periféricos, no ambos.
/// </summary>
public class MantenimientoCorrectivoEntity
{
    /// <summary>ID único del mantenimiento correctivo.</summary>
    public int Id { get; set; }    /// <summary>Tipo de entidad reparada: "Equipo" o "Periferico".</summary>
    public string TipoEntidad { get; set; } = "Equipo"; // Equipo | Periferico

    /// <summary>Código del equipo o periférico (unificado, diferenciado por TipoEntidad).</summary>
    public string? Codigo { get; set; }

    /// <summary>Fecha y hora en que se reportó la falla.</summary>
    public DateTime FechaFalla { get; set; }

    /// <summary>Descripción detallada del problema reportado.</summary>
    public string DescripcionFalla { get; set; } = string.Empty;

    /// <summary>Nombre o razón social del proveedor tercero asignado (empresa de reparación).</summary>
    public string? ProveedorAsignado { get; set; }

    /// <summary>Estado actual del mantenimiento correctivo.</summary>
    public EstadoMantenimientoCorrectivo Estado { get; set; } = EstadoMantenimientoCorrectivo.Pendiente;

    /// <summary>Fecha de inicio de la reparación (cuando el proveedor comienza el trabajo).</summary>
    public DateTime? FechaInicio { get; set; }

    /// <summary>Fecha de finalización de la reparación.</summary>
    public DateTime? FechaCompletado { get; set; }    /// <summary>Observaciones adicionales (diagnóstico, trabajo realizado, etc.).</summary>
    public string? Observaciones { get; set; }

    /// <summary>Costo total de la reparación realizada por el proveedor (en COP).</summary>
    public decimal? CostoReparacion { get; set; }

    /// <summary>Fecha y hora en que se registró la falla en el sistema.</summary>
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de última actualización del registro.</summary>
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}
