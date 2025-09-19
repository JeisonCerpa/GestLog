using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Entities
{
    /// <summary>
    /// Entidad para periféricos de equipos informáticos
    /// </summary>
    [Table("PerifericosEquiposInformaticos")]
    public class PerifericoEquipoInformaticoEntity
    {
        [Key]
        [Required]
        [StringLength(50)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Dispositivo { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateTime? FechaCompra { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Costo { get; set; }

        [StringLength(50)]
        public string? Marca { get; set; }

        [StringLength(50)]
        public string? Modelo { get; set; }

        [StringLength(100)]
        public string? SerialNumber { get; set; }        /// <summary>
        /// Código del equipo al que está asignado (puede ser null si no está asignado)
        /// </summary>
        [StringLength(20)]
        public string? CodigoEquipoAsignado { get; set; }

        /// <summary>
        /// Usuario al que está asignado (puede ser null si está asignado a un equipo o no asignado)
        /// </summary>
        [StringLength(100)]
        public string? UsuarioAsignado { get; set; }

        [Required]
        public SedePeriferico Sede { get; set; }

        [Required]
        public EstadoPeriferico Estado { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Required]
        public DateTime FechaModificacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Descripción del estado para mostrar en UI
        /// </summary>
        [NotMapped]
        public string EstadoDescripcion
        {
            get
            {
                return Estado switch
                {
                    EstadoPeriferico.EnUso => "En Uso",
                    EstadoPeriferico.AlmacenadoFuncionando => "Almacenado (Funcionando)",
                    EstadoPeriferico.DadoDeBaja => "Dado de Baja",
                    _ => Estado.ToString()
                };
            }
        }

        /// <summary>
        /// Relación de navegación opcional con el equipo asignado
        /// </summary>
        [ForeignKey(nameof(CodigoEquipoAsignado))]
        public virtual EquipoInformaticoEntity? EquipoAsignado { get; set; }
    }
}
