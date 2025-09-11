using System;
using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.GestionMantenimientos.Models.Entities
{
    public class MantenimientoPlantillaTarea
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la tarea es obligatorio.")]
        public string Nombre { get; set; } = null!;

        public string? Descripcion { get; set; }

        public int Orden { get; set; }

        public bool Predeterminada { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
