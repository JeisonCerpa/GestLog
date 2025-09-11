using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter que convierte el número de día programado a nombre del día
    /// </summary>
    public class DiaProgramadoToNombreDiaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte dia || value is int)
            {
                var diaNumero = System.Convert.ToInt32(value);
                return diaNumero switch
                {
                    1 => "Lunes",
                    2 => "Martes",
                    3 => "Miércoles",
                    4 => "Jueves",
                    5 => "Viernes",
                    6 => "Sábado",
                    7 => "Domingo",
                    _ => "No definido"
                };
            }
            
            return "No definido";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
