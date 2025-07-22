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
            if (value is EstadoSeguimientoMantenimiento estado)
            {
                // Paleta: Verde #2B8E3F, Amarillo/Ámbar #F9B233, Rojo #C0392B, Verde intermedio #7AC943
                switch (estado)
                {
                    case EstadoSeguimientoMantenimiento.RealizadoEnTiempo:
                        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(43, 142, 63)); // #2B8E3F
                    case EstadoSeguimientoMantenimiento.Atrasado:
                        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 178, 51)); // #F9B233
                    case EstadoSeguimientoMantenimiento.NoRealizado:
                        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(192, 57, 43)); // #C0392B
                    case EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo:
                        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(122, 201, 67)); // #7AC943
                    default:
                        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(112, 111, 111)); // #706F6F
                }
            }
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(112, 111, 111));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
