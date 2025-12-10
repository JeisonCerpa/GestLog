using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionMantenimientos.Services.Autocomplete
{
    /// <summary>
    /// Servicio para obtener valores únicos de 'CompradoA' desde la tabla Equipos
    /// con funcionalidades de autocompletado y búsqueda.
    /// </summary>
    public class CompradoAAutocompletadoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public CompradoAAutocompletadoService(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<string>> ObtenerTodosAsync()
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var list = await context.Equipos
                .Where(e => !string.IsNullOrEmpty(e.CompradoA))
                .GroupBy(e => e.CompradoA!.Trim().ToLower())
                .Select(g => new { Val = g.First().CompradoA!.Trim(), Cant = g.Count() })
                .OrderByDescending(x => x.Cant)
                .ThenBy(x => x.Val)
                .Select(x => x.Val)
                .ToListAsync();

            return list;
        }

        public async Task<List<string>> BuscarAsync(string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro))
                return await ObtenerTodosAsync();

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var filtroLower = filtro.Trim().ToLower();

            var list = await context.Equipos
                .Where(e => !string.IsNullOrEmpty(e.CompradoA) && e.CompradoA!.ToLower().Contains(filtroLower))
                .GroupBy(e => e.CompradoA!.Trim().ToLower())
                .Select(g => new { Val = g.First().CompradoA!.Trim(), Cant = g.Count() })
                .OrderByDescending(x => x.Cant)
                .ThenBy(x => x.Val)
                .Select(x => x.Val)
                .ToListAsync();

            return list;
        }

        public async Task<List<string>> ObtenerMasUtilizadasAsync(int limite = 10)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var list = await context.Equipos
                .Where(e => !string.IsNullOrEmpty(e.CompradoA))
                .GroupBy(e => e.CompradoA!.Trim().ToLower())
                .Select(g => new { Val = g.First().CompradoA!.Trim(), Cant = g.Count() })
                .OrderByDescending(x => x.Cant)
                .Take(limite)
                .Select(x => x.Val)
                .ToListAsync();
            return list;
        }
    }
}
