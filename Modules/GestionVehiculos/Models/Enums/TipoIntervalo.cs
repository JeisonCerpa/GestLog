namespace GestLog.Modules.GestionVehiculos.Models.Enums
{
    /// <summary>
    /// Define cómo se aplica el intervalo de mantenimiento
    /// </summary>
    public enum TipoIntervalo
    {
        /// <summary>
        /// El mantenimiento se ejecuta cuando ocurra lo primero (km o días)
        /// </summary>
        Primero = 1,

        /// <summary>
        /// El mantenimiento se ejecuta cuando se cumplan ambos intervalos (km Y días)
        /// </summary>
        Ambos = 2,

        /// <summary>
        /// El mantenimiento se ejecuta cuando ocurra lo último (km o días)
        /// </summary>
        Ultimo = 3
    }
}
