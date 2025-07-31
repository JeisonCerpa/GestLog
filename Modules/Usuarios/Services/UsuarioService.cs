using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Interfaces;
using Modules.Usuarios.Helpers;
using System.Linq;

namespace Modules.Usuarios.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IGestLogLogger _logger;
        private readonly IAuditoriaService _auditoriaService;

        public UsuarioService(IUsuarioRepository usuarioRepository, IGestLogLogger logger, IAuditoriaService auditoriaService)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        public async Task<Usuario> AsignarUsuarioAPersonaAsync(Guid idPersona, string nombreUsuario, string contraseña)
        {
            // Implementación pendiente
            return await Task.FromResult<Usuario>(null!);
        }

        public async Task RestablecerContraseñaAsync(Guid idUsuario, string nuevaContraseña)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public async Task AsignarRolesAsync(Guid idUsuario, IEnumerable<Guid> rolesIds)
        {
            try
            {
                using var db = new GestLog.Modules.DatabaseConnection.GestLogDbContextFactory().CreateDbContext(Array.Empty<string>());
                // Eliminar roles previos
                var existentes = db.UsuarioRoles.Where(ur => ur.IdUsuario == idUsuario);
                db.UsuarioRoles.RemoveRange(existentes);
                // Agregar nuevos roles
                foreach (var idRol in rolesIds.Distinct())
                {
                    db.UsuarioRoles.Add(new UsuarioRol { IdUsuario = idUsuario, IdRol = idRol });
                }
                await db.SaveChangesAsync();
                _logger.LogInformation($"Roles asignados a usuario {idUsuario}: {string.Join(",", rolesIds)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al asignar roles a usuario {idUsuario}: {ex.Message}");
                throw;
            }
        }

        public async Task AsignarPermisosAsync(Guid idUsuario, IEnumerable<Guid> permisosIds)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public async Task<Usuario> RegistrarUsuarioAsync(Usuario usuario)
        {
            try
            {
                if (await _usuarioRepository.ExisteNombreUsuarioAsync(usuario.NombreUsuario))
                    throw new UsuarioDuplicadoException(usuario.NombreUsuario);
                var result = await _usuarioRepository.AgregarAsync(usuario);
                _logger.LogInformation($"User registered: {usuario.NombreUsuario}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Usuario",
                    IdEntidad = result.IdUsuario,
                    Accion = "Crear",
                    UsuarioResponsable = "admin", // Reemplazar por usuario real
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Registro de usuario: {result.NombreUsuario} para persona {result.PersonaId}"
                });
                return result;
            }
            catch (UsuarioDuplicadoException)
            {
                _logger.LogWarning($"Duplicate username: {usuario.NombreUsuario}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering user: {ex.Message}");
                throw;
            }
        }

        public async Task<Usuario> EditarUsuarioAsync(Usuario usuario)
        {
            try
            {
                var result = await _usuarioRepository.ActualizarAsync(usuario);
                _logger.LogInformation($"User edited: {usuario.NombreUsuario}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Usuario",
                    IdEntidad = result.IdUsuario,
                    Accion = "Editar",
                    UsuarioResponsable = "admin", // Reemplazar por usuario real
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Edición de usuario: {result.NombreUsuario} para persona {result.PersonaId}"
                });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing user: {ex.Message}");
                throw;
            }
        }

        public async Task DesactivarUsuarioAsync(Guid idUsuario)
        {
            try
            {
                await _usuarioRepository.DesactivarAsync(idUsuario);
                _logger.LogInformation($"User deactivated: {idUsuario}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Usuario",
                    IdEntidad = idUsuario,
                    Accion = "Desactivar",
                    UsuarioResponsable = "admin", // Reemplazar por usuario real
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Desactivación de usuario: {idUsuario}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating user: {ex.Message}");
                throw;
            }
        }

        public async Task<Usuario> ObtenerUsuarioPorIdAsync(Guid idUsuario)
        {
            try
            {
                return await _usuarioRepository.ObtenerPorIdAsync(idUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Usuario>> BuscarUsuariosAsync(string filtro)
        {
            try
            {
                return await _usuarioRepository.BuscarAsync(filtro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching users: {ex.Message}");
                throw;
            }
        }
    }
}
