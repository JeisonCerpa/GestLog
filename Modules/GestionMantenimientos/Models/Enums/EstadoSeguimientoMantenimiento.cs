using System.ComponentModel;

namespace GestLog.Modules.GestionMantenimientos.Models.Enums
{
    public enum EstadoSeguimientoMantenimiento
    {
        [Description("Pendiente")]
        Pendiente = 0, // No realizado y la semana aún no ha terminado
        [Description("Realizado en tiempo")]
        RealizadoEnTiempo = 1, // Realizado dentro de la semana correspondiente
        [Description("Realizado fuera de tiempo")]
        RealizadoFueraDeTiempo = 2, // Realizado después de la semana programada
        [Description("Atrasado")]
        Atrasado = 3 // No realizado y la semana ya terminó
    }
}
