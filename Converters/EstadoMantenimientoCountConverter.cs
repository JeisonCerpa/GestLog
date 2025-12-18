using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter que cuenta los mantenimientos por un estado específico
    /// Se pasa como parámetro el estado a contar (p.ej. "Pendiente")
    /// </summary>
    public class EstadoMantenimientoCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is System.Collections.ObjectModel.ObservableCollection<MantenimientoCorrectivoDto> mantenimientos))
            {
                return "0";
            }

            if (parameter == null)
            {
                return "0";
            }

            if (!Enum.TryParse<EstadoMantenimientoCorrectivo>(parameter.ToString(), out var estado))
            {
                return "0";
            }

            var count = mantenimientos.Count(m => m.Estado == estado);
            return count.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
