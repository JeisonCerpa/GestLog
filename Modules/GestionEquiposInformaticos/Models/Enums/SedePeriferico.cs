using System.ComponentModel;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Enums
{
    /// <summary>
    /// Sedes disponibles para asignación de periféricos
    /// </summary>
    public enum SedePeriferico
    {
        [Description("Administrativa - Barranquilla")]
        AdministrativaBarranquilla = 1,

        [Description("Taller - Barranquilla")]
        TallerBarranquilla = 2,

        [Description("Bodega - Bayunca")]
        BodegaBayunca = 3
    }
}
