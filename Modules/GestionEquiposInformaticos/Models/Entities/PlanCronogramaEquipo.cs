using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Entities
{
    /// <summary>
    /// Plan semanal de mantenimiento para un equipo informático.
    /// Día programado: 1=Lunes .. 7=Domingo. La primera semana efectiva es la siguiente a FechaCreacion.
    /// </summary>
    public class PlanCronogramaEquipo
    {
        [Key]
        public Guid PlanId { get; set; } = Guid.NewGuid();        [Required]
        [MaxLength(20)]
        public string CodigoEquipo { get; set; } = string.Empty; // FK -> EquipoInformaticoEntity.Codigo

        [Required]
        [MaxLength(200)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Responsable { get; set; } = string.Empty;

        /// <summary>
        /// Día programado (1=Lunes .. 7=Domingo)
        /// </summary>
        [Range(1,7)]
        public byte DiaProgramado { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public bool Activo { get; set; } = true;

        /// <summary>
        /// Checklist estructurado en JSON (descripcion, items, etc.)
        /// </summary>
        public string? ChecklistJson { get; set; }

        // Navegación
        public virtual ICollection<EjecucionSemanal> Ejecuciones { get; set; } = new List<EjecucionSemanal>();
        public virtual EquipoInformaticoEntity? Equipo { get; set; }
    }
}
