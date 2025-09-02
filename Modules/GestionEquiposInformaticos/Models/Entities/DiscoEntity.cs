using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Entities
{
    public class DiscoEntity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string CodigoEquipo { get; set; } = string.Empty;
        
        [Required]
        public int NumeroDisco { get; set; }
        
        [MaxLength(50)]
        public string? Tipo { get; set; } = "HDD";
        
        public int? CapacidadGB { get; set; }
        
        [MaxLength(100)]
        public string? Marca { get; set; }
        
        [MaxLength(100)]
        public string? Modelo { get; set; }
        
        // Navegación - Relación N:1
        [ForeignKey("CodigoEquipo")]
        public virtual EquipoInformaticoEntity Equipo { get; set; } = null!;
    }
}
