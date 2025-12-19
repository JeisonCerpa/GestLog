using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte una fecha y un número de días a la fecha de vencimiento
    /// MultiValueConverter: {Binding FechaCompletado}, {Binding PeriodoGarantia} -> string
    /// </summary>
    public class DateAddDaysConverter : IMultiValueConverter
    {        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return "N/A";

            if (!(values[0] is DateTime fecha))
                return "N/A";

            // Obtener días de garantía desde el segundo valor (puede ser int o int?)
            int diasGarantia = 0;
            
            if (values[1] is int intValue)
            {
                diasGarantia = intValue;
            }
            else if (values[1] != null)
            {
                // Intenta convertir a int en caso de que sea nullable
                try
                {
                    diasGarantia = System.Convert.ToInt32(values[1]);
                }
                catch
                {
                    return "N/A";
                }
            }
            else
            {
                return "N/A";
            }

            if (fecha == DateTime.MinValue || diasGarantia <= 0)
                return "N/A";

            var fechaVencimiento = fecha.AddDays(diasGarantia);
            return fechaVencimiento.ToString("dd/MM/yyyy", culture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
