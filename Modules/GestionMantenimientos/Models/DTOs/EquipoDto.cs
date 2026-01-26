using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace GestLog.Modules.GestionMantenimientos.Models.DTOs
{
    public class EquipoDto : INotifyPropertyChanged, IDataErrorInfo
    {
        [Required(ErrorMessage = "El código del equipo es obligatorio.")]
        private string? _codigo;
        public string? Codigo
        {
            get => _codigo;
            set
            {
                var normalized = value?.ToUpperInvariant().Trim();
                if (_codigo != normalized)
                {
                    _codigo = normalized;
                    OnPropertyChanged();
                }
            }
        }

        private string? _nombre;
        public string? Nombre
        {
            get => _nombre;
            set
            {
                var normalized = value?.ToUpperInvariant().Trim();
                if (_nombre != normalized)
                {
                    _nombre = normalized;
                    OnPropertyChanged();
                }
            }
        } // Nombre ahora puede ser null

        private string? _marca;
        public string? Marca
        {
            get => _marca;
            set
            {
                var normalized = value?.ToUpperInvariant().Trim();
                if (_marca != normalized)
                {
                    _marca = normalized;
                    OnPropertyChanged();
                }
            }
        }
        public EstadoEquipo? Estado { get; set; }
        public Sede? Sede { get; set; }        
        public FrecuenciaMantenimiento? FrecuenciaMtto { get; set; }
        public DateTime? FechaRegistro { get; set; } // Fecha de alta del equipo
        public DateTime? FechaCompra { get; set; }   // Fecha de compra - usada como referencia para generar cronogramas
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo.")]
        public decimal? Precio { get; set; }
        public string? Observaciones { get; set; }
        public DateTime? FechaBaja { get; set; }
        // Nuevas propiedades
        private string? _clasificacion;
        public string? Clasificacion
        {
            get => _clasificacion;
            set
            {
                var normalized = value?.ToUpperInvariant().Trim();
                if (_clasificacion != normalized)
                {
                    _clasificacion = normalized;
                    OnPropertyChanged();
                }
            }
        }

        private string? _compradoA;
        public string? CompradoA
        {
            get => _compradoA;
            set
            {
                var normalized = value?.ToUpperInvariant().Trim();
                if (_compradoA != normalized)
                {
                    _compradoA = normalized;
                    OnPropertyChanged();
                }
            }
        }
        
        // Colección de seguimientos (historial de mantenimientos realizados)
        public ObservableCollection<SeguimientoMantenimientoDto> MantenimientosRealizados { get; set; } = new();
    
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
            // Copiar el historial de mantenimientos
            MantenimientosRealizados = new ObservableCollection<SeguimientoMantenimientoDto>(other.MantenimientosRealizados);
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
