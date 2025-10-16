using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using System.Runtime.CompilerServices;

namespace GestLog.Modules.GestionMantenimientos.Models
{
    public class EquipoDto : INotifyPropertyChanged, IDataErrorInfo
    {
        [Required(ErrorMessage = "El código del equipo es obligatorio.")]
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Marca { get; set; }
        public EstadoEquipo? Estado { get; set; }
        public Sede? Sede { get; set; }
        public FrecuenciaMantenimiento? FrecuenciaMtto { get; set; }
        public DateTime? FechaRegistro { get; set; } // Usar como fecha de alta y referencia
        public DateTime? FechaCompra { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo.")]
        public decimal? Precio { get; set; }
        public string? Observaciones { get; set; }
        public DateTime? FechaBaja { get; set; }
        // Nuevas propiedades
        public string? Clasificacion { get; set; }
        public string? CompradoA { get; set; }
        // SemanaInicioMtto eliminado: se calcula a partir de FechaRegistro

        // Propiedades auxiliares para la UI (no persistentes)
        private bool _isCodigoReadOnly = false;
        public bool IsCodigoReadOnly
        {
            get => _isCodigoReadOnly;
            set
            {
                if (_isCodigoReadOnly != value)
                {
                    _isCodigoReadOnly = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isCodigoEnabled = true;
        public bool IsCodigoEnabled
        {
            get => _isCodigoEnabled;
            set
            {
                if (_isCodigoEnabled != value)
                {
                    _isCodigoEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

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
            Clasificacion = other.Clasificacion;
            CompradoA = other.CompradoA;
            FechaRegistro = other.FechaRegistro;
            FrecuenciaMtto = other.FrecuenciaMtto;
            FechaBaja = other.FechaBaja;
            FechaCompra = other.FechaCompra;
            // SemanaInicioMtto eliminado
            IsCodigoReadOnly = true;
            IsCodigoEnabled = false;
        }

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                // Validación de precio no negativo
                if (columnName == nameof(Precio))
                {
                    if (Precio != null && Precio < 0)
                        return "El precio no puede ser negativo.";
                }

                // Fecha de compra no puede ser en el futuro
                if (columnName == nameof(FechaCompra))
                {
                    if (FechaCompra != null && FechaCompra.Value.Date > DateTime.Today)
                        return "La fecha de compra no puede ser futura.";
                }

                return string.Empty;
            }
        }
        public string Error => string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
