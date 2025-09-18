using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Entities
{
    [Table("ConexionesEquiposInformaticos")]
    public class ConexionEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string CodigoEquipo { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Adaptador { get; set; }

        [StringLength(17)] // Format: XX:XX:XX:XX:XX:XX
        public string? DireccionMAC { get; set; }

        [StringLength(15)] // Format: XXX.XXX.XXX.XXX
        public string? DireccionIPv4 { get; set; }

        [StringLength(15)] // Format: XXX.XXX.XXX.XXX
        public string? MascaraSubred { get; set; }        [StringLength(15)] // Format: XXX.XXX.XXX.XXX
        public string? PuertoEnlace { get; set; }

        // Relación con EquipoInformaticoEntity
        [ForeignKey("CodigoEquipo")]
        public virtual EquipoInformaticoEntity? EquipoInformatico { get; set; }
    }
}
