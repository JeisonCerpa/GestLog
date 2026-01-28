using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Converters
{
    /// <summary>
    /// Converter que mapea el enum Sede a un color de fondo para distinguir visualmente las sedes en DataGrid.
    /// - Taller: Verde claro (#E8F5E9)
    /// - Bayunca: Azul claro (#E3F2FD)
    /// </summary>
    public class SedeToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Sede sede)
                return new SolidColorBrush(Colors.White);

            return sede switch
            {
                Sede.Taller => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E8F5E9")),
                Sede.Bayunca => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E3F2FD")),
                _ => new SolidColorBrush(Colors.White)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
