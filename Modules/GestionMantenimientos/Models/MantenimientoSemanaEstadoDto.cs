using System;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models
{
    public class MantenimientoSemanaEstadoDto
    {
        public string CodigoEquipo { get; set; } = "";
        public string NombreEquipo { get; set; } = "";
        public int Semana { get; set; }
        public int Anio { get; set; }
        public FrecuenciaMantenimiento? Frecuencia { get; set; }
        public bool Programado { get; set; }
        public bool Realizado { get; set; }
        public bool Atrasado { get; set; }
        public SeguimientoMantenimientoDto? Seguimiento { get; set; }
        public EstadoSeguimientoMantenimiento Estado { get; set; }
    }
}
