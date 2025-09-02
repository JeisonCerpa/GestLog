using System.ComponentModel;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Enums
{
    public enum TipoDisco
    {
        [Description("HDD")]
        HDD = 1,
        
        [Description("SSD")]
        SSD = 2,
        
        [Description("NVMe")]
        NVMe = 3,
        
        [Description("eMMC")]
        eMMC = 4,
        
        [Description("Otro")]
        Otro = 99
    }
}
