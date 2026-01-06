using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Entities
{
    /// <summary>
    /// Ejecución (o estado) de un mantenimiento semanal para un equipo.
    /// ✅ DESACOPLADA de PlanCronogramaEquipo - persiste aunque el plan se elimine.
    /// ✅ Almacena snapshot de datos del plan para trazabilidad histórica.
    /// Solo se crea registro cuando se completa o se marca no realizada.
    /// </summary>
    public class EjecucionSemanal
    {
        [Key]
        public Guid EjecucionId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// ✅ NUEVO: Referencia al equipo (no al plan) - REQUERIDA
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string CodigoEquipo { get; set; } = string.Empty;

        /// <summary>
        /// ⚠️ OPCIONAL: Referencia al plan (para recuperar metadatos si existe)
        /// Puede ser NULL si el plan fue eliminado - permite historial independiente
        /// </summary>
        public Guid? PlanId { get; set; }

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

        /// <summary>
        /// ✅ SNAPSHOT: Guardar descripción del plan al momento de ejecución (para historial)
        /// Permite consultar qué se ejecutaba aunque el plan se haya eliminado
        /// </summary>
        [MaxLength(200)]
        public string? DescripcionPlanSnapshot { get; set; }

        /// <summary>
        /// ✅ SNAPSHOT: Guardar responsable del plan al momento de ejecución
        /// </summary>
        [MaxLength(100)]
        public string? ResponsablePlanSnapshot { get; set; }

        // Navegación: OPCIONAL (puede ser NULL)
        [ForeignKey(nameof(PlanId))]
        public virtual PlanCronogramaEquipo? Plan { get; set; }

        // ✅ NUEVO: Navegación al equipo (para consultas sin depender del plan)
        [ForeignKey(nameof(CodigoEquipo))]
        public virtual EquipoInformaticoEntity? Equipo { get; set; }
    }
}
