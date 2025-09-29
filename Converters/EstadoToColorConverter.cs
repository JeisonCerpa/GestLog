using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Converters
{
    public class EstadoToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return new SolidColorBrush(Colors.Transparent);

                // Obtener representación de texto del estado (funciona para string o enum)
                var estadoStr = value.ToString() ?? string.Empty;

                // Normalizar
                estadoStr = estadoStr.Trim().ToLowerInvariant();

                // Mapear estados a colores (usar paleta del proyecto)
                // Activo -> verde
                if (estadoStr == "activo" || estadoStr == "enuso" || estadoStr == "en uso")
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(43, 142, 63)); // #2B8E3F

                // En mantenimiento -> ámbar
                if (estadoStr == "enmantenimiento" || estadoStr == "en mantenimiento")
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 178, 51)); // #F9B233

                // En reparación -> naranja/ámbar oscuro
                if (estadoStr == "enreparacion" || estadoStr == "en reparacion" || estadoStr == "enreparación")
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(168, 91, 0)); // #A85B00

                // Dado de baja -> gris claro/neutral
                if (estadoStr == "dadodebaja" || estadoStr == "dado de baja" || estadoStr == "dadode baja")
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(237, 237, 237)); // #EDEDED (más claro)

                // Inactivo -> gris medio/oscuro
                if (estadoStr == "inactivo")
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)); // #9E9E9E

                // En uso/Disponible/Otros -> color neutro o verde suave
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(112, 111, 111)); // #706F6F
            }
            catch
            {
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
