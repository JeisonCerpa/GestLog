using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Interfaces;

namespace Modules.Usuarios.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly IAuditoriaRepository _auditoriaRepository;
        private readonly IGestLogLogger _logger;
        private readonly GestLog.Modules.Usuarios.Interfaces.ICurrentUserService _currentUserService;

        public AuditoriaService(
            IAuditoriaRepository auditoriaRepository,
            IGestLogLogger logger,
            GestLog.Modules.Usuarios.Interfaces.ICurrentUserService currentUserService)
        {
            _auditoriaRepository = auditoriaRepository;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task RegistrarCambioAsync(string entidadAfectada, string claveEntidad, string accion, string detalle)
        {
            var auditoria = new Auditoria
            {
                IdAuditoria = Guid.NewGuid(),
                EntidadAfectada = entidadAfectada,
                IdEntidad = Guid.Empty,
                ClaveEntidad = claveEntidad,
                Accion = accion,
                UsuarioResponsable = !string.IsNullOrWhiteSpace(_currentUserService.Current?.FullName)
                    ? _currentUserService.Current!.FullName
                    : (_currentUserService.Current?.Username ?? "Sistema"),
                FechaHora = DateTime.Now,
                Detalle = detalle
            };

            // ponytail: la auditoría no debe impedir guardar el cambio de negocio; se registra el fallo y se sigue.
            try
            {
                await _auditoriaRepository.RegistrarAsync(auditoria);
                _logger.LogDebug($"Audit event registered: {entidadAfectada}/{claveEntidad} - {accion}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registrando auditoría de {entidadAfectada}/{claveEntidad}: {ex.Message}");
            }
        }

        public async Task<IEnumerable<Auditoria>> ObtenerHistorialPorClaveAsync(string entidadAfectada, string claveEntidad)
        {
            try
            {
                return await _auditoriaRepository.ObtenerPorClaveAsync(entidadAfectada, claveEntidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting audit history by key: {ex.Message}");
                throw new Exception("Error al obtener el historial de auditoría. Por favor intente nuevamente o contacte al soporte.", ex);
            }
        }

        public async Task RegistrarEventoAsync(Auditoria auditoria)
        {            try
            {
                await _auditoriaRepository.RegistrarAsync(auditoria);
                // Reducir ruido: degradar a Debug para evitar spam en logs normales
                _logger.LogDebug($"Audit event registered: {auditoria.IdAuditoria}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering audit event: {ex.Message}");
                throw new Exception("Error al registrar el evento de auditoría. Por favor intente nuevamente o contacte al soporte.", ex);
            }
        }

        public async Task<IEnumerable<Auditoria>> ObtenerHistorialPorEntidadAsync(string entidadAfectada, Guid idEntidad)
        {
            try
            {
                return await _auditoriaRepository.ObtenerPorEntidadAsync(entidadAfectada, idEntidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting audit history by entity: {ex.Message}");
                throw new Exception("Error al obtener el historial de auditoría. Por favor intente nuevamente o contacte al soporte.", ex);
            }
        }

        public async Task<IEnumerable<Auditoria>> BuscarAsync(string? entidadAfectada, string? usuarioResponsable,
            DateTime? desde, DateTime? hasta, string? texto, int maxResultados = 500)
        {
            try
            {
                return await _auditoriaRepository.BuscarAsync(entidadAfectada, usuarioResponsable, desde, hasta, texto, maxResultados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error buscando en auditoría: {ex.Message}");
                throw new Exception("Error al consultar la auditoría. Por favor intente nuevamente o contacte al soporte.", ex);
            }
        }

        public async Task<IEnumerable<string>> ObtenerEntidadesAsync()
        {
            try
            {
                return await _auditoriaRepository.ObtenerEntidadesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo entidades auditadas: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        public async Task<IEnumerable<Auditoria>> ObtenerHistorialPorUsuarioAsync(string usuarioResponsable)
        {
            try
            {
                return await _auditoriaRepository.ObtenerPorUsuarioAsync(usuarioResponsable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting audit history by user: {ex.Message}");
                throw new Exception("Error al obtener el historial de auditoría del usuario. Por favor intente nuevamente o contacte al soporte.", ex);
            }
        }
    }
}
