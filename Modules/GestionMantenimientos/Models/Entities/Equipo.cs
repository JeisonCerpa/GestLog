using System;
using System.ComponentModel.DataAnnotations;
using GestLog.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models.Entities
{
    public class Equipo
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El código del equipo es obligatorio.")]
        public string Codigo { get; set; } = null!;
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        public string? Nombre { get; set; }
        public string? Marca { get; set; }
        public EstadoEquipo Estado { get; set; }
        public Sede? Sede { get; set; }
        public DateTime? FechaRegistro { get; set; } // Usar como fecha de alta y referencia
        public DateTime? FechaCompra { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo.")]        public decimal? Precio { get; set; }
        [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
        public string? Observaciones { get; set; }
        // Nuevos campos: Clasificación y Comprado a
        public string? Clasificacion { get; set; }
        public string? CompradoA { get; set; }
        public FrecuenciaMantenimiento? FrecuenciaMtto { get; set; }
        /// <summary>
        /// Horas de uso entre servicios de la escalera de rutinas (4.000 en los compresores AXP).
        /// Null = el equipo no lleva mantenimiento por horómetro; es lo que marca cuáles sí.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Las horas por servicio deben ser mayores a cero.")]
        public int? HorasPorServicio { get; set; }
        /// <summary>
        /// Ruta o URL del documento original del equipo (manual, ficha técnica). Se abre con la
        /// aplicación asociada del sistema; el archivo vive donde ya está, no se copia a la base.
        /// </summary>
        [StringLength(500)]
        public string? RutaDocumento { get; set; }
        public DateTime? FechaBaja { get; set; }
        // SemanaInicioMtto eliminado: se calcula a partir de FechaRegistro
    }
}
