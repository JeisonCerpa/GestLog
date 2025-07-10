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
        // 52 semanas: S1...S52
        public bool[] Semanas { get; set; } = new bool[52];
        public int Anio { get; set; } // Año del cronograma
    }
}
