namespace GestLog.Modules.GestionVehiculos.Models.Enums
{
    /// <summary>
    /// Estados posibles de una ejecución de mantenimiento
    /// </summary>
    public enum EstadoEjecucion
    {
        /// <summary>
        /// Ejecución programada pero no realizada
        /// </summary>
        Pendiente = 1,

        /// <summary>
        /// Ejecución realizada correctamente
        /// </summary>
        Completado = 2,

        /// <summary>
        /// Ejecución cancelada
        /// </summary>
        Cancelado = 3
    }
}
