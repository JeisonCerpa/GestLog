using System;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models
{
    public class EquipoDto
    {
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Marca { get; set; }
        public EstadoEquipo? Estado { get; set; }
        public Sede? Sede { get; set; }
        public FrecuenciaMantenimiento? FrecuenciaMtto { get; set; }
        public DateTime? FechaCompra { get; set; }
        public decimal? Precio { get; set; }
        public string? Observaciones { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public DateTime? FechaBaja { get; set; }
        public int? SemanaInicioMtto { get; set; }
    }
}
