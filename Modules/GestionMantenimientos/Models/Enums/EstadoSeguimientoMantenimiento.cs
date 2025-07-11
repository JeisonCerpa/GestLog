namespace GestLog.Modules.GestionMantenimientos.Models.Enums
{
    public enum EstadoSeguimientoMantenimiento
    {
        Pendiente = 0, // No realizado y la semana aún no ha terminado
        RealizadoEnTiempo = 1, // Realizado dentro de la semana correspondiente
        RealizadoFueraDeTiempo = 2, // Realizado después de la semana programada
        Atrasado = 3 // No realizado y la semana ya terminó
    }
}
