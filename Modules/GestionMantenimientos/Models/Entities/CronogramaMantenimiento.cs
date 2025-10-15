using System;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models.Entities
{
    public class CronogramaMantenimiento
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        // NOTA: El campo Codigo debe ser único e inmutable. No permitir edición ni duplicados.
        public string Nombre { get; set; } = null!;
        public string? Marca { get; set; }
        public string? Sede { get; set; }
        public FrecuenciaMantenimiento? FrecuenciaMtto { get; set; }
        // Semanas: S1...SN (N = 52 o 53 según el año ISO). La longitud se determina por ISOWeek.GetWeeksInYear(anio).
        public bool[] Semanas { get; set; } = Array.Empty<bool>();
        public int Anio { get; set; } // Año del cronograma
    }
}
