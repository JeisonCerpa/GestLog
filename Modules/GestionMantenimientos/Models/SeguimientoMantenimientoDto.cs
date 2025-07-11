using System;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models
{
    public class SeguimientoMantenimientoDto
    {
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public DateTime? FechaRegistro { get; set; } // Usar solo esta como fecha oficial de realización y registro
        public TipoMantenimiento? TipoMtno { get; set; }
        public string? Descripcion { get; set; }
        public string? Responsable { get; set; }
        public decimal? Costo { get; set; }
        public string? Observaciones { get; set; }
        public EstadoSeguimientoMantenimiento Estado { get; set; }
        public int Semana { get; set; } // Semana del año (1-53)
        public int Anio { get; set; }   // Año del seguimiento
        public DateTime? FechaRealizacion { get; set; } // Fecha real de ejecución del mantenimiento

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
            IsCodigoReadOnly = true;
            IsCodigoEnabled = false;
        }
    }
}
