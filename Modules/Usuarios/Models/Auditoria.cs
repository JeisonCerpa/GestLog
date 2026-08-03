using System;

namespace GestLog.Modules.Usuarios.Models
{
    public class Auditoria
    {
        public Guid IdAuditoria { get; set; }
        public required string EntidadAfectada { get; set; }
        public Guid IdEntidad { get; set; }
        /// <summary>
        /// Clave natural de la entidad cuando su identificador no es Guid (ej. código de equipo "CLASE-001").
        /// </summary>
        public string? ClaveEntidad { get; set; }
        /// <summary>
        /// Nombre legible del registro afectado (ej. nombre del equipo), para no dejar solo el código.
        /// </summary>
        public string? DescripcionEntidad { get; set; }
        public required string Accion { get; set; }
        public required string UsuarioResponsable { get; set; }
        public DateTime FechaHora { get; set; }
        public required string Detalle { get; set; }
    }
}
