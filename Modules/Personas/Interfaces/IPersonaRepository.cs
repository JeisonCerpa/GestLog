using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Personas.Models;

namespace Modules.Personas.Interfaces
{
    /// <summary>
    /// Contrato para acceso a datos de personas.
    /// </summary>
    public interface IPersonaRepository
    {
        Task<Persona> AgregarAsync(Persona persona);
        Task<Persona> ActualizarAsync(Persona persona);
        Task DesactivarAsync(Guid idPersona);
        Task<Persona> ObtenerPorIdAsync(Guid idPersona);
        Task<IEnumerable<Persona>> BuscarAsync(string filtro);
        Task<bool> ExisteDocumentoAsync(string tipoDocumento, string numeroDocumento);
        Task<bool> ExisteCorreoAsync(string correo);
    }
}
