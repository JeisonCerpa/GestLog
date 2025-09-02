using System.ComponentModel;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Enums
{
    public enum TipoRam
    {
        [Description("DDR3")]
        DDR3 = 1,
        
        [Description("DDR4")]
        DDR4 = 2,
        
        [Description("DDR5")]
        DDR5 = 3,
        
        [Description("LPDDR4")]
        LPDDR4 = 4,
        
        [Description("LPDDR5")]
        LPDDR5 = 5,
        
        [Description("Otro")]
        Otro = 99
    }
}
