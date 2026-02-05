using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters;

/// <summary>
/// MultiValueConverter que determina el estado de garantía basado en FechaCompletado y PeriodoGarantia
/// Retorna: "Vigente" si la garantía aún está activa, "Vencida" si expiró, "Sin garantía" si no aplica
/// </summary>
public class GarantiaStatusConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            // Validar que recibimos exactamente 2 valores
            if (values == null || values.Length < 2)
            {
                return "Sin garantía";
            }

            var fechaCompletadoObj = values[0];
            var periodoGarantiaObj = values[1];

            // Extraer valores de forma defensiva
            DateTime? fechaCompletado = null;
            int? periodoGarantia = null;

            // Procesar FechaCompletado
            if (fechaCompletadoObj != System.Windows.Data.Binding.DoNothing && 
                fechaCompletadoObj != null && 
                fechaCompletadoObj is DateTime dt)
            {
                // Solo aceptar si es una fecha válida y no es MinValue o default
                if (dt != DateTime.MinValue && dt != default(DateTime) && dt.Year > 1900)
                {
                    fechaCompletado = dt;
                }
            }

            // Procesar PeriodoGarantia
            if (periodoGarantiaObj != System.Windows.Data.Binding.DoNothing && 
                periodoGarantiaObj != null && 
                periodoGarantiaObj is int dias)
            {
                if (dias > 0)
                {
                    periodoGarantia = dias;
                }
            }

            // Si falta FechaCompletado, no hay garantía
            if (!fechaCompletado.HasValue)
            {
                return "Sin garantía";
            }

            // Si falta PeriodoGarantia, no hay garantía
            if (!periodoGarantia.HasValue)
            {
                return "Sin garantía";
            }

            // En este punto, tenemos ambos valores válidos
            DateTime fechaVencimiento = fechaCompletado.Value.AddDays(periodoGarantia.Value);
            DateTime hoy = DateTime.Today;

            if (hoy <= fechaVencimiento)
            {
                return "Vigente";
            }
            else
            {
                return "Vencida";
            }
        }
        catch (Exception ex)
        {
            // Log silencioso del error para debugging
            System.Diagnostics.Debug.WriteLine($"Error en GarantiaStatusConverter: {ex.Message}");
            return "Sin garantía";
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("GarantiaStatusConverter no admite conversión bidireccional.");
    }
}
