using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte dos fechas a la diferencia en días
    /// MultiValueConverter: {Binding Path1}, {Binding Path2} -> string
    /// </summary>
    public class DateDifferenceToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return "0 días";

            if (!(values[0] is DateTime fechaInicio) || !(values[1] is DateTime fechaCompletado))
                return "0 días";

            if (fechaInicio == DateTime.MinValue || fechaCompletado == DateTime.MinValue)
                return "0 días";

            var diferencia = fechaCompletado.Date - fechaInicio.Date;
            var dias = diferencia.Days;

            return $"{dias} días";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
