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
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        public string? Nombre { get; set; }
        public string? Marca { get; set; }
        public EstadoEquipo? Estado { get; set; }
        public Sede? Sede { get; set; }
        public FrecuenciaMantenimiento? FrecuenciaMtto { get; set; }
        [Required(ErrorMessage = "La fecha de registro es obligatoria.")]
        public DateTime? FechaRegistro { get; set; } // Usar como fecha de alta y referencia
        [Required(ErrorMessage = "La fecha de compra es obligatoria.")]
        public DateTime? FechaCompra { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo.")]
        public decimal? Precio { get; set; }
        public string? Observaciones { get; set; }
        public DateTime? FechaBaja { get; set; }
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
                if (columnName == nameof(FechaCompra))
                {
                    if (FechaCompra == null)
                        return "Debe ingresar la fecha de compra";
                }
                // ...otras validaciones si las necesitas...
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
