using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models.DTOs
{    public class MantenimientoSemanaEstadoDto : INotifyPropertyChanged
    {
        [Required(ErrorMessage = "El código del equipo es obligatorio.")]
        public string CodigoEquipo { get; set; } = "";
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        public string NombreEquipo { get; set; } = "";
        [Range(1, 53, ErrorMessage = "La semana debe estar entre 1 y 53.")]
        public int Semana { get; set; }
        [Range(2000, 2100, ErrorMessage = "El año debe ser válido.")]
        public int Anio { get; set; }
        public FrecuenciaMantenimiento? Frecuencia { get; set; }
        public Sede? Sede { get; set; }
        public bool Programado { get; set; }

        private bool _realizado;
        public bool Realizado
        {
            get => _realizado;
            set
            {
                if (_realizado != value)
                {
                    _realizado = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _atrasado;
        public bool Atrasado
        {
            get => _atrasado;
            set
            {
                if (_atrasado != value)
                {
                    _atrasado = value;
                    OnPropertyChanged();
                }
            }
        }

        private SeguimientoMantenimientoDto? _seguimiento;
        public SeguimientoMantenimientoDto? Seguimiento
        {
            get => _seguimiento;
            set
            {
                if (_seguimiento != value)
                {
                    _seguimiento = value;
                    OnPropertyChanged();
                }
            }
        }

        private EstadoSeguimientoMantenimiento _estado;
        public EstadoSeguimientoMantenimiento Estado
        {
            get => _estado;
            set
            {
                if (_estado != value)
                {
                    _estado = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _puedeRegistrar;
        public bool PuedeRegistrar
        {
            get => _puedeRegistrar;
            set
            {
                if (_puedeRegistrar != value)
                {
                    _puedeRegistrar = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
