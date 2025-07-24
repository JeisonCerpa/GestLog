using System;

namespace GestLog.Modules.Usuarios.Models
{
    public class TipoDocumento
    {
        public Guid IdTipoDocumento { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty; // Ej: CC, TI, PAS, NIT
        public string Descripcion { get; set; } = string.Empty;
    }
}
