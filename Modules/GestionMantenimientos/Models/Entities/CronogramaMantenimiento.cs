using System;

namespace GestLog.Modules.GestionMantenimientos.Models.Entities
{
    public class CronogramaMantenimiento
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Marca { get; set; }
        public string? Sede { get; set; }
        public int? SemanaInicioMtto { get; set; }
        public int? FrecuenciaMtto { get; set; }
        // 52 semanas: S1...S52
        public bool[] Semanas { get; set; } = new bool[52];
    }
}
