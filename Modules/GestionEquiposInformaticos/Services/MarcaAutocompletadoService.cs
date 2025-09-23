using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.Services
{
    /// <summary>
    /// Servicio para obtener valores únicos de marcas desde la tabla PerifericosEquiposInformaticos
    /// con funcionalidad de autocompletado y aprendizaje automático
    /// </summary>
    public class MarcaAutocompletadoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public MarcaAutocompletadoService(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Obtiene todas las marcas únicas ordenadas por frecuencia de uso
        /// </summary>
        public async Task<List<string>> ObtenerTodosAsync()
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var marcas = await context.PerifericosEquiposInformaticos
                .Where(p => !string.IsNullOrEmpty(p.Marca))
                .GroupBy(p => p.Marca!.Trim().ToLower())
                .Select(g => new { 
                    Marca = g.First().Marca!.Trim(), 
                    Cantidad = g.Count() 
                })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.Marca)
                .Select(x => x.Marca)
                .ToListAsync();

            return marcas;
        }

        /// <summary>
        /// Busca marcas que contengan el texto especificado
        /// </summary>
        public async Task<List<string>> BuscarAsync(string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro))
                return await ObtenerTodosAsync();

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var filtroLower = filtro.Trim().ToLower();
            
            var marcas = await context.PerifericosEquiposInformaticos
                .Where(p => !string.IsNullOrEmpty(p.Marca) && 
                           p.Marca!.ToLower().Contains(filtroLower))
                .GroupBy(p => p.Marca!.Trim().ToLower())
                .Select(g => new { 
                    Marca = g.First().Marca!.Trim(), 
                    Cantidad = g.Count() 
                })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.Marca)
                .Select(x => x.Marca)
                .ToListAsync();

            return marcas;
        }

        /// <summary>
        /// Obtiene estadísticas de uso de marcas
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerEstadisticasAsync()
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var estadisticas = await context.PerifericosEquiposInformaticos
                .Where(p => !string.IsNullOrEmpty(p.Marca))
                .GroupBy(p => p.Marca!.Trim())
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            return estadisticas;
        }

        /// <summary>
        /// Obtiene las marcas más utilizadas (top N)
        /// </summary>
        public async Task<List<string>> ObtenerMasUtilizadasAsync(int limite = 10)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var marcas = await context.PerifericosEquiposInformaticos
                .Where(p => !string.IsNullOrEmpty(p.Marca))
                .GroupBy(p => p.Marca!.Trim().ToLower())
                .Select(g => new { 
                    Marca = g.First().Marca!.Trim(), 
                    Cantidad = g.Count() 
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(limite)
                .Select(x => x.Marca)
                .ToListAsync();

            return marcas;
        }
    }
}
