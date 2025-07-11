using System;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models.Entities
{
    public class SeguimientoMantenimiento
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public TipoMantenimiento TipoMtno { get; set; }
        public string? Descripcion { get; set; }
        public string? Responsable { get; set; }
        public decimal? Costo { get; set; }
        public string? Observaciones { get; set; }
        public DateTime? FechaRegistro { get; set; } // Usar solo esta como fecha oficial de realización y registro
        public int Semana { get; set; } // Semana del año (1-53)
        public int Anio { get; set; } // Año del seguimiento
        public EstadoSeguimientoMantenimiento Estado { get; set; } // Estado calculado del seguimiento
    }
}
