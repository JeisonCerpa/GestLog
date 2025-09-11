using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter que determina el color del borde basado en el tipo de elemento del cronograma
    /// </summary>
    public class CronogramaTipoToBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GestLog.Modules.GestionMantenimientos.Models.CronogramaMantenimientoDto cronograma)
            {
                // Si Marca es "Plan Semanal", es un plan de equipo
                if (cronograma.Marca == "Plan Semanal")
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Verde
                }
                // Si no, es un mantenimiento programado tradicional
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 221, 221)); // Gris claro (original)
            }
            
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 221, 221)); // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
