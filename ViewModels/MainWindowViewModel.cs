using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.Usuarios.ViewModels;
using GestLog.Messages; // Asegúrate de tener la referencia correcta para el Messenger
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly LoginViewModel _loginViewModel;
        private readonly ICurrentUserService _currentUserService = null!;
        private CurrentUserInfo _currentUser;

        [ObservableProperty]
        private bool _isAuthenticated = false;
        [ObservableProperty]
        private string _nombrePersona = string.Empty;        // Propiedad calculada que siempre obtiene el nombre actual
        public string NombrePersonaActual
        {
            get
            {                try
                {
                    var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                    var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                    return currentUserService?.Current?.FullName ?? string.Empty;
                }
                catch (Exception ex)
                {
                    var logger = GestLog.Services.Core.Logging.LoggingService.GetLogger<MainWindowViewModel>();
                    logger.Logger.LogError(ex, "Error al obtener el nombre del usuario actual: {Message}", ex.Message);
                    return string.Empty;
                }
            }
        }

        [ObservableProperty]
        private bool canAccessAdminPanel;
        [ObservableProperty]
        private bool canAccessUserManagement;
        [ObservableProperty]
        private bool canAccessErrorLog;

        public MainWindowViewModel(LoginViewModel loginViewModel, ICurrentUserService currentUserService)
        {
            _loginViewModel = loginViewModel;
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
            // Suscribirse a eventos y mensajes
            if (_currentUserService != null)
            {
                _currentUserService.CurrentUserChanged += (s, user) =>
                {
                    OnPropertyChanged(nameof(NombrePersonaActual));
                };
            }            WeakReferenceMessenger.Default.Register<UserLoggedInMessage>(this, (r, m) =>
            {
                if (m?.Value != null)
                {
                    OnPropertyChanged(nameof(NombrePersonaActual));
                    IsAuthenticated = true;
                    NotificarCambioNombrePersona();
                }
            });
            // --- NUEVO: Forzar notificación inicial para refrescar el binding en el primer render ---
            OnPropertyChanged(nameof(NombrePersonaActual));
        }
        [RelayCommand]
        public async Task CerrarSesionAsync()
        {
            await _loginViewModel.CerrarSesionAsync();
            IsAuthenticated = false;
            WeakReferenceMessenger.Default.Send(new ShowLoginViewMessage());
        }        public void SetAuthenticated(bool value, string? nombrePersona = null)
        {
            IsAuthenticated = value;
            if (value)
            {
                if (!string.IsNullOrEmpty(nombrePersona))
                {
                    NombrePersona = nombrePersona;
                }
                else
                {                    // Obtener el nombre desde el servicio de usuario actual
                    var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                    var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                    NombrePersona = currentUserService?.Current?.FullName ?? string.Empty;
                }
            }
            else
            {
                NombrePersona = string.Empty;
            }
        }        public void NotificarCambioNombrePersona()
        {
            OnPropertyChanged(nameof(NombrePersonaActual));
        }

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }

        private void RecalcularPermisos()
        {
            CanAccessAdminPanel = _currentUser.HasRole("Administrador");
            CanAccessUserManagement = _currentUser.HasPermission("Usuarios.GestionarUsuarios");
            CanAccessErrorLog = _currentUser.HasPermission("Herramientas.VerErrorLog");
        }
    }
}