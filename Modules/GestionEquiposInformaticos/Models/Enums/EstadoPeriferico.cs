using System.ComponentModel;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Enums
{    /// <summary>
    /// Estados posibles de un periférico informático
    /// </summary>
    public enum EstadoPeriferico
    {
        [Description("En uso")]
        EnUso = 1,

        [Description("Almacenado y Funcionando")]
        AlmacenadoFuncionando = 2,

        [Description("Dado de baja")]
        DadoDeBaja = 3,

        [Description("En Reparación")]
        EnReparacion = 4
    }
}
