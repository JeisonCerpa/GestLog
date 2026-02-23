using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GestLog.Modules.GestionVehiculos.Messages.UI
{
    /// <summary>
    /// Mensaje de UI genérico para notificaciones al usuario
    /// </summary>
    public class NotificationMessage : ValueChangedMessage<string>
    {
        public NotificationType Type { get; set; }

        public NotificationMessage(string message, NotificationType type = NotificationType.Info) 
            : base(message)
        {
            Type = type;
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
