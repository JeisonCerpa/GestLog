using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models.DTOs
{
    public class MantenimientoSemanaEstadoDto : INotifyPropertyChanged
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
        public bool Programado { get; set; }
        public bool Realizado { get; set; }
        public bool Atrasado { get; set; }
        public SeguimientoMantenimientoDto? Seguimiento { get; set; }
        public EstadoSeguimientoMantenimiento Estado { get; set; }

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
