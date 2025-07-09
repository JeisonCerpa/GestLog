using System;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models.Entities
{
    public class Equipo
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Marca { get; set; }
        public EstadoEquipo Estado { get; set; }
        public Sede? Sede { get; set; }
        public DateTime? FechaRegistro { get; set; } // Usar como fecha de alta y referencia
        public decimal? Precio { get; set; }
        public string? Observaciones { get; set; }
        public FrecuenciaMantenimiento? FrecuenciaMtto { get; set; }
        public DateTime? FechaBaja { get; set; }
        // SemanaInicioMtto eliminado: se calcula a partir de FechaRegistro
    }
}
