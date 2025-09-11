using System;
using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Entities
{
    /// <summary>
    /// Ejecución (o estado) de un mantenimiento semanal para un plan y una semana ISO.
    /// Solo se crea registro cuando se completa o se marca no realizada (o si deseas materializar pendientes).
    /// </summary>
    public class EjecucionSemanal
    {
        [Key]
        public Guid EjecucionId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PlanId { get; set; }

        public short AnioISO { get; set; }
        public byte SemanaISO { get; set; }

        /// <summary>
        /// Fecha objetivo (el día programado de esa semana)
        /// </summary>
        public DateTime FechaObjetivo { get; set; }

        /// <summary>
        /// Fecha y hora real de ejecución.
        /// </summary>
        public DateTime? FechaEjecucion { get; set; }

        /// <summary>
        /// 1 Pendiente, 2 Completada, 3 NoRealizada
        /// </summary>
        public byte Estado { get; set; }

        [MaxLength(100)]
        public string? UsuarioEjecuta { get; set; }

        /// <summary>
        /// Resultado del checklist con estados por item, observaciones puntuales, etc.
        /// </summary>
        public string? ResultadoJson { get; set; }

        // Navegación
        public virtual PlanCronogramaEquipo? Plan { get; set; }
    }
}
