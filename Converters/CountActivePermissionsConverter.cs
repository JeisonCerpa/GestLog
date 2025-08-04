using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Converters
{
    /// <summary>
    /// Convertidor para contar permisos activos en una colección de módulos
    /// </summary>
    public class CountActivePermissionsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {            if (value is ObservableCollection<ModuloPermisos> modulos)
            {
                return modulos.SelectMany(m => m.Permisos).Count(p => p.EstaAsignado);
            }
            
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
