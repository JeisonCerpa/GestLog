using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionMantenimientos.Services.Autocomplete
{
    /// <summary>
    /// Servicio para obtener valores únicos de marca desde la tabla Equipos
    /// con funcionalidades de autocompletado y búsqueda.
    /// </summary>
    public class MarcaAutocompletadoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public MarcaAutocompletadoService(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<string>> ObtenerTodosAsync()
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var list = await context.Equipos
                .Where(e => !string.IsNullOrEmpty(e.Marca))
                .GroupBy(e => e.Marca!.Trim().ToLower())
                .Select(g => new { Val = g.First().Marca!.Trim(), Cant = g.Count() })
                .OrderByDescending(x => x.Cant)
                .ThenBy(x => x.Val)
                .Select(x => x.Val)
                .ToListAsync();

            return list;
        }

        public async Task<List<string>> ObtenerMasUtilizadasAsync(int cantidad = 50)
        {
            return (await ObtenerTodosAsync()).Take(cantidad).ToList();
        }

        public async Task<List<string>> BuscarAsync(string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro))
                return await ObtenerTodosAsync();

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var filtroLower = filtro.Trim().ToLower();

            var list = await context.Equipos
                .Where(e => !string.IsNullOrEmpty(e.Marca) && e.Marca!.ToLower().Contains(filtroLower))
                .GroupBy(e => e.Marca!.Trim().ToLower())
                .Select(g => new { Val = g.First().Marca!.Trim(), Cant = g.Count() })
                .OrderByDescending(x => x.Cant)
                .ThenBy(x => x.Val)
                .Select(x => x.Val)
                .ToListAsync();

            return list;
        }
    }
}
