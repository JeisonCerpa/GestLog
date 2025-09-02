using System.ComponentModel;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Enums
{
    public enum EstadoEquipoInformatico
    {
        [Description("Activo")]
        Activo = 1,
        
        [Description("En Mantenimiento")]
        EnMantenimiento = 2,
        
        [Description("Disponible")]
        Disponible = 3,
        
        [Description("Dañado")]
        Danado = 4,
        
        [Description("De Baja")]
        DeBaja = 5,
        
        [Description("En Reparación")]
        EnReparacion = 6
    }
}
