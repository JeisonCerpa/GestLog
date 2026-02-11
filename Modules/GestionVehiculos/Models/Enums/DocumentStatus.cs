namespace GestLog.Modules.GestionVehiculos.Models.Enums
{
    /// <summary>
    /// Estado semántico de un documento de vehículo (se usa para indicar archivado además de vigencia por fecha)
    /// </summary>
    public enum DocumentStatus
    {
        Vigente = 0,
        Vencido = 1,
        SinVencimiento = 2,
        Archivado = 3
    }
}