using System.Threading.Tasks;

namespace GestLog.Services.Interfaces
{
    /// <summary>
    /// Proveedor centralizado de configuración de base de datos desde config/database-development.json
    /// </summary>
    public interface IDatabaseConfigurationProvider
    {
        /// <summary>
        /// Obtiene la cadena de conexión actual
        /// </summary>
        Task<string> GetConnectionStringAsync();
    }
}
