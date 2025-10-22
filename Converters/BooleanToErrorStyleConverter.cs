using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte un booleano a un estilo de error o normal para campos de formulario.
    /// Si es true (hay error), aplica el estilo de error; si es false, aplica el estilo normal.
    /// </summary>
    public class BooleanToErrorStyleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isError)
            {
                // Obtener los estilos del diccionario de recursos
                var app = System.Windows.Application.Current;
                if (app?.Resources != null)
                {
                    // Si hay error, usar ErrorFieldStyle; si no, usar RequiredFieldStyle
                    var styleName = isError ? "ErrorFieldStyle" : "RequiredFieldStyle";
                    return app.Resources[styleName] as System.Windows.Style;
                }
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
