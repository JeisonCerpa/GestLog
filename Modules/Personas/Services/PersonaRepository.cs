using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Personas.Models;
using Modules.Personas.Interfaces;

namespace Modules.Personas.Services
{
    public class PersonaRepository : IPersonaRepository
    {
        private static readonly List<Persona> _personas = new();

        // Aquí se inyectaría el contexto de datos o acceso a base de datos
        public PersonaRepository()
        {
            // Inicialización de recursos de datos
        }

        public Task<Persona> AgregarAsync(Persona persona)
        {
            persona.IdPersona = Guid.NewGuid();
            _personas.Add(persona);
            return Task.FromResult(persona);
        }

        public Task<Persona> ActualizarAsync(Persona persona)
        {
            var existente = _personas.Find(p => p.IdPersona == persona.IdPersona);
            if (existente != null)
            {
                existente.Nombres = persona.Nombres;
                existente.Apellidos = persona.Apellidos;
                existente.TipoDocumento = persona.TipoDocumento;
                existente.NumeroDocumento = persona.NumeroDocumento;
                existente.Correo = persona.Correo;
                existente.Telefono = persona.Telefono;
                existente.Cargo = persona.Cargo;
                existente.Estado = persona.Estado;
                existente.Activo = persona.Activo;
            }
            return Task.FromResult(persona);
        }

        public Task DesactivarAsync(Guid idPersona)
        {
            var existente = _personas.Find(p => p.IdPersona == idPersona);
            if (existente != null)
            {
                existente.Activo = false;
                existente.Estado = "Inactivo";
            }
            return Task.CompletedTask;
        }

        public Task<Persona> ObtenerPorIdAsync(Guid idPersona)
        {
            var persona = _personas.Find(p => p.IdPersona == idPersona);
            if (persona == null)
                throw new InvalidOperationException("Persona no encontrada");
            return Task.FromResult(persona);
        }

        public Task<IEnumerable<Persona>> BuscarAsync(string filtro)
        {
            IEnumerable<Persona> personas = _personas;
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                filtro = filtro.ToLowerInvariant();
                personas = _personas.FindAll(p =>
                    (p.Nombres?.ToLowerInvariant().Contains(filtro) ?? false) ||
                    (p.Apellidos?.ToLowerInvariant().Contains(filtro) ?? false) ||
                    (p.NumeroDocumento?.ToLowerInvariant().Contains(filtro) ?? false) ||
                    (p.Correo?.ToLowerInvariant().Contains(filtro) ?? false) ||
                    (p.Telefono?.ToLowerInvariant().Contains(filtro) ?? false)
                );
            }
            return Task.FromResult(personas);
        }

        public Task<bool> ExisteDocumentoAsync(string tipoDocumento, string numeroDocumento)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExisteCorreoAsync(string correo)
        {
            throw new NotImplementedException();
        }
    }
}
