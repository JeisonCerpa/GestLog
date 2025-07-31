using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using GestLog.Modules.Usuarios.Models;
using System.Collections.ObjectModel;

namespace GestLog.Converters
{
    public class RoleCheckedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: ViewModel, values[1]: Rol
            if (values.Length < 2)
                return false;
            var viewModel = values[0] as dynamic;
            var rol = values[1] as Rol;
            if (viewModel?.RolesSeleccionados is ObservableCollection<Rol> seleccionados && rol != null)
                return seleccionados.Any(r => r.IdRol == rol.IdRol);
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
