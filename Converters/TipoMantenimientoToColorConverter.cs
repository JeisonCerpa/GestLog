using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using MediaColor = System.Windows.Media.Color;

namespace GestLog.Converters
{
    /// <summary>
    /// Convertidor que asigna colores según el tipo de mantenimiento.
    /// </summary>
    public class TipoMantenimientoToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return new SolidColorBrush(Colors.Gray);

            // Si es un enum TipoMantenimiento
            if (value is TipoMantenimiento tipo)
            {
                return tipo switch
                {
                    // #388E3C - Verde (Preventivo) - semántica: mantenimiento planificado
                    TipoMantenimiento.Preventivo => new SolidColorBrush(MediaColor.FromRgb(56, 142, 60)),

                    // #7E57C2 - Morado (Correctivo) - semántica: mantenimiento no planificado, urgencia especial
                    TipoMantenimiento.Correctivo => new SolidColorBrush(MediaColor.FromRgb(126, 87, 194)),

                    // #2196F3 - Azul (Predictivo) - semántica: basado en predicciones/análisis
                    TipoMantenimiento.Predictivo => new SolidColorBrush(MediaColor.FromRgb(33, 150, 243)),

                    // Gris para otros tipos
                    _ => new SolidColorBrush(MediaColor.FromRgb(157, 157, 156))
                };
            }

            // Si es string, intentar convertir
            if (value is string tipoStr && Enum.TryParse<TipoMantenimiento>(tipoStr, ignoreCase: true, out var parsedTipo))
            {
                return parsedTipo switch
                {
                    TipoMantenimiento.Preventivo => new SolidColorBrush(MediaColor.FromRgb(56, 142, 60)),
                    TipoMantenimiento.Correctivo => new SolidColorBrush(MediaColor.FromRgb(126, 87, 194)),
                    TipoMantenimiento.Predictivo => new SolidColorBrush(MediaColor.FromRgb(33, 150, 243)),
                    _ => new SolidColorBrush(MediaColor.FromRgb(157, 157, 156))
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
