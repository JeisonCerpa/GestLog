using System;
using System.Text;

namespace GestLog.Modules.GestionVehiculos.Services.Utilities
{
    /// <summary>
    /// Construye historial textual de observaciones para correctivos con marca temporal.
    /// </summary>
    public static class ObservacionesCorrectivoTimelineService
    {
        private const string DateFormat = "dd/MM/yyyy";

        private static string Append(string? actual, string line)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(actual))
            {
                return trimmed;
            }

            return $"{actual}\n{trimmed}";
        }

        public static string RegistrarFalla(string? actual, string descripcionFalla, DateTime? when = null)
        {
            var dt = (when ?? DateTime.Now).ToString(DateFormat);
            var line = $"- [{dt}] Falla reportada - {descripcionFalla}";
            return Append(actual, line);
        }

        public static string EnviarAReparacion(string? actual, string proveedor, string? observaciones, DateTime? when = null)
        {
            var dt = (when ?? DateTime.Now).ToString(DateFormat);
            var sb = new StringBuilder($"- [{dt}] Enviado a reparación - Proveedor: {proveedor}");
            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                sb.Append($" - {observaciones.Trim()}");
            }
            return Append(actual, sb.ToString());
        }

        public static string Completar(
            string? actual,
            string? responsable,
            string? proveedor,
            decimal? costo,
            string? rutaFactura,
            string? observaciones,
            DateTime? when = null)
        {
            var dt = (when ?? DateTime.Now).ToString(DateFormat);
            var sb = new StringBuilder($"- [{dt}] Completado");

            if (!string.IsNullOrWhiteSpace(responsable))
            {
                sb.Append($" - Responsable: {responsable.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(proveedor))
            {
                sb.Append($" - Proveedor: {proveedor.Trim()}");
            }

            if (costo.HasValue)
            {
                sb.Append($" - Costo: ${costo.Value:N0}");
            }

            if (!string.IsNullOrWhiteSpace(rutaFactura))
            {
                sb.Append($" - Factura: {rutaFactura.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                sb.Append($" - {observaciones.Trim()}");
            }

            return Append(actual, sb.ToString());
        }
    }
}
