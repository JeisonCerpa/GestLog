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

        public AuditoriaService(IAuditoriaRepository auditoriaRepository, IGestLogLogger logger)
        {
            _auditoriaRepository = auditoriaRepository;
            _logger = logger;
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
