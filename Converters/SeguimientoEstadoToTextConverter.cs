using System;
using System.Globalization;
using System.Windows.Data;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Utilities;

namespace GestLog.Converters
{
    /// <summary>
    /// Convertidor que muestra el texto correcto para un seguimiento.
    /// Si es Correctivo, muestra "Correctivo".
    /// Si no es Correctivo, muestra el texto del estado.
    /// </summary>
    public class SeguimientoEstadoToTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SeguimientoMantenimientoDto seguimiento)
            {
                // Si es Correctivo, mostrar "Correctivo"
                if (seguimiento.TipoMtno == TipoMantenimiento.Correctivo)
                {
                    return "Correctivo";
                }

                // Si no es Correctivo, mostrar el estado
                return EstadoSeguimientoUtils.EstadoToTexto(seguimiento.Estado);
            }

            return "-";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
