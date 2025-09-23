using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GestLog.Modules.GestionMantenimientos.Messages;

public class SeguimientosActualizadosMessage : ValueChangedMessage<bool>
{
    public SeguimientosActualizadosMessage(bool value = true) : base(value) { }
}

public class CronogramasActualizadosMessage : ValueChangedMessage<bool>
{
    public CronogramasActualizadosMessage(bool value = true) : base(value) { }
}

public class MantenimientosActualizadosMessage : ValueChangedMessage<bool>
{
    public MantenimientosActualizadosMessage(bool value = true) : base(value) { }
}

public class EjecucionesPlanesActualizadasMessage : ValueChangedMessage<bool>
{
    public EjecucionesPlanesActualizadasMessage(bool value = true) : base(value) { }
}
