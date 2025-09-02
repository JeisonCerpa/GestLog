using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Entities
{
    public class SlotRamEntity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string CodigoEquipo { get; set; } = string.Empty;
        
        [Required]
        public int NumeroSlot { get; set; }
        
        public int? CapacidadGB { get; set; }
        
        [MaxLength(50)]
        public string? TipoMemoria { get; set; }
        
        [MaxLength(100)]
        public string? Marca { get; set; }
        
        [MaxLength(50)]
        public string? Frecuencia { get; set; }
        
        public bool Ocupado { get; set; } = false;
        
        [MaxLength(200)]
        public string? Observaciones { get; set; }
        
        // Navegación - Relación N:1
        [ForeignKey("CodigoEquipo")]
        public virtual EquipoInformaticoEntity Equipo { get; set; } = null!;
    }
}
