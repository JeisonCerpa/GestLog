using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Usuarios.Interfaces;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Services
{
    public class AuditoriaRepository : IAuditoriaRepository
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public AuditoriaRepository(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task RegistrarAsync(Auditoria auditoria)
        {
            if (auditoria == null) throw new ArgumentNullException(nameof(auditoria));

            if (auditoria.IdAuditoria == Guid.Empty)
                auditoria.IdAuditoria = Guid.NewGuid();
            if (auditoria.FechaHora == default)
                auditoria.FechaHora = DateTime.Now;

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            db.Auditorias.Add(auditoria);
            await db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Auditoria>> ObtenerPorEntidadAsync(string entidadAfectada, Guid idEntidad)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.Auditorias
                .AsNoTracking()
                .Where(a => a.EntidadAfectada == entidadAfectada && a.IdEntidad == idEntidad)
                .OrderByDescending(a => a.FechaHora)
                .ToListAsync();
        }

        public async Task<IEnumerable<Auditoria>> ObtenerPorClaveAsync(string entidadAfectada, string claveEntidad)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.Auditorias
                .AsNoTracking()
                .Where(a => a.EntidadAfectada == entidadAfectada && a.ClaveEntidad == claveEntidad)
                .OrderByDescending(a => a.FechaHora)
                .ToListAsync();
        }

        public async Task<IEnumerable<Auditoria>> BuscarAsync(string? entidadAfectada, string? usuarioResponsable,
            DateTime? desde, DateTime? hasta, string? texto, int maxResultados = 500)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var consulta = db.Auditorias.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(entidadAfectada))
                consulta = consulta.Where(a => a.EntidadAfectada == entidadAfectada);

            if (!string.IsNullOrWhiteSpace(usuarioResponsable))
                consulta = consulta.Where(a => a.UsuarioResponsable.Contains(usuarioResponsable));

            if (desde.HasValue)
                consulta = consulta.Where(a => a.FechaHora >= desde.Value);

            if (hasta.HasValue)
            {
                // Incluye el día completo indicado como límite superior
                var limite = hasta.Value.Date.AddDays(1);
                consulta = consulta.Where(a => a.FechaHora < limite);
            }

            if (!string.IsNullOrWhiteSpace(texto))
                consulta = consulta.Where(a => a.Detalle.Contains(texto)
                                            || (a.ClaveEntidad != null && a.ClaveEntidad.Contains(texto))
                                            || (a.DescripcionEntidad != null && a.DescripcionEntidad.Contains(texto))
                                            || a.Accion.Contains(texto));

            return await consulta
                .OrderByDescending(a => a.FechaHora)
                .Take(maxResultados)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> ObtenerEntidadesAsync()
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.Auditorias
                .AsNoTracking()
                .Select(a => a.EntidadAfectada)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();
        }

        public async Task<IEnumerable<Auditoria>> ObtenerPorUsuarioAsync(string usuarioResponsable)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.Auditorias
                .AsNoTracking()
                .Where(a => a.UsuarioResponsable == usuarioResponsable)
                .OrderByDescending(a => a.FechaHora)
                .ToListAsync();
        }
    }
}
