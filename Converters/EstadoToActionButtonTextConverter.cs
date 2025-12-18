using System;
using System.Globalization;
using System.Windows.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter que retorna el texto del botón de acción basado en el estado del mantenimiento
    /// </summary>
    public class EstadoToActionButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EstadoMantenimientoCorrectivo estado)
            {
                return estado switch
                {
                    EstadoMantenimientoCorrectivo.Pendiente => "Enviar a Reparación",
                    EstadoMantenimientoCorrectivo.EnReparacion => "Completar",
                    EstadoMantenimientoCorrectivo.Completado => "Detalles",
                    EstadoMantenimientoCorrectivo.Cancelado => "Detalles",
                    _ => "Detalles"
                };
            }
            return "Detalles";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
