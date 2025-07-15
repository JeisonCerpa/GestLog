using System;
using System.ComponentModel.DataAnnotations;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models
{
    public class EquipoDto
    {
        [Required(ErrorMessage = "El código del equipo es obligatorio.")]
        public string? Codigo { get; set; }
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        public string? Nombre { get; set; }
        public string? Marca { get; set; }
        public EstadoEquipo? Estado { get; set; }
        public Sede? Sede { get; set; }
        public FrecuenciaMantenimiento? FrecuenciaMtto { get; set; }
        [Required(ErrorMessage = "La fecha de registro es obligatoria.")]
        public DateTime? FechaRegistro { get; set; } // Usar como fecha de alta y referencia
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo.")]
        public decimal? Precio { get; set; }
        public string? Observaciones { get; set; }
        public DateTime? FechaBaja { get; set; }
        // SemanaInicioMtto eliminado: se calcula a partir de FechaRegistro

        // Propiedades auxiliares para la UI (no persistentes)
        public bool IsCodigoReadOnly { get; set; } = false;
        public bool IsCodigoEnabled { get; set; } = true;

        public EquipoDto()
        {
        }

        public EquipoDto(EquipoDto other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            Codigo = other.Codigo;
            Nombre = other.Nombre;
            Marca = other.Marca;
            Estado = other.Estado;
            Sede = other.Sede;
            Precio = other.Precio;
            Observaciones = other.Observaciones;
            FechaRegistro = other.FechaRegistro;
            FrecuenciaMtto = other.FrecuenciaMtto;
            FechaBaja = other.FechaBaja;
            // SemanaInicioMtto eliminado
            IsCodigoReadOnly = true;
            IsCodigoEnabled = false;
        }
    }
}
