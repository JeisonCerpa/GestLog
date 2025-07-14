using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Models
{
    public class MantenimientoSemanaEstadoDto : INotifyPropertyChanged
    {
        public string CodigoEquipo { get; set; } = "";
        public string NombreEquipo { get; set; } = "";
        public int Semana { get; set; }
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
