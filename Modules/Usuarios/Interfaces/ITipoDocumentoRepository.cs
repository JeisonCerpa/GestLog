using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    public interface ITipoDocumentoRepository
    {
        Task<List<TipoDocumento>> ObtenerTodosAsync();
        Task<TipoDocumento?> ObtenerPorIdAsync(Guid id);
        Task<TipoDocumento?> ObtenerPorCodigoAsync(string codigo);
        Task AgregarAsync(TipoDocumento tipoDocumento);
        Task ActualizarAsync(TipoDocumento tipoDocumento);
        Task EliminarAsync(Guid id);
    }
}
