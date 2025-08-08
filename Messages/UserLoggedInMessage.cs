using CommunityToolkit.Mvvm.Messaging.Messages;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.Messages
{
    /// <summary>
    /// Mensaje para notificar login exitoso y pasar el usuario autenticado
    /// </summary>
    public class UserLoggedInMessage : ValueChangedMessage<CurrentUserInfo>
    {
        public UserLoggedInMessage(CurrentUserInfo userInfo) : base(userInfo) { }
    }
}
