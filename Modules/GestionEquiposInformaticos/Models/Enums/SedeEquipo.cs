using System.ComponentModel;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Enums
{
    public enum SedeEquipo
    {
        [Description("Sede Principal")]
        SedePrincipal = 1,
        
        [Description("Sucursal Norte")]
        SucursalNorte = 2,
        
        [Description("Sucursal Sur")]
        SucursalSur = 3,
        
        [Description("Oficina Central")]
        OficinaCentral = 4,
        
        [Description("Trabajo Remoto")]
        TrabajoRemoto = 5,
        
        [Description("Almacén")]
        Almacen = 6
    }
}
