using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.Usuarios.Models;
using Microsoft.EntityFrameworkCore;
using Modules.Usuarios.Helpers;
using Modules.Usuarios.Interfaces;

namespace Modules.Usuarios.Services
{
    public class RolRepository : IRolRepository
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public RolRepository(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Rol> AgregarAsync(Rol rol)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            if (rol == null)
                throw new ArgumentNullException(nameof(rol), "El rol no puede ser nulo");
            if (string.IsNullOrWhiteSpace(rol.Nombre))
                throw new ArgumentException("El nombre del rol es obligatorio", nameof(rol.Nombre));
            if (await dbContext.Roles.AnyAsync(r => r.Nombre == rol.Nombre))
                throw new RolDuplicadoException(rol.Nombre);
            rol.IdRol = Guid.NewGuid();
            dbContext.Roles.Add(rol);
            await dbContext.SaveChangesAsync();
            return rol;
        }

        public async Task<Rol> ActualizarAsync(Rol rol)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existente = await dbContext.Roles.FindAsync(rol.IdRol);
            if (existente == null)
                throw new RolNotFoundException(rol.IdRol);
            if (await dbContext.Roles.AnyAsync(r => r.Nombre == rol.Nombre && r.IdRol != rol.IdRol))
                throw new RolDuplicadoException(rol.Nombre);
            existente.Nombre = rol.Nombre;
            existente.Descripcion = rol.Descripcion;
            await dbContext.SaveChangesAsync();
            return existente;
        }

        public async Task EliminarAsync(Guid idRol)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existente = await dbContext.Roles.FindAsync(idRol);
            if (existente == null)
                throw new RolNotFoundException(idRol);
            dbContext.Roles.Remove(existente);
            await dbContext.SaveChangesAsync();
        }

        public async Task<Rol> ObtenerPorIdAsync(Guid idRol)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var rol = await dbContext.Roles.FindAsync(idRol);
            if (rol == null)
                throw new RolNotFoundException(idRol);
            return rol;
        }        public async Task<IEnumerable<Rol>> ObtenerTodosAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            System.Diagnostics.Debug.WriteLine("RolRepository.ObtenerTodosAsync: Iniciando consulta");
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"RolRepository: Connection string: {dbContext.Database.GetConnectionString()}");
                
                // Probar conectividad antes de la consulta
                await dbContext.Database.OpenConnectionAsync();
                System.Diagnostics.Debug.WriteLine("RolRepository: Conexión a BD establecida correctamente");
                
                // Verificar si la tabla existe
                var canConnect = await dbContext.Database.CanConnectAsync();
                System.Diagnostics.Debug.WriteLine($"RolRepository: CanConnect = {canConnect}");
                
                // Prueba directa con SQL crudo para verificar conectividad
                try
                {
                    var countResult = await dbContext.Database.ExecuteSqlRawAsync("SELECT COUNT(*) FROM Roles");
                    System.Diagnostics.Debug.WriteLine($"RolRepository: Count directo SQL = {countResult}");
                }
                catch (Exception sqlEx)
                {
                    System.Diagnostics.Debug.WriteLine($"RolRepository: Error en SQL directo: {sqlEx.Message}");
                }
                
                var roles = await dbContext.Roles.ToListAsync();
                System.Diagnostics.Debug.WriteLine($"RolRepository.ObtenerTodosAsync: Consulta ejecutada, roles encontrados: {roles.Count}");
                
                foreach (var rol in roles)
                {
                    System.Diagnostics.Debug.WriteLine($"RolRepository - Rol: ID={rol.IdRol}, Nombre='{rol.Nombre}', Descripcion='{rol.Descripcion}'");
                }
                
                return roles;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en RolRepository.ObtenerTodosAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Roles.AnyAsync(r => r.Nombre == nombre);
        }
    }
}
