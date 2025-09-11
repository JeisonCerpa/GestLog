using System;
using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.GestionMantenimientos.Models.Entities
{
    public class SeguimientoMantenimientoTarea
    {
        public int Id { get; set; }
        public int SeguimientoMantenimientoId { get; set; }
        // FK navigation set by existing SeguimientoMantenimiento entity if needed

        public int? MantenimientoPlantillaTareaId { get; set; }

        [Required(ErrorMessage = "El nombre de la tarea realizada es obligatorio.")]
        public string NombreTarea { get; set; } = null!;

        public bool Completada { get; set; }

        public string? Observaciones { get; set; }

        public string? RepuestoUsado { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "La duración debe ser positiva.")]
        public int? DuracionMinutos { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
