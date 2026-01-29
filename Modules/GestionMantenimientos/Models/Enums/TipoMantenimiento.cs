using System.ComponentModel;

namespace GestLog.Modules.GestionMantenimientos.Models.Enums
{
    public enum TipoMantenimiento
    {
        [Description("Preventivo")]
        Preventivo = 1,
        [Description("Correctivo")]
        Correctivo = 2,
        [Description("Predictivo")]
        Predictivo = 3,
        [Description("Otro")]
        Otro = 99
    }
}
