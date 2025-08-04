using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;

namespace Modules.Usuarios.Services
{
    public class RolPermisoRepository : IRolPermisoRepository
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        public RolPermisoRepository(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }
        public async Task AsignarPermisosAsync(Guid idRol, IEnumerable<Guid> permisosIds)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var actuales = db.RolPermisos.Where(rp => rp.IdRol == idRol);
            db.RolPermisos.RemoveRange(actuales);
            foreach (var idPermiso in permisosIds)
            {
                db.RolPermisos.Add(new RolPermiso { IdRol = idRol, IdPermiso = idPermiso });
            }
            await db.SaveChangesAsync();
        }
        public async Task<IEnumerable<RolPermiso>> ObtenerPorRolAsync(Guid idRol)
        {
            using var db = _dbContextFactory.CreateDbContext();
            return await db.RolPermisos.Where(rp => rp.IdRol == idRol).ToListAsync();
        }
        public async Task EliminarPermisosDeRolAsync(Guid idRol)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var actuales = db.RolPermisos.Where(rp => rp.IdRol == idRol);
            db.RolPermisos.RemoveRange(actuales);
            await db.SaveChangesAsync();
        }
    }
}
