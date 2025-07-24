using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Personas.Models;

namespace Modules.Personas.Interfaces
{
    /// <summary>
    /// Contrato para la gestión de personas.
    /// </summary>
    public interface IPersonaService
    {
        Task<Persona> RegistrarPersonaAsync(Persona persona);
        Task<Persona> EditarPersonaAsync(Persona persona);
        Task DesactivarPersonaAsync(Guid idPersona);
        Task<Persona> ObtenerPersonaPorIdAsync(Guid idPersona);
        Task<IEnumerable<Persona>> BuscarPersonasAsync(string filtro);
        Task<bool> ValidarDocumentoUnicoAsync(string tipoDocumento, string numeroDocumento);
        Task<bool> ValidarCorreoUnicoAsync(string correo);
    }
}
