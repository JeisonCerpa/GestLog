using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.ViewModels.Tools
{
    public partial class HerramientasViewModel : ObservableObject
    {
        private readonly CurrentUserInfo _currentUser;

        public bool CanAccessDaaterProcessor => _currentUser.HasPermission("Herramientas.AccederDaaterProccesor");

        public HerramientasViewModel(CurrentUserInfo currentUser)
        {
            _currentUser = currentUser;
        }
    }
}
