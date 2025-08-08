using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GestLog.Messages
{
    public class ShowLoginViewMessage : ValueChangedMessage<bool>
    {
        public ShowLoginViewMessage() : base(true) { }
    }
}
