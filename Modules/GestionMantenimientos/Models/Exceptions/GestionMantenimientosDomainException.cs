using System;

namespace GestLog.Modules.GestionMantenimientos.Models.Exceptions
{
    /// <summary>
    /// Excepción de dominio para errores de validación y reglas de negocio en Gestión de Mantenimientos.
    /// </summary>
    public class GestionMantenimientosDomainException : Exception
    {
        public GestionMantenimientosDomainException(string message) : base(message) { }
        public GestionMantenimientosDomainException(string message, Exception inner) : base(message, inner) { }
    }
}
