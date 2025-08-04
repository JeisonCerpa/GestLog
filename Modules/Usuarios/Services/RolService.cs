using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Helpers;
using Modules.Usuarios.Interfaces;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;

namespace Modules.Usuarios.Services
{    public class RolService : IRolService
    {
        private readonly IRolRepository _rolRepository;
        private readonly IGestLogLogger _logger;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IRolPermisoRepository _rolPermisoRepository;
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public RolService(IRolRepository rolRepository, IGestLogLogger logger, IAuditoriaService auditoriaService, IRolPermisoRepository rolPermisoRepository, IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _rolRepository = rolRepository;
            _logger = logger;
            _auditoriaService = auditoriaService;
            _rolPermisoRepository = rolPermisoRepository;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Rol> CrearRolAsync(Rol rol)
        {
            try
            {
                if (await _rolRepository.ExisteNombreAsync(rol.Nombre))
                {
                    _logger.LogWarning($"Duplicate role name: {rol.Nombre}");
                    throw new RolDuplicadoException(rol.Nombre);
                }
                var result = await _rolRepository.AgregarAsync(rol);
                _logger.LogInformation($"Role registered: {rol.Nombre}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Rol",
                    IdEntidad = result.IdRol,
                    Accion = "Crear",
                    UsuarioResponsable = "Sistema",
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Rol creado: {rol.Nombre} ({rol.IdRol})"
                });
                return result;
            }
            catch (RolDuplicadoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering role: {ex.Message}");
                throw;
            }
        }

        public async Task<Rol> EditarRolAsync(Rol rol)
        {
            try
            {
                var existente = await _rolRepository.ObtenerPorIdAsync(rol.IdRol);
                if (existente == null)
                {
                    _logger.LogWarning($"Rol not found: {rol.IdRol}");
                    throw new RolNotFoundException(rol.IdRol);
                }
                if (await _rolRepository.ExisteNombreAsync(rol.Nombre) && existente.Nombre != rol.Nombre)
                {
                    _logger.LogWarning($"Duplicate role name on edit: {rol.Nombre}");
                    throw new RolDuplicadoException(rol.Nombre);
                }
                var result = await _rolRepository.ActualizarAsync(rol);
                _logger.LogInformation($"Role edited: {rol.Nombre}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Rol",
                    IdEntidad = result.IdRol,
                    Accion = "Editar",
                    UsuarioResponsable = "Sistema",
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Rol editado: {rol.Nombre} ({rol.IdRol})"
                });
                return result;
            }
            catch (RolDuplicadoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing role: {ex.Message}");
                throw;
            }
        }

        public async Task EliminarRolAsync(Guid idRol)
        {
            try
            {
                var existente = await _rolRepository.ObtenerPorIdAsync(idRol);
                if (existente == null)
                {
                    _logger.LogWarning($"Rol not found for delete: {idRol}");
                    throw new RolNotFoundException(idRol);
                }
                await _rolRepository.EliminarAsync(idRol);
                _logger.LogInformation($"Role deleted: {idRol}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Rol",
                    IdEntidad = idRol,
                    Accion = "Eliminar",
                    UsuarioResponsable = "Sistema",
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Rol eliminado: {existente.Nombre} ({idRol})"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting role: {ex.Message}");
                throw;
            }
        }

        public async Task<Rol> ObtenerRolPorIdAsync(Guid idRol)
        {
            try
            {
                var rol = await _rolRepository.ObtenerPorIdAsync(idRol);
                if (rol == null)
                {
                    _logger.LogWarning($"Rol not found: {idRol}");
                    throw new RolNotFoundException(idRol);
                }
                return rol;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting role: {ex.Message}");
                throw;
            }
        }        public async Task<IEnumerable<Rol>> ObtenerTodosAsync()
        {
            try
            {
                _logger.LogInformation("RolService.ObtenerTodosAsync: Iniciando consulta de roles");
                System.Diagnostics.Debug.WriteLine("RolService.ObtenerTodosAsync: Iniciando consulta de roles");
                
                var roles = await _rolRepository.ObtenerTodosAsync();
                var rolesLista = roles.ToList(); // Materializar la consulta para obtener el count
                
                _logger.LogInformation($"RolService.ObtenerTodosAsync: Se obtuvieron {rolesLista.Count} roles de la base de datos");
                System.Diagnostics.Debug.WriteLine($"RolService.ObtenerTodosAsync: Se obtuvieron {rolesLista.Count} roles de la base de datos");
                
                // Log de cada rol para debugging
                foreach (var rol in rolesLista)
                {
                    System.Diagnostics.Debug.WriteLine($"Rol encontrado: ID={rol.IdRol}, Nombre='{rol.Nombre}', Descripcion='{rol.Descripcion}'");
                }
                
                return rolesLista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting all roles: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ERROR en RolService.ObtenerTodosAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            return await _rolRepository.ExisteNombreAsync(nombre);
        }

        public async Task AsignarPermisosARolAsync(Guid idRol, IEnumerable<Guid> permisosIds)
        {
            await _rolPermisoRepository.AsignarPermisosAsync(idRol, permisosIds);        _logger.LogInformation($"Permisos asignados al rol {idRol}: {string.Join(",", permisosIds)}");
        }
        
        public async Task<IEnumerable<Permiso>> ObtenerPermisosDeRolAsync(Guid idRol)
        {
            try
            {
                var rolPermisos = await _rolPermisoRepository.ObtenerPorRolAsync(idRol);
                var permisosIds = rolPermisos.Select(rp => rp.IdPermiso).ToList();
                
                if (!permisosIds.Any())
                    return new List<Permiso>();
                
                // Obtener permisos completos de la base de datos
                using var dbContext = _dbContextFactory.CreateDbContext();
                var permisos = await dbContext.Permisos
                    .Where(p => permisosIds.Contains(p.IdPermiso))
                    .ToListAsync();
                
                return permisos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting permissions for role {idRol}: {ex.Message}");
                throw;
            }
        }
    }
}
