using System;
using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.Usuarios.Models
{
    public class Cargo
    {
        [Key]
        public Guid IdCargo { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
    }
}
