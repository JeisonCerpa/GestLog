using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    public class TipoMemoriaTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tipo)
            {
                if (tipo.StartsWith("Desconocido"))
                {
                    return $"Tipo de memoria no reconocido. Código original: {tipo.Replace("Desconocido (", "").Replace(")", "")}";
                }
                return $"Tipo de memoria detectado: {tipo}";
            }
            return "Tipo de RAM";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
