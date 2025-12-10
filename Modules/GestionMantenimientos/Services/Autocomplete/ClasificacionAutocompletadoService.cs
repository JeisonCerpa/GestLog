using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionMantenimientos.Services.Autocomplete
{
    /// <summary>
    /// Servicio para obtener valores únicos de clasificación desde la tabla Equipos
    /// con funcionalidades de autocompletado y búsqueda.
    /// </summary>
    public class ClasificacionAutocompletadoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public ClasificacionAutocompletadoService(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<string>> ObtenerTodosAsync()
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var list = await context.Equipos
                .Where(e => !string.IsNullOrEmpty(e.Clasificacion))
                .GroupBy(e => e.Clasificacion!.Trim().ToLower())
                .Select(g => new { Val = g.First().Clasificacion!.Trim(), Cant = g.Count() })
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
                .Where(e => !string.IsNullOrEmpty(e.Clasificacion) && e.Clasificacion!.ToLower().Contains(filtroLower))
                .GroupBy(e => e.Clasificacion!.Trim().ToLower())
                .Select(g => new { Val = g.First().Clasificacion!.Trim(), Cant = g.Count() })
                .OrderByDescending(x => x.Cant)
                .ThenBy(x => x.Val)
                .Select(x => x.Val)
                .ToListAsync();

            return list;
        }

        public async Task<Dictionary<string,int>> ObtenerEstadisticasAsync()
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var stats = await context.Equipos
                .Where(e => !string.IsNullOrEmpty(e.Clasificacion))
                .GroupBy(e => e.Clasificacion!.Trim())
                .ToDictionaryAsync(g => g.Key, g => g.Count());
            return stats;
        }

        public async Task<List<string>> ObtenerMasUtilizadasAsync(int limite = 10)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var list = await context.Equipos
                .Where(e => !string.IsNullOrEmpty(e.Clasificacion))
                .GroupBy(e => e.Clasificacion!.Trim().ToLower())
                .Select(g => new { Val = g.First().Clasificacion!.Trim(), Cant = g.Count() })
                .OrderByDescending(x => x.Cant)
                .Take(limite)
                .Select(x => x.Val)
                .ToListAsync();
            return list;
        }
    }
}
