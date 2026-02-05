using System;
using System.Text;

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Utilities
{
    /// <summary>
    /// Servicio para manejar observaciones con timestamps automáticos
    /// Proporciona métodos para agregar, actualizar y formatear observaciones con trazabilidad temporal
    /// </summary>
    public static class ObservacionesConTimestampService
    {
        /// <summary>
        /// Formato del timestamp para las observaciones
        /// </summary>
        private const string TimestampFormat = "dd/MM/yyyy HH:mm:ss";

        /// <summary>
        /// Agrega una nueva observación con timestamp a un texto de observaciones existente
        /// </summary>
        /// <param name="observacionesActuales">Observaciones existentes (puede ser null o vacío)</param>
        /// <param name="nuevaObservacion">Texto de la nueva observación</param>
        /// <param name="timestamp">Timestamp personalizado (opcional, usa DateTime.Now si no se proporciona)</param>
        /// <returns>Observaciones con la nueva línea agregada</returns>
        public static string AgregarObservacionConTimestamp(
            string? observacionesActuales,
            string nuevaObservacion,
            DateTime? timestamp = null)
        {
            if (string.IsNullOrWhiteSpace(nuevaObservacion))
                return observacionesActuales ?? string.Empty;

            timestamp ??= DateTime.Now;
            var timestampFormateado = timestamp.Value.ToString(TimestampFormat);
            var linea = $"• [{timestampFormateado}] {nuevaObservacion.Trim()}";

            if (string.IsNullOrWhiteSpace(observacionesActuales))
                return linea;

            return $"{observacionesActuales}\n{linea}";
        }

        /// <summary>
        /// Agrega una observación indicando que el mantenimiento fue creado/registrado
        /// </summary>
        public static string AgregarObservacionRegistro(string? observacionesActuales, string descripcionFalla)
        {
            var obs = $"Mantenimiento registrado - Falla: {descripcionFalla}";
            return AgregarObservacionConTimestamp(observacionesActuales, obs);
        }

        /// <summary>
        /// Agrega una observación indicando que el mantenimiento fue enviado a reparación
        /// </summary>
        public static string AgregarObservacionEnviado(
            string? observacionesActuales,
            string proveedorAsignado,
            string? observacionesAdicionales = null)
        {
            var sb = new StringBuilder($"Enviado a reparación - Proveedor: {proveedorAsignado}");
            if (!string.IsNullOrWhiteSpace(observacionesAdicionales))
                sb.Append($". {observacionesAdicionales}");

            return AgregarObservacionConTimestamp(observacionesActuales, sb.ToString());
        }

        /// <summary>
        /// Agrega una observación indicando que el mantenimiento fue completado
        /// </summary>
        public static string AgregarObservacionCompletado(
            string? observacionesActuales,
            decimal? costoReparacion = null,
            int? periodoGarantia = null,
            string? observacionesAdicionales = null)
        {
            var sb = new StringBuilder("Mantenimiento completado");

            if (costoReparacion.HasValue)
                sb.Append($" - Costo: ${costoReparacion:N0}");

            if (periodoGarantia.HasValue && periodoGarantia > 0)
                sb.Append($" - Garantía: {periodoGarantia} días");

            if (!string.IsNullOrWhiteSpace(observacionesAdicionales))
                sb.Append($". {observacionesAdicionales}");

            return AgregarObservacionConTimestamp(observacionesActuales, sb.ToString());
        }

        /// <summary>
        /// Agrega una observación indicando que el mantenimiento fue cancelado
        /// </summary>
        public static string AgregarObservacionCancelado(
            string? observacionesActuales,
            string razonCancelacion)
        {
            var obs = $"Mantenimiento cancelado - Razón: {razonCancelacion}";
            return AgregarObservacionConTimestamp(observacionesActuales, obs);
        }

        /// <summary>
        /// Agrega una observación personalizada con timestamp
        /// </summary>
        public static string AgregarObservacion(
            string? observacionesActuales,
            string texto,
            string? accion = null)
        {
            var obs = accion == null ? texto : $"{accion}: {texto}";
            return AgregarObservacionConTimestamp(observacionesActuales, obs);
        }
    }
}
