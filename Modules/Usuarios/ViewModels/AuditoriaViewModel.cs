using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Interfaces;

namespace GestLog.Modules.Usuarios.ViewModels
{
    /// <summary>
    /// Consulta del historial de auditoría de todos los módulos, con filtros combinables.
    /// </summary>
    public partial class AuditoriaViewModel : ObservableObject
    {
        private const int MaxResultados = 500;

        private readonly IAuditoriaService _auditoriaService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private ObservableCollection<Auditoria> registros = new();

        [ObservableProperty]
        private ObservableCollection<string> entidades = new();

        [ObservableProperty]
        private string? entidadSeleccionada;

        [ObservableProperty]
        private string usuarioFiltro = string.Empty;

        [ObservableProperty]
        private DateTime? desde;

        [ObservableProperty]
        private DateTime? hasta;

        [ObservableProperty]
        private string textoFiltro = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool hayMasResultados;

        public AuditoriaViewModel(IAuditoriaService auditoriaService, IGestLogLogger logger)
        {
            _auditoriaService = auditoriaService ?? throw new ArgumentNullException(nameof(auditoriaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InicializarAsync()
        {
            await CargarEntidadesAsync();
            await BuscarAsync();
        }

        private async Task CargarEntidadesAsync()
        {
            try
            {
                var lista = await _auditoriaService.ObtenerEntidadesAsync();
                Entidades = new ObservableCollection<string>(lista);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuditoriaViewModel] Error cargando entidades auditadas");
            }
        }

        [RelayCommand]
        public async Task BuscarAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Consultando…";

                var resultados = (await _auditoriaService.BuscarAsync(
                    EntidadSeleccionada,
                    UsuarioFiltro,
                    Desde,
                    Hasta,
                    TextoFiltro,
                    MaxResultados)).ToList();

                Registros = new ObservableCollection<Auditoria>(resultados);
                HayMasResultados = resultados.Count == MaxResultados;
                StatusMessage = HayMasResultados
                    ? $"Mostrando los {MaxResultados} eventos más recientes. Afine los filtros para ver el resto."
                    : $"{resultados.Count} evento(s) encontrado(s).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuditoriaViewModel] Error consultando auditoría");
                StatusMessage = "No se pudo consultar el historial de auditoría.";
                Registros = new ObservableCollection<Auditoria>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LimpiarFiltrosAsync()
        {
            EntidadSeleccionada = null;
            UsuarioFiltro = string.Empty;
            TextoFiltro = string.Empty;
            Desde = null;
            Hasta = null;
            await BuscarAsync();
        }
    }
}
