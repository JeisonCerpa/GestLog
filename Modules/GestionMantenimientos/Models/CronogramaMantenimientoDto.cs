using System;

namespace GestLog.Modules.GestionMantenimientos.Models
{
    public class CronogramaMantenimientoDto
    {
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Marca { get; set; }
        public string? Sede { get; set; }
        public int? SemanaInicioMtto { get; set; }
        public int? FrecuenciaMtto { get; set; }
        // S1...S52: Representación semanal del cronograma
        public bool[] Semanas { get; set; } = new bool[52];

        public CronogramaMantenimientoDto() { }

        public CronogramaMantenimientoDto(CronogramaMantenimientoDto other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            Codigo = other.Codigo;
            Nombre = other.Nombre;
            Marca = other.Marca;
            Sede = other.Sede;
            SemanaInicioMtto = other.SemanaInicioMtto;
            FrecuenciaMtto = other.FrecuenciaMtto;
            Semanas = (bool[])other.Semanas.Clone();
        }
    }
}
