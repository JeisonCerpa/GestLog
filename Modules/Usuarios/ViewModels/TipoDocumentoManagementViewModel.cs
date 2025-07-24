using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GestLog.Modules.Usuarios.ViewModels
{
    public partial class TipoDocumentoManagementViewModel : ObservableObject
    {
        private readonly ITipoDocumentoRepository _tipoDocumentoRepository;

        [ObservableProperty]
        private ObservableCollection<TipoDocumento> tiposDocumento;

        [ObservableProperty]
        private TipoDocumento? tipoDocumentoSeleccionado;

        [ObservableProperty]
        private string mensajeError = string.Empty;

        public TipoDocumentoManagementViewModel(ITipoDocumentoRepository tipoDocumentoRepository)
        {
            _tipoDocumentoRepository = tipoDocumentoRepository;
            TiposDocumento = new ObservableCollection<TipoDocumento>(_tipoDocumentoRepository.ObtenerTodosAsync().Result);
        }

        private async Task BuscarTiposDocumentoAsync()
        {
            TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task BuscarTiposDocumentoAsync(string filtro)
        {
            var todos = await _tipoDocumentoRepository.ObtenerTodosAsync();
            if (string.IsNullOrWhiteSpace(filtro))
            {
                TiposDocumento = new ObservableCollection<TipoDocumento>(todos);
            }
            else
            {
                var filtrados = todos.FindAll(td => td.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) || (td.Descripcion != null && td.Descripcion.Contains(filtro, StringComparison.OrdinalIgnoreCase)));
                TiposDocumento = new ObservableCollection<TipoDocumento>(filtrados);
            }
        }

        [RelayCommand]
        public async Task EditarTipoDocumentoAsync()
        {
            if (TipoDocumentoSeleccionado == null) return;
            await _tipoDocumentoRepository.ActualizarAsync(TipoDocumentoSeleccionado);
            await BuscarTiposDocumentoAsync();
        }

        [RelayCommand]
        public async Task DesactivarTipoDocumentoAsync()
        {
            if (TipoDocumentoSeleccionado == null) return;
            await _tipoDocumentoRepository.EliminarAsync(TipoDocumentoSeleccionado.IdTipoDocumento);
            await BuscarTiposDocumentoAsync();
        }

        [RelayCommand]
        public void AbrirRegistroTipoDocumento()
        {
            // Abrir ventana modal para registrar nuevo tipo de documento
        }
    }
}
