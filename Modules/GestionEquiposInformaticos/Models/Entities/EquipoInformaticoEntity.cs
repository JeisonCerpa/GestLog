using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Entities
{
    public class EquipoInformaticoEntity
    {
        [Key]
        [Required]
        [MaxLength(20)]
        public string Codigo { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? UsuarioAsignado { get; set; }
        
        [MaxLength(100)]
        public string? NombreEquipo { get; set; }
        
        public decimal? Costo { get; set; }
        
        public DateTime? FechaCompra { get; set; }
        
        [MaxLength(50)]
        public string? Estado { get; set; } = "Disponible";
        
        [MaxLength(50)]
        public string? Sede { get; set; } = "Administrativa - Barranquilla";
        
        [MaxLength(50)]
        public string? CodigoAnydesk { get; set; }
        
        // Especificaciones técnicas
        [MaxLength(100)]
        public string? Modelo { get; set; }
        
        [MaxLength(100)]
        public string? SO { get; set; }
        
        [MaxLength(50)]
        public string? Marca { get; set; }
        
        [MaxLength(100)]
        public string? SerialNumber { get; set; }
        
        [MaxLength(100)]
        public string? Procesador { get; set; }
        
        // RAM - Información general
        // public int? SlotsTotales { get; set; }
        // public int? SlotsUtilizados { get; set; }
        // [MaxLength(50)]
        // public string? TipoRam { get; set; }
        // public int? CapacidadTotalRamGB { get; set; }
        
        // Almacenamiento - Información general
        // public int? CantidadDiscos { get; set; }
        // public int? CapacidadTotalDiscosGB { get; set; }
        
        [MaxLength(500)]
        public string? Observaciones { get; set; }
        
        public DateTime? FechaBaja { get; set; }
        
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        public DateTime? FechaModificacion { get; set; }
        
        // Navegación - Relaciones 1:N
        public virtual ICollection<SlotRamEntity> SlotsRam { get; set; } = new List<SlotRamEntity>();
        public virtual ICollection<DiscoEntity> Discos { get; set; } = new List<DiscoEntity>();
    }
}
