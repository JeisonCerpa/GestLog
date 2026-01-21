using System;
using System.ComponentModel.DataAnnotations;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models.DTOs
{
    public class CronogramaMantenimientoDto
    {
        [Required(ErrorMessage = "El código del equipo es obligatorio.")]
        public string? Codigo { get; set; }
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        public string? Nombre { get; set; }
        public string? Marca { get; set; }
        public string? Sede { get; set; }
        public int? SemanaInicioMtto { get; set; }
        public FrecuenciaMantenimiento? FrecuenciaMtto { get; set; }
        [Required(ErrorMessage = "Las semanas del cronograma son obligatorias.")]
        // S1...SN: Representación semanal del cronograma. El número de semanas (N) puede ser 52 o 53 según el año ISO.
        public bool[] Semanas { get; set; } = Array.Empty<bool>();

        // Propiedades auxiliares para la UI (no persistentes)
        public bool IsCodigoReadOnly { get; set; } = false;
        public bool IsCodigoEnabled { get; set; } = true;
        public int Anio { get; set; } // Año del cronograma
        // Indica si este item representa un plan semanal (no un cronograma tradicional)
        public bool EsPlanSemanal { get; set; } = false; // NUEVO
        // Indica si el plan semanal ya fue ejecutado en la semana seleccionada
        public bool PlanEjecutadoSemana { get; set; } = false; // NUEVO
        // NUEVO: derivado para color atrasado
        public bool EsAtrasadoSemana { get; set; } = false;
        // NUEVO: derivado para color no realizado (Estado = 3)
        public bool EsNoRealizadoSemana { get; set; } = false;

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
            IsCodigoReadOnly = true;
            IsCodigoEnabled = false;
            EsPlanSemanal = other.EsPlanSemanal; // copiar
            PlanEjecutadoSemana = other.PlanEjecutadoSemana; // copiar
            EsAtrasadoSemana = other.EsAtrasadoSemana; // copiar
            EsNoRealizadoSemana = other.EsNoRealizadoSemana; // copiar
        }
    }
}
