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
    public int Id { get; set; }

    /// <summary>Tipo de entidad reparada: "Equipo" o "Periferico".</summary>
    public string TipoEntidad { get; set; } = "Equipo"; // Equipo | Periferico

    /// <summary>FK: Referencia al equipo que requiere reparación (nullable si es periférico).</summary>
    public int? EquipoInformaticoId { get; set; }

    /// <summary>Navegación: Equipo asociado.</summary>
    public EquipoInformaticoEntity? EquipoInformatico { get; set; }

    /// <summary>FK: Referencia al periférico que requiere reparación (nullable si es equipo).</summary>
    public int? PerifericoEquipoInformaticoId { get; set; }

    /// <summary>Navegación: Periférico asociado.</summary>
    public PerifericoEquipoInformaticoEntity? PerifericoEquipoInformatico { get; set; }

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
    public DateTime? FechaCompletado { get; set; }

    /// <summary>Observaciones adicionales (diagnóstico, trabajo realizado, etc.).</summary>
    public string? Observaciones { get; set; }

    /// <summary>Costo total de la reparación realizada por el proveedor (en COP).</summary>
    public decimal? CostoReparacion { get; set; }

    /// <summary>Indica si la entidad fue dado de baja debido a una reparación imposible.</summary>
    public bool DadoDeBaja { get; set; }

    /// <summary>FK: Usuario que reportó la falla.</summary>
    public int? UsuarioRegistroId { get; set; }

    /// <summary>Fecha y hora en que se registró la falla en el sistema.</summary>
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de creación del registro.</summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de última actualización del registro.</summary>
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}
