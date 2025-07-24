using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Helpers;
using Modules.Usuarios.Interfaces;

namespace Modules.Usuarios.Services
{
    public class CargoService : ICargoService
    {
        private readonly ICargoRepository _cargoRepository;
        private readonly IGestLogLogger _logger;
        private readonly IAuditoriaService _auditoriaService;

        public CargoService(ICargoRepository cargoRepository, IGestLogLogger logger, IAuditoriaService auditoriaService)
        {
            _cargoRepository = cargoRepository;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        public async Task<Cargo> CrearCargoAsync(Cargo cargo)
        {
            try
            {
                if (await _cargoRepository.ExisteNombreAsync(cargo.Nombre))
                {
                    _logger.LogWarning($"Duplicate role name: {cargo.Nombre}");
                    throw new CargoDuplicadoException(cargo.Nombre);
                }
                var result = await _cargoRepository.AgregarAsync(cargo);
                _logger.LogInformation($"Role registered: {cargo.Nombre}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Cargo",
                    IdEntidad = result.IdCargo,
                    Accion = "Crear",
                    UsuarioResponsable = "Sistema",
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Cargo creado: {cargo.Nombre} ({cargo.IdCargo})"
                });
                return result;
            }
            catch (CargoDuplicadoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering role: {ex.Message}");
                throw;
            }
        }

        public async Task<Cargo> EditarCargoAsync(Cargo cargo)
        {
            try
            {
                var existente = await _cargoRepository.ObtenerPorIdAsync(cargo.IdCargo);
                if (existente == null)
                {
                    _logger.LogWarning($"Cargo not found: {cargo.IdCargo}");
                    throw new CargoNotFoundException(cargo.IdCargo);
                }
                if (await _cargoRepository.ExisteNombreAsync(cargo.Nombre) && existente.Nombre != cargo.Nombre)
                {
                    _logger.LogWarning($"Duplicate role name on edit: {cargo.Nombre}");
                    throw new CargoDuplicadoException(cargo.Nombre);
                }
                var result = await _cargoRepository.ActualizarAsync(cargo);
                _logger.LogInformation($"Role edited: {cargo.Nombre}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Cargo",
                    IdEntidad = result.IdCargo,
                    Accion = "Editar",
                    UsuarioResponsable = "Sistema",
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Cargo editado: {cargo.Nombre} ({cargo.IdCargo})"
                });
                return result;
            }
            catch (CargoDuplicadoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing role: {ex.Message}");
                throw;
            }
        }

        public async Task EliminarCargoAsync(Guid idCargo)
        {
            try
            {
                var existente = await _cargoRepository.ObtenerPorIdAsync(idCargo);
                if (existente == null)
                {
                    _logger.LogWarning($"Cargo not found for delete: {idCargo}");
                    throw new CargoNotFoundException(idCargo);
                }
                await _cargoRepository.EliminarAsync(idCargo);
                _logger.LogInformation($"Role deleted: {idCargo}");
                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Cargo",
                    IdEntidad = idCargo,
                    Accion = "Eliminar",
                    UsuarioResponsable = "Sistema",
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Cargo eliminado: {existente.Nombre} ({idCargo})"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting role: {ex.Message}");
                throw;
            }
        }

        public async Task<Cargo> ObtenerCargoPorIdAsync(Guid idCargo)
        {
            try
            {
                var cargo = await _cargoRepository.ObtenerPorIdAsync(idCargo);
                if (cargo == null)
                {
                    _logger.LogWarning($"Cargo not found: {idCargo}");
                    throw new CargoNotFoundException(idCargo);
                }
                return cargo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting role: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Cargo>> ObtenerTodosAsync()
        {
            try
            {
                return await _cargoRepository.ObtenerTodosAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting all roles: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            return await _cargoRepository.ExisteNombreAsync(nombre);
        }
    }
}
