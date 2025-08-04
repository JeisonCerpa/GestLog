using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Helpers;
using Modules.Usuarios.Interfaces;

namespace Modules.Usuarios.Services
{
    public class PermisoService : IPermisoService
    {
        private readonly IPermisoRepository _permisoRepository;
        private readonly IGestLogLogger _logger;
        private readonly IAuditoriaService _auditoriaService;

        public PermisoService(IPermisoRepository permisoRepository, IGestLogLogger logger, IAuditoriaService auditoriaService)
        {
            _permisoRepository = permisoRepository;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        public async Task<Permiso> CrearPermisoAsync(Permiso permiso)
        {
            try
            {
                if (await _permisoRepository.ExisteNombreAsync(permiso.Nombre))
                {
                    _logger.LogWarning($"Duplicate permission name: {permiso.Nombre}");
                    throw new PermisoDuplicadoException(permiso.Nombre);
                }
                var result = await _permisoRepository.AgregarAsync(permiso);
                _logger.LogInformation($"Permission registered: {permiso.Nombre}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Permiso",
                    IdEntidad = result.IdPermiso,
                    Accion = "Crear",
                    UsuarioResponsable = "Sistema",
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Permiso creado: {permiso.Nombre} ({permiso.IdPermiso})"
                });
                return result;
            }
            catch (PermisoDuplicadoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering permission: {ex.Message}");
                throw;
            }
        }

        public async Task<Permiso> EditarPermisoAsync(Permiso permiso)
        {
            try
            {
                var existente = await _permisoRepository.ObtenerPorIdAsync(permiso.IdPermiso);
                if (existente == null)
                {
                    _logger.LogWarning($"Permission not found: {permiso.IdPermiso}");
                    throw new PermisoNotFoundException(permiso.IdPermiso);
                }
                existente.Nombre = permiso.Nombre;
                existente.Descripcion = permiso.Descripcion;
                var result = await _permisoRepository.ActualizarAsync(existente);
                _logger.LogInformation($"Permission updated: {result.Nombre}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Permiso",
                    IdEntidad = result.IdPermiso,
                    Accion = "Editar",
                    UsuarioResponsable = "Sistema",
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Permiso editado: {permiso.Nombre} ({permiso.IdPermiso})"
                });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating permission: {ex.Message}");
                throw;
            }
        }

        public async Task EliminarPermisoAsync(Guid idPermiso)
        {
            try
            {
                var existente = await _permisoRepository.ObtenerPorIdAsync(idPermiso);
                if (existente == null)
                {
                    _logger.LogWarning($"Permission not found for delete: {idPermiso}");
                    throw new PermisoNotFoundException(idPermiso);
                }
                await _permisoRepository.EliminarAsync(idPermiso);
                _logger.LogInformation($"Permission deleted: {idPermiso}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Permiso",
                    IdEntidad = idPermiso,
                    Accion = "Eliminar",
                    UsuarioResponsable = "Sistema",
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Permiso eliminado: {existente.Nombre} ({idPermiso})"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting permission: {ex.Message}");
                throw;
            }
        }

        public async Task<Permiso> ObtenerPermisoPorIdAsync(Guid idPermiso)
        {
            try
            {
                var permiso = await _permisoRepository.ObtenerPorIdAsync(idPermiso);
                if (permiso == null)
                {
                    _logger.LogWarning($"Permission not found: {idPermiso}");
                    throw new PermisoNotFoundException(idPermiso);
                }
                return permiso;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting permission: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Permiso>> ObtenerTodosAsync()
        {
            try
            {
                return await _permisoRepository.ObtenerTodosAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting all permissions: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            return await _permisoRepository.ExisteNombreAsync(nombre);
        }

        public async Task<IEnumerable<Permiso>> ObtenerPorModuloAsync(string modulo)
        {
            try
            {
                return await _permisoRepository.ObtenerPorModuloAsync(modulo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting permissions by module: {ex.Message}");
                throw;
            }
        }
    }
}
