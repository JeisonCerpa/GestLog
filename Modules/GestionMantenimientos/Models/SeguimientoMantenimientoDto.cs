using System;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models
{
    public class SeguimientoMantenimientoDto
    {
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public DateTime? Fecha { get; set; }
        public TipoMantenimiento? TipoMtno { get; set; }
        public string? Descripcion { get; set; }
        public string? Responsable { get; set; }
        public decimal? Costo { get; set; }
        public string? Observaciones { get; set; }
        public DateTime? FechaRegistro { get; set; }

        public SeguimientoMantenimientoDto() { }

        public SeguimientoMantenimientoDto(SeguimientoMantenimientoDto other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            Codigo = other.Codigo;
            Nombre = other.Nombre;
            Fecha = other.Fecha;
            TipoMtno = other.TipoMtno;
            Descripcion = other.Descripcion;
            Responsable = other.Responsable;
            Costo = other.Costo;
            Observaciones = other.Observaciones;
            FechaRegistro = other.FechaRegistro;
        }
    }
}
