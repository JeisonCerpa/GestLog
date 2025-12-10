using System;
using System.Globalization;
using System.Windows.Data;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter que determina el texto del botón basado en el tipo de elemento del cronograma
    /// </summary>
    public class CronogramaTipoToButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CronogramaMantenimientoDto cronograma)
            {
                // Si Marca es "Plan Semanal", es un plan de equipo
                if (cronograma.Marca == "Plan Semanal")
                {
                    return "Ejecutar";
                }
                // Si no, es un mantenimiento programado tradicional
                return "Registrar";
            }
            
            return "Acción"; // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
