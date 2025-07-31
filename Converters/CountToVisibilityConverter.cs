using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte un valor numérico a Visibility. 
    /// Por defecto: 0 = Visible, > 0 = Collapsed
    /// Con parámetro "invert": 0 = Collapsed, > 0 = Visible
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convierte un valor numérico a Visibility.
        /// </summary>
        /// <param name="value">El valor numérico (int, double, etc.)</param>
        /// <param name="targetType">El tipo de la propiedad de enlace de destino.</param>
        /// <param name="parameter">Parámetro opcional. "invert" invierte la lógica.</param>
        /// <param name="culture">La cultura que se utiliza en el convertidor.</param>
        /// <returns>Visibility basada en el valor numérico.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convertir el valor a número
            int count = 0;
            if (value != null)
            {
                if (value is int intValue)
                    count = intValue;
                else if (double.TryParse(value.ToString(), out double doubleValue))
                    count = (int)doubleValue;
            }

            // Determinar si debe invertir la lógica
            bool invert = parameter is string paramStr && paramStr.ToLower() == "invert";
            
            // Lógica por defecto: 0 = Visible, > 0 = Collapsed
            // Con invert: 0 = Collapsed, > 0 = Visible
            bool isVisible = count == 0;
            
            if (invert)
                isVisible = !isVisible;
                
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// No implementado - conversión de vuelta no es necesaria para este caso de uso
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
