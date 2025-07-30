using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using GestLog.Modules.Personas.Models;

namespace GestLog.Converters
{
    public class PersonaIdToNombreCompletoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Guid id && parameter is ObservableCollection<Persona> personas)
            {
                var persona = personas.FirstOrDefault(p => p.IdPersona == id);
                return persona?.NombreCompleto ?? string.Empty;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
