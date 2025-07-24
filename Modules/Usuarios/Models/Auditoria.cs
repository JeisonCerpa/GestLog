using System;

namespace GestLog.Modules.Usuarios.Models
{
    public class Auditoria
    {
        public Guid IdAuditoria { get; set; }
        public required string EntidadAfectada { get; set; }
        public Guid IdEntidad { get; set; }
        public required string Accion { get; set; }
        public required string UsuarioResponsable { get; set; }
        public DateTime FechaHora { get; set; }
        public required string Detalle { get; set; }
    }
}
