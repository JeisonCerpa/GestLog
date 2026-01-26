using System;
using System.ComponentModel.DataAnnotations;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models.Entities
{
    public class SeguimientoMantenimiento
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El código del equipo es obligatorio.")]
        public string Codigo { get; set; } = null!;
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        public string Nombre { get; set; } = null!;
        [Required(ErrorMessage = "El tipo de mantenimiento es obligatorio.")]
        public TipoMantenimiento TipoMtno { get; set; }        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres.")]
        public string? Descripcion { get; set; }
        [Required(ErrorMessage = "El responsable es obligatorio.")]
        public string? Responsable { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "El costo no puede ser negativo.")]        public decimal? Costo { get; set; }
        [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
        public string? Observaciones { get; set; }
        [Required(ErrorMessage = "La fecha de registro es obligatoria.")]
        public DateTime? FechaRegistro { get; set; } // Usar solo esta como fecha oficial de realización y registro
        [Range(1, 53, ErrorMessage = "La semana debe estar entre 1 y 53.")]
        public int Semana { get; set; } // Semana del año (1-53)
        [Range(2000, 2100, ErrorMessage = "El año debe ser válido.")]
        public int Anio { get; set; } // Año del seguimiento
        public EstadoSeguimientoMantenimiento Estado { get; set; } // Estado calculado del seguimiento
        public DateTime? FechaRealizacion { get; set; } // Fecha real de ejecución del mantenimiento
        public FrecuenciaMantenimiento? Frecuencia { get; set; } // NUEVO: para correctivo/predictivo
    }
}
