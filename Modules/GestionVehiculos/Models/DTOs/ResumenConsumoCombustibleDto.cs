namespace GestLog.Modules.GestionVehiculos.Models.DTOs
{
    public class ResumenConsumoCombustibleDto
    {
        public int TotalRegistros { get; set; }
        public decimal TotalGalones { get; set; }
        public decimal TotalCosto { get; set; }
        public decimal PromedioCostoPorGalon { get; set; }
    }
}
