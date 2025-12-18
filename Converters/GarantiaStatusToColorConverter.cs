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
            if (values.Length >= 2)
            {
                var fechaCompletado = values[0];
                var periodoGarantia = values[1];

                // Si no hay fecha de completado o período de garantía, no hay garantía (gris)
                if (fechaCompletado == null || periodoGarantia == null || 
                    (fechaCompletado is DateTime dt && dt == DateTime.MinValue) || 
                    periodoGarantia is not int diasGarantia)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(157, 157, 156)); // Gris (#9D9D9C)
                }

                if (fechaCompletado is DateTime completado && diasGarantia > 0)
                {
                    DateTime fechaVencimiento = completado.AddDays(diasGarantia);
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

                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(157, 157, 156)); // Gris - Sin garantía
            }

            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(157, 157, 156));
        }
        catch
        {
            return new SolidColorBrush(Colors.Transparent);
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("GarantiaStatusToColorConverter no admite conversión bidireccional.");
    }
}
