using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Entities
{
    public class SlotRamEntity : ObservableObject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string CodigoEquipo { get; set; } = string.Empty;
        
        [Required]
        public int NumeroSlot { get; set; }
        
        private int? _capacidadGB;
        public int? CapacidadGB
        {
            get => _capacidadGB;
            set
            {
                if (SetProperty(ref _capacidadGB, value))
                    UpdateOcupadoFromFields();
            }
        }
        
        private string? _tipoMemoria;
        [MaxLength(50)]
        public string? TipoMemoria
        {
            get => _tipoMemoria;
            set
            {
                if (SetProperty(ref _tipoMemoria, value))
                    UpdateOcupadoFromFields();
            }
        }
        
        private string? _marca;
        [MaxLength(100)]
        public string? Marca
        {
            get => _marca;
            set
            {
                if (SetProperty(ref _marca, value))
                    UpdateOcupadoFromFields();
            }
        }
        
        private string? _frecuencia;
        [MaxLength(50)]
        public string? Frecuencia
        {
            get => _frecuencia;
            set
            {
                if (SetProperty(ref _frecuencia, value))
                    UpdateOcupadoFromFields();
            }
        }
        
        private bool _ocupado = false;
        public bool Ocupado
        {
            get => _ocupado;
            set => SetProperty(ref _ocupado, value);
        }
        
        private string? _observaciones;
        [MaxLength(200)]
        public string? Observaciones
        {
            get => _observaciones;
            set
            {
                if (SetProperty(ref _observaciones, value))
                    UpdateOcupadoFromFields();
            }
        }
        
        // Navegación - Relación N:1
        [ForeignKey("CodigoEquipo")]
        public virtual EquipoInformaticoEntity Equipo { get; set; } = null!;

        private void UpdateOcupadoFromFields()
        {
            // Si cualquiera de los campos relevantes tiene valor, marcar como ocupado; si todos vacíos, marcar no ocupado.
            bool tieneDatos = CapacidadGB.HasValue
                              || !string.IsNullOrWhiteSpace(TipoMemoria)
                              || !string.IsNullOrWhiteSpace(Marca)
                              || !string.IsNullOrWhiteSpace(Frecuencia)
                              || !string.IsNullOrWhiteSpace(Observaciones);

            // Evitar set redundante
            if (tieneDatos && !Ocupado)
            {
                _ocupado = true; // asignar directamente al campo para no disparar UpdateOcupadoFromFields nuevamente
                OnPropertyChanged(nameof(Ocupado));
            }
            else if (!tieneDatos && Ocupado)
            {
                _ocupado = false;
                OnPropertyChanged(nameof(Ocupado));
            }
        }
    }
}
