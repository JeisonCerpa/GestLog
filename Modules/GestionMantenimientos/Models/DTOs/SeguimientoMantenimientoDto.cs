using System;
using System.ComponentModel.DataAnnotations;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models.DTOs
{public class SeguimientoMantenimientoDto
    {
        [Required(ErrorMessage = "El código del equipo es obligatorio.")]
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        [Required(ErrorMessage = "La fecha de registro es obligatoria.")]
        public DateTime? FechaRegistro { get; set; } // Usar solo esta como fecha oficial de realización y registro
        [Required(ErrorMessage = "El tipo de mantenimiento es obligatorio.")]
        public TipoMantenimiento? TipoMtno { get; set; }
        public string? Descripcion { get; set; }
        public string? Responsable { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "El costo no puede ser negativo.")]
        public decimal? Costo { get; set; }
        public string? Observaciones { get; set; }
        public EstadoSeguimientoMantenimiento Estado { get; set; }
        [Range(1, 53, ErrorMessage = "La semana debe estar entre 1 y 53.")]
        public int Semana { get; set; } // Semana del año (1-53)
        [Range(2000, 2100, ErrorMessage = "El año debe ser válido.")]
        public int Anio { get; set; }   // Año del seguimiento
        public DateTime? FechaRealizacion { get; set; } // Fecha real de ejecución del mantenimiento
        public FrecuenciaMantenimiento? Frecuencia { get; set; } // NUEVO: para correctivo/predictivo

        // Propiedades auxiliares para la UI (no persistentes)
        public bool IsCodigoReadOnly { get; set; } = false;
        public bool IsCodigoEnabled { get; set; } = true;

        // --- Cache de campos normalizados para filtrado eficiente ---
        private string? _codigoNorm;
        private string? _nombreNorm;
        private string? _tipoMtnoNorm;
        private string? _responsableNorm;
        private string? _estadoNorm;
        private string? _fechaRegistroNorm;
        private string? _semanaNorm;
        private string? _anioNorm;

        public void RefrescarCacheFiltro()
        {
            _codigoNorm = Normalizar(Codigo);
            _nombreNorm = Normalizar(Nombre);
            _tipoMtnoNorm = Normalizar(TipoMtno?.ToString());
            _responsableNorm = Normalizar(Responsable);
            _estadoNorm = Normalizar(Estado.ToString());
            _fechaRegistroNorm = FechaRegistro?.ToString("dd/MM/yyyy") ?? string.Empty;
            _semanaNorm = Semana.ToString();
            _anioNorm = Anio.ToString();
        }
        
        /// <summary>
        /// Propiedad calculada que retorna el texto de la semana programada en formato legible
        /// </summary>
        public string SemanaTexto => $"Semana {Semana}, año {Anio}";

        public string CodigoNorm => _codigoNorm ?? string.Empty;
        public string NombreNorm => _nombreNorm ?? string.Empty;
        public string TipoMtnoNorm => _tipoMtnoNorm ?? string.Empty;
        public string ResponsableNorm => _responsableNorm ?? string.Empty;
        public string EstadoNorm => _estadoNorm ?? string.Empty;
        public string FechaRegistroNorm => _fechaRegistroNorm ?? string.Empty;
        public string SemanaNorm => _semanaNorm ?? string.Empty;
        public string AnioNorm => _anioNorm ?? string.Empty;

        private static string Normalizar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
            string s = valor.ToLowerInvariant().Replace(" ", "");
            s = s.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ü", "u")
                 .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U").Replace("Ü", "U")
                 .Replace("ñ", "n").Replace("Ñ", "N");
            return s;
        }

        public SeguimientoMantenimientoDto() { }

        public SeguimientoMantenimientoDto(SeguimientoMantenimientoDto other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            Codigo = other.Codigo;
            Nombre = other.Nombre;
            FechaRegistro = other.FechaRegistro;
            TipoMtno = other.TipoMtno;
            Descripcion = other.Descripcion;
            Responsable = other.Responsable;
            Costo = other.Costo;
            Observaciones = other.Observaciones;
            Estado = other.Estado;
            Semana = other.Semana;
            Anio = other.Anio;
            FechaRealizacion = other.FechaRealizacion;
            Frecuencia = other.Frecuencia;
            IsCodigoReadOnly = true;
            IsCodigoEnabled = false;
        }
    }
}
