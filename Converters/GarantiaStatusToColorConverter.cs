using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters;

/// <summary>
/// MultiValueConverter que retorna un color basado en el estado de garantía (Vigente/Vencida/Sin garantía)
/// Recibe FechaCompletado y PeriodoGarantia, y retorna el color correspondiente
/// </summary>
public class GarantiaStatusToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            // Validar que recibimos exactamente 2 valores
            if (values == null || values.Length < 2)
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(157, 157, 156)); // Gris (#9D9D9C)
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

            // Si falta FechaCompletado, no hay garantía (gris)
            if (!fechaCompletado.HasValue)
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(157, 157, 156)); // Gris (#9D9D9C)
            }

            // Si falta PeriodoGarantia, no hay garantía (gris)
            if (!periodoGarantia.HasValue)
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(157, 157, 156)); // Gris (#9D9D9C)
            }

            // En este punto, tenemos ambos valores válidos
            DateTime fechaVencimiento = fechaCompletado.Value.AddDays(periodoGarantia.Value);
            DateTime hoy = DateTime.Today;

            if (hoy <= fechaVencimiento)
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96)); // Verde (#27AE60) - Vigente
            }
            else
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(192, 57, 43)); // Rojo (#C0392B) - Vencida
            }
        }
        catch (Exception ex)
        {
            // Log silencioso del error para debugging
            System.Diagnostics.Debug.WriteLine($"Error en GarantiaStatusToColorConverter: {ex.Message}");
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(157, 157, 156));
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("GarantiaStatusToColorConverter no admite conversión bidireccional.");
    }
}
