using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
using System;

namespace GestLog.ViewModels.Tools
{
    public partial class HerramientasViewModel : ObservableObject
    {
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;

        [ObservableProperty]
        private bool canAccessDaaterProcessor;
        [ObservableProperty]
        private bool canAccessGestionCartera;
        [ObservableProperty]
        private bool canAccessEnvioCatalogo;
        [ObservableProperty]
        private bool canAccessGestionMantenimientos;
        [ObservableProperty]
        private bool canAccessErrorLog;
        [ObservableProperty]
        private bool canAccessGestionIdentidadCatalogos;
        [ObservableProperty]
        private bool canAccessGestionEquipos;
        [ObservableProperty]
        private bool canAccessEquiposInformaticos;
        [ObservableProperty]
        private bool canAccessGestionVehiculos;

        public HerramientasViewModel(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
        }

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }

        private void RecalcularPermisos()
        {
            CanAccessDaaterProcessor = _currentUser.HasPermission("Herramientas.AccederDaaterProccesor");
            CanAccessGestionCartera = _currentUser.HasPermission("Herramientas.AccederGestionCartera");
            CanAccessEnvioCatalogo = _currentUser.HasPermission("Herramientas.AccederEnvioCatalogo");
            CanAccessGestionMantenimientos = _currentUser.HasPermission("Herramientas.AccederGestionMantenimientos");
            CanAccessErrorLog = _currentUser.HasPermission("Herramientas.VerErrorLog");
            CanAccessGestionIdentidadCatalogos = _currentUser.HasPermission("Herramientas.AccederGestionIdentidadCatalogos");
            CanAccessGestionEquipos = _currentUser.HasPermission("Herramientas.AccederGestionEquipos");
            CanAccessGestionVehiculos = _currentUser.HasPermission("Herramientas.AccederGestionVehiculos");
            CanAccessEquiposInformaticos = _currentUser.HasPermission("Herramientas.AccederEquiposInformaticos");
        }
    }
}
