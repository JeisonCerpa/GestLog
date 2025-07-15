using System;
using System.ComponentModel.DataAnnotations;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models.Entities
{
    public class Equipo
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El código del equipo es obligatorio.")]
        public string Codigo { get; set; } = null!;
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        public string Nombre { get; set; } = null!;
        public string? Marca { get; set; }
        public EstadoEquipo Estado { get; set; }
        public Sede? Sede { get; set; }
        [Required(ErrorMessage = "La fecha de registro es obligatoria.")]
        public DateTime? FechaRegistro { get; set; } // Usar como fecha de alta y referencia
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo.")]
        public decimal? Precio { get; set; }
        public string? Observaciones { get; set; }
        public FrecuenciaMantenimiento? FrecuenciaMtto { get; set; }
        public DateTime? FechaBaja { get; set; }
        // SemanaInicioMtto eliminado: se calcula a partir de FechaRegistro
    }
}
