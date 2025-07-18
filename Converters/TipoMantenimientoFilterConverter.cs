using System;
using System.Globalization;
using System.Windows.Data;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Converters
{
    public class TipoMantenimientoFilterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var modoRestringido = values.Length > 0 && values[0] is bool b && b;
            var tipos = Enum.GetValues(typeof(TipoMantenimiento));
            if (modoRestringido)
            {
                // Solo mostrar Correctivo y Predictivo
                return new[] { TipoMantenimiento.Correctivo, TipoMantenimiento.Predictivo };
            }
            return tipos;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
