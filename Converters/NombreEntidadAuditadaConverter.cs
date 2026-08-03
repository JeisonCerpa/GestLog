using System;
using System.Globalization;
using System.Windows.Data;
using Modules.Usuarios.Services;

namespace GestLog.Converters
{
    /// <summary>
    /// Muestra el nombre de negocio de la entidad auditada, según su perfil de auditoría.
    /// </summary>
    public class NombreEntidadAuditadaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var nombre = value?.ToString();
            return string.IsNullOrEmpty(nombre) ? string.Empty : AuditoriaPerfiles.NombreLegible(nombre);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() ?? string.Empty;
    }
}
