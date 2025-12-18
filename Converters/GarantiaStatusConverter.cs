using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters;

/// <summary>
/// MultiValueConverter que determina el estado de garantía basado en FechaCompletado y PeriodoGarantia
/// Retorna: "Vigente" si la garantía aún está activa, "Vencida" si expiró, "Sin garantía" si no aplica
/// </summary>
public class GarantiaStatusConverter : IMultiValueConverter
{    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (values.Length >= 2)
            {
                var fechaCompletado = values[0];
                var periodoGarantia = values[1];

                // Si no hay fecha de completado o período de garantía, no hay garantía
                if (fechaCompletado == null || periodoGarantia == null)
                {
                    return "Sin garantía";
                }

                if (fechaCompletado is not DateTime completado || periodoGarantia is not int diasGarantia)
                {
                    return "Sin garantía";
                }

                if (completado == DateTime.MinValue || diasGarantia <= 0)
                {
                    return "Sin garantía";
                }

                DateTime fechaVencimiento = completado.AddDays(diasGarantia);
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

            return "Sin garantía";
        }
        catch
        {
            return "Sin garantía";
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("GarantiaStatusConverter no admite conversión bidireccional.");
    }
}
