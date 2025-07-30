using GestLog.Modules.Personas.Models;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Modules.Personas.Interfaces;
using Modules.Usuarios.Helpers;
using Modules.Usuarios.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Modules.Personas.Services
{
    public class PersonaService : IPersonaService
    {
        private readonly IPersonaRepository _personaRepository;
        private readonly IGestLogLogger _logger;
        private readonly IAuditoriaService _auditoriaService;

        public PersonaService(IPersonaRepository personaRepository, IGestLogLogger logger, IAuditoriaService auditoriaService)
        {
            _personaRepository = personaRepository;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        public async Task<Persona> RegistrarPersonaAsync(Persona persona)
        {
            try
            {
                if (await _personaRepository.ExisteDocumentoAsync(persona.TipoDocumentoId, persona.NumeroDocumento))
                {
                    _logger.LogWarning($"Duplicate document: {persona.TipoDocumentoId}-{persona.NumeroDocumento}");
                    throw new Exception($"Ya existe una persona con el documento '{persona.NumeroDocumento}'.");
                }
                if (await _personaRepository.ExisteCorreoAsync(persona.Correo))
                {
                    _logger.LogWarning($"Duplicate email: {persona.Correo}");
                    throw new CorreoDuplicadoException(persona.Correo);
                }
                var result = await _personaRepository.AgregarAsync(persona);
                _logger.LogInformation($"Person registered: {persona.NumeroDocumento}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Persona",
                    IdEntidad = result.IdPersona,
                    Accion = "Crear",
                    UsuarioResponsable = "admin", // Reemplazar por usuario real
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Registro de persona: {result.Nombres} {result.Apellidos}, Documento: {result.TipoDocumento}-{result.NumeroDocumento}"
                });
                return result;
            }
            catch (CorreoDuplicadoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering person: {ex.Message}");
                throw;
            }
        }

        public async Task<Persona> EditarPersonaAsync(Persona persona)
        {
            try
            {
                var existente = await _personaRepository.ObtenerPorIdAsync(persona.IdPersona);
                if (existente == null)
                {
                    _logger.LogWarning($"Person not found: {persona.IdPersona}");
                    throw new PersonaNotFoundException(persona.IdPersona.ToString());
                }
                if (await _personaRepository.ExisteDocumentoAsync(persona.TipoDocumentoId, persona.NumeroDocumento) &&
                    existente.NumeroDocumento != persona.NumeroDocumento)
                {
                    _logger.LogWarning($"Duplicate document on edit: {persona.TipoDocumentoId}-{persona.NumeroDocumento}");
                    throw new Exception($"Ya existe una persona con el documento '{persona.NumeroDocumento}'.");
                }
                if (await _personaRepository.ExisteCorreoAsync(persona.Correo) && existente.Correo != persona.Correo)
                {
                    _logger.LogWarning($"Duplicate email on edit: {persona.Correo}");
                    throw new CorreoDuplicadoException(persona.Correo);
                }
                var result = await _personaRepository.ActualizarAsync(persona);
                _logger.LogInformation($"Person edited: {persona.NumeroDocumento}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Persona",
                    IdEntidad = result.IdPersona,
                    Accion = "Editar",
                    UsuarioResponsable = "admin", // Reemplazar por usuario real
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Edición de persona: {result.Nombres} {result.Apellidos}, Documento: {result.TipoDocumento}-{result.NumeroDocumento}"
                });
                return result;
            }
            catch (PersonaNotFoundException)
            {
                throw;
            }
            catch (CorreoDuplicadoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing person: {ex.Message}");
                throw;
            }
        }

        public async Task DesactivarPersonaAsync(Guid idPersona)
        {
            try
            {
                var existente = await _personaRepository.ObtenerPorIdAsync(idPersona);
                if (existente == null)
                {
                    _logger.LogWarning($"Person not found for deactivation: {idPersona}");
                    throw new PersonaNotFoundException(idPersona.ToString());
                }
                await _personaRepository.DesactivarAsync(idPersona);
                _logger.LogInformation($"Person deactivated: {idPersona}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Persona",
                    IdEntidad = idPersona,
                    Accion = "Desactivar",
                    UsuarioResponsable = "admin", // Reemplazar por usuario real
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Desactivación de persona: {existente.Nombres} {existente.Apellidos}, Documento: {existente.TipoDocumento}-{existente.NumeroDocumento}"
                });
            }
            catch (PersonaNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating person: {ex.Message}");
                throw;
            }
        }

        public async Task<Persona> ObtenerPersonaPorIdAsync(Guid idPersona)
        {
            try
            {
                var persona = await _personaRepository.ObtenerPorIdAsync(idPersona);
                if (persona == null)
                {
                    _logger.LogWarning($"Person not found: {idPersona}");
                    throw new PersonaNotFoundException(idPersona.ToString());
                }
                return persona;
            }
            catch (PersonaNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting person: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Persona>> BuscarPersonasAsync(string filtro)
        {
            try
            {
                return await _personaRepository.BuscarAsync(filtro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching people: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ValidarDocumentoUnicoAsync(Guid tipoDocumentoId, string numeroDocumento)
        {
            return !await _personaRepository.ExisteDocumentoAsync(tipoDocumentoId, numeroDocumento);
        }

        public async Task<bool> ValidarCorreoUnicoAsync(string correo)
        {
            return !await _personaRepository.ExisteCorreoAsync(correo);
        }
    }
}
