using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Autocomplete;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Autocomplete
{
    /// <summary>
    /// Servicio para obtener valores únicos de dispositivos desde la tabla PerifericosEquiposInformaticos
    /// con funcionalidad de autocompletado y aprendizaje automático
    /// </summary>
    public class DispositivoAutocompletadoService : IDispositivoAutocompletadoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public DispositivoAutocompletadoService(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Obtiene todos los dispositivos únicos ordenados por frecuencia de uso
        /// </summary>
        public async Task<List<string>> ObtenerTodosAsync()
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var dispositivos = await context.PerifericosEquiposInformaticos
                .Where(p => !string.IsNullOrEmpty(p.Dispositivo))
                .GroupBy(p => p.Dispositivo.Trim().ToLower())
                .Select(g => new { 
                    Dispositivo = g.First().Dispositivo.Trim(), 
                    Cantidad = g.Count() 
                })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.Dispositivo)
                .Select(x => x.Dispositivo)
                .ToListAsync();

            return dispositivos;
        }        /// <summary>
        /// Obtiene dispositivos que coinciden con el filtro especificado
        /// </summary>
        public async Task<List<string>> BuscarAsync(string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro))
                return await ObtenerTodosAsync();

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var filtroLower = filtro.Trim().ToLower();
            
            var dispositivos = await context.PerifericosEquiposInformaticos
                .Where(p => !string.IsNullOrEmpty(p.Dispositivo) && 
                           p.Dispositivo.ToLower().Contains(filtroLower))
                .GroupBy(p => p.Dispositivo.Trim().ToLower())
                .Select(g => new { 
                    Dispositivo = g.First().Dispositivo.Trim(), 
                    Cantidad = g.Count() 
                })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.Dispositivo)
                .Select(x => x.Dispositivo)
                .ToListAsync();

            return dispositivos;
        }

        /// <summary>
        /// Obtiene estadísticas de uso de dispositivos
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerEstadisticasAsync()
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var estadisticas = await context.PerifericosEquiposInformaticos
                .Where(p => !string.IsNullOrEmpty(p.Dispositivo))
                .GroupBy(p => p.Dispositivo.Trim())
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            return estadisticas;
        }

        /// <summary>
        /// Obtiene los dispositivos más utilizados (top N)
        /// </summary>
        public async Task<List<string>> ObtenerMasUtilizadosAsync(int limite = 10)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            
            var dispositivos = await context.PerifericosEquiposInformaticos
                .Where(p => !string.IsNullOrEmpty(p.Dispositivo))
                .GroupBy(p => p.Dispositivo.Trim().ToLower())
                .Select(g => new { 
                    Dispositivo = g.First().Dispositivo.Trim(), 
                    Cantidad = g.Count() 
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(limite)
                .Select(x => x.Dispositivo)
                .ToListAsync();

            return dispositivos;
        }
    }
}
