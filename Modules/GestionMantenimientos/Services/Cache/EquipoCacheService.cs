using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Interfaces.Cache;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Services.Core.Logging;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages.Equipos;

namespace GestLog.Modules.GestionMantenimientos.Services.Cache
{    /// <summary>
    /// Servicio de cache para equipos que optimiza las consultas y evita recargas innecesarias.
    /// </summary>
    public class EquipoCacheService : IEquipoCacheService, IDisposable
    {
        private readonly IEquipoService _equipoService;
        private readonly IGestLogLogger _logger;
        private List<EquipoDto>? _equiposCache;
        private DateTime _ultimaActualizacion = DateTime.MinValue;
        private const int CACHE_EXPIRY_MINUTES = 5; // Cache válido por 5 minutos

        public EquipoCacheService(IEquipoService equipoService, IGestLogLogger logger)
        {
            _equipoService = equipoService;
            _logger = logger;

            // Suscribirse a mensajes que invaliden el cache
            WeakReferenceMessenger.Default.Register<EquiposActualizadosMessage>(this, (r, m) => InvalidarCache());
            WeakReferenceMessenger.Default.Register<EquiposCambioEstadoMessage>(this, (r, m) => 
            {
                if (m.RequiereRecargaCompleta)
                    InvalidarCache();
                else
                    ActualizarEquipoEnCache(m.CodigoEquipo);
            });
        }

        /// <summary>
        /// Obtiene la lista de equipos, usando cache si está disponible y válido.
        /// </summary>
        public async Task<IEnumerable<EquipoDto>> GetEquiposAsync(bool forzarRecarga = false)
        {
            if (forzarRecarga || !CacheEsValido())
            {
                _logger.LogDebug("[EquipoCacheService] Recargando cache de equipos");
                await RecargarCacheAsync();
            }
            else
            {
                _logger.LogDebug("[EquipoCacheService] Usando cache de equipos (válido hasta {expiry})", 
                    _ultimaActualizacion.AddMinutes(CACHE_EXPIRY_MINUTES));
            }

            return _equiposCache ?? new List<EquipoDto>();
        }

        /// <summary>
        /// Obtiene un equipo específico por código, con optimización de cache.
        /// </summary>
        public async Task<EquipoDto?> GetEquipoPorCodigoAsync(string codigo)
        {
            if (!CacheEsValido())
            {
                await RecargarCacheAsync();
            }

            return _equiposCache?.FirstOrDefault(e => e.Codigo == codigo);
        }

        /// <summary>
        /// Invalida completamente el cache.
        /// </summary>
        public void InvalidarCache()
        {
            _logger.LogDebug("[EquipoCacheService] Cache invalidado");
            _equiposCache = null;
            _ultimaActualizacion = DateTime.MinValue;
        }

        /// <summary>
        /// Actualiza un equipo específico en el cache sin recargar todo.
        /// </summary>
        private async void ActualizarEquipoEnCache(string? codigoEquipo)
        {
            if (string.IsNullOrEmpty(codigoEquipo) || _equiposCache == null)
                return;

            try
            {
                var equipoActualizado = await _equipoService.GetByCodigoAsync(codigoEquipo);
                var indice = _equiposCache.FindIndex(e => e.Codigo == codigoEquipo);
                
                if (indice >= 0 && equipoActualizado != null)
                {
                    _equiposCache[indice] = equipoActualizado;
                    _logger.LogDebug("[EquipoCacheService] Equipo {codigo} actualizado en cache", codigoEquipo);
                }
                else if (equipoActualizado != null)
                {
                    _equiposCache.Add(equipoActualizado);
                    _logger.LogDebug("[EquipoCacheService] Equipo {codigo} agregado al cache", codigoEquipo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[EquipoCacheService] Error al actualizar equipo {codigo} en cache", codigoEquipo);
                InvalidarCache(); // En caso de error, invalidar todo el cache
            }
        }

        private bool CacheEsValido()
        {
            return _equiposCache != null && 
                   (DateTime.Now - _ultimaActualizacion).TotalMinutes < CACHE_EXPIRY_MINUTES;
        }

        private async Task RecargarCacheAsync()
        {
            try
            {
                var equipos = await _equipoService.GetAllAsync();
                _equiposCache = equipos.ToList();
                _ultimaActualizacion = DateTime.Now;
                _logger.LogInformation("[EquipoCacheService] Cache recargado con {count} equipos", _equiposCache.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquipoCacheService] Error al recargar cache de equipos");
                throw;
            }
        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.Unregister<EquiposActualizadosMessage>(this);
            WeakReferenceMessenger.Default.Unregister<EquiposCambioEstadoMessage>(this);
        }
    }
}
