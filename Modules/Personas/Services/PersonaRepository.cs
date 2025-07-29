using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.Personas.Models;
using Microsoft.EntityFrameworkCore;
using Modules.Personas.Interfaces;

namespace Modules.Personas.Services
{
    public class PersonaRepository : IPersonaRepository
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public PersonaRepository(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Persona> AgregarAsync(Persona persona)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            persona.IdPersona = Guid.NewGuid();
            persona.FechaCreacion = DateTime.UtcNow;
            persona.FechaModificacion = DateTime.UtcNow;
            // Evitar que EF intente insertar el Cargo asociado si ya existe
            persona.Cargo = null;
            persona.TipoDocumento = null;
            dbContext.Personas.Add(persona);
            await dbContext.SaveChangesAsync();
            return persona;
        }

        public async Task<Persona> ActualizarAsync(Persona persona)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            persona.FechaModificacion = DateTime.UtcNow;
            dbContext.Personas.Update(persona);
            await dbContext.SaveChangesAsync();
            return persona;
        }

        public async Task DesactivarAsync(Guid idPersona)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existente = await dbContext.Personas.FindAsync(idPersona);
            if (existente != null)
            {
                existente.Activo = false;
                existente.FechaModificacion = DateTime.UtcNow;
                dbContext.Personas.Update(existente);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<Persona> ObtenerPorIdAsync(Guid idPersona)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var persona = await dbContext.Personas.FindAsync(idPersona);
            if (persona == null)
                throw new InvalidOperationException("Persona no encontrada");
            return persona;
        }

        public async Task<IEnumerable<Persona>> BuscarAsync(string filtro)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var query = dbContext.Personas
                .Include(p => p.Cargo) // Incluye el nombre del cargo
                .Include(p => p.TipoDocumento) // Incluye el nombre del tipo de documento
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                filtro = filtro.ToLowerInvariant();
                query = query.Where(p =>
                    p.Nombres.ToLower().Contains(filtro) ||
                    p.Apellidos.ToLower().Contains(filtro) ||
                    p.NumeroDocumento.ToLower().Contains(filtro) ||
                    p.Correo.ToLower().Contains(filtro) ||
                    p.Telefono.ToLower().Contains(filtro)
                );
            }
            return await query.ToListAsync();
        }

        public async Task<bool> ExisteDocumentoAsync(Guid tipoDocumentoId, string numeroDocumento)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Personas.AnyAsync(p => p.TipoDocumentoId == tipoDocumentoId && p.NumeroDocumento == numeroDocumento);
        }

        public async Task<bool> ExisteCorreoAsync(string correo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Personas.AnyAsync(p => p.Correo.ToLower() == correo.ToLower());
        }
    }
}
