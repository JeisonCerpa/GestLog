using System;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter que retorna el color del botón de acción basado en el estado del mantenimiento
    /// </summary>
    public class EstadoToActionButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EstadoMantenimientoCorrectivo estado)
            {
                return estado switch
                {
                    // Pendiente - Naranja/Warning
                    EstadoMantenimientoCorrectivo.Pendiente => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 165, 0)),
                    
                    // En Reparación - Verde/Success
                    EstadoMantenimientoCorrectivo.EnReparacion => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 40, 167, 69)),
                    
                    // Completado/Cancelado - Gris/Secondary
                    EstadoMantenimientoCorrectivo.Completado => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 108, 117, 125)),
                    EstadoMantenimientoCorrectivo.Cancelado => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 108, 117, 125)),
                    
                    _ => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 108, 117, 125))
                };
            }
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 108, 117, 125));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
