using System;
using System.ComponentModel.DataAnnotations;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models
{
    public class SeguimientoMantenimientoDto
    {
        [Required(ErrorMessage = "El código del equipo es obligatorio.")]
        public string? Codigo { get; set; }
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        public string? Nombre { get; set; }
        [Required(ErrorMessage = "La fecha de registro es obligatoria.")]
        public DateTime? FechaRegistro { get; set; } // Usar solo esta como fecha oficial de realización y registro
        [Required(ErrorMessage = "El tipo de mantenimiento es obligatorio.")]
        public TipoMantenimiento? TipoMtno { get; set; }
        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(200, ErrorMessage = "La descripción no puede superar los 200 caracteres.")]
        public string? Descripcion { get; set; }
        [Required(ErrorMessage = "El responsable es obligatorio.")]
        public string? Responsable { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "El costo no puede ser negativo.")]
        public decimal? Costo { get; set; }
        public string? Observaciones { get; set; }
        public EstadoSeguimientoMantenimiento Estado { get; set; }
        [Range(1, 53, ErrorMessage = "La semana debe estar entre 1 y 53.")]
        public int Semana { get; set; } // Semana del año (1-53)
        [Range(2000, 2100, ErrorMessage = "El año debe ser válido.")]
        public int Anio { get; set; }   // Año del seguimiento
        public DateTime? FechaRealizacion { get; set; } // Fecha real de ejecución del mantenimiento
        public FrecuenciaMantenimiento? Frecuencia { get; set; } // NUEVO: para correctivo/predictivo

        // Propiedades auxiliares para la UI (no persistentes)
        public bool IsCodigoReadOnly { get; set; } = false;
        public bool IsCodigoEnabled { get; set; } = true;

        public SeguimientoMantenimientoDto() { }

        public SeguimientoMantenimientoDto(SeguimientoMantenimientoDto other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            Codigo = other.Codigo;
            Nombre = other.Nombre;
            FechaRegistro = other.FechaRegistro;
            TipoMtno = other.TipoMtno;
            Descripcion = other.Descripcion;
            Responsable = other.Responsable;
            Costo = other.Costo;
            Observaciones = other.Observaciones;
            Estado = other.Estado;
            Semana = other.Semana;
            Anio = other.Anio;
            FechaRealizacion = other.FechaRealizacion;
            Frecuencia = other.Frecuencia;
            IsCodigoReadOnly = true;
            IsCodigoEnabled = false;
        }
    }
}
