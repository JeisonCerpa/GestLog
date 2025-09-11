using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter que determina el color de fondo basado en el tipo de elemento del cronograma
    /// </summary>
    public class CronogramaTipoToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GestLog.Modules.GestionMantenimientos.Models.CronogramaMantenimientoDto cronograma)
            {
                // Si Marca es "Plan Semanal", es un plan de equipo
                if (cronograma.Marca == "Plan Semanal")
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 233)); // Verde claro
                }
                // Si no, es un mantenimiento programado tradicional
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(247, 247, 247)); // Gris claro (original)
            }
            
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(247, 247, 247)); // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
