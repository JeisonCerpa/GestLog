using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Helpers;
using Modules.Usuarios.Interfaces;

namespace Modules.Usuarios.Services
{
    public class RolService : IRolService
    {
        private readonly IRolRepository _rolRepository;
        private readonly IGestLogLogger _logger;
        private readonly IAuditoriaService _auditoriaService;

        public RolService(IRolRepository rolRepository, IGestLogLogger logger, IAuditoriaService auditoriaService)
        {
            _rolRepository = rolRepository;
            _logger = logger;
            _auditoriaService = auditoriaService;
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
        }

        public async Task<IEnumerable<Rol>> ObtenerTodosAsync()
        {
            try
            {
                return await _rolRepository.ObtenerTodosAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting all roles: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            return await _rolRepository.ExisteNombreAsync(nombre);
        }
    }
}
