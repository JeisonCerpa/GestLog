using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Utilities;
using MediaColor = System.Windows.Media.Color;

namespace GestLog.Converters
{
    /// <summary>
    /// Convertidor inteligente para mostrar colores en seguimientos.
    /// Si es Correctivo, muestra el color del tipo (morado).
    /// Si no es Correctivo, muestra el color del estado.
    /// </summary>
    public class SeguimientoEstadoToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {            // Si el valor es un DTO de seguimiento, accedemos a su TipoMtno y Estado
            if (value is SeguimientoMantenimientoDto seguimiento)
            {
                // Si es Correctivo, mostrar color morado del tipo
                if (seguimiento.TipoMtno == TipoMantenimiento.Correctivo)
                {
                    return ConvertFromColor(EstadoSeguimientoUtils.XLColorFromTipo(TipoMantenimiento.Correctivo));
                }

                // Si no es Correctivo, mostrar color del estado
                return ConvertFromColor(EstadoSeguimientoUtils.XLColorFromEstado(seguimiento.Estado));
            }

            // Fallback: si recibimos un EstadoSeguimientoMantenimiento directamente
            if (value is EstadoSeguimientoMantenimiento estado)
            {
                return ConvertFromColor(EstadoSeguimientoUtils.XLColorFromEstado(estado));
            }

            return new SolidColorBrush(MediaColor.FromRgb(157, 157, 156)); // Gris por defecto
        }        /// <summary>
        /// Convierte un XLColor a SolidColorBrush de WPF.
        /// Usa una tabla de colores conocidos para mapear XLColor a RGB.
        /// </summary>
        private SolidColorBrush ConvertFromColor(ClosedXML.Excel.XLColor xlColor)
        {
            try
            {
                // Crear mapa de colores por valor RGB
                var colorMap = new Dictionary<(byte, byte, byte), SolidColorBrush>
                {
                    // #388E3C - Verde (Realizado en tiempo)
                    { (56, 142, 60), new SolidColorBrush(MediaColor.FromRgb(56, 142, 60)) },
                    
                    // #FFB300 - Ámbar (Realizado fuera de tiempo)
                    { (255, 179, 0), new SolidColorBrush(MediaColor.FromRgb(255, 179, 0)) },
                    
                    // #A85B00 - Naranja (Atrasado)
                    { (168, 91, 0), new SolidColorBrush(MediaColor.FromRgb(168, 91, 0)) },
                    
                    // #C80000 - Rojo (No realizado)
                    { (200, 0, 0), new SolidColorBrush(MediaColor.FromRgb(200, 0, 0)) },
                    
                    // #B3E5FC - Celeste (Pendiente)
                    { (179, 229, 252), new SolidColorBrush(MediaColor.FromRgb(179, 229, 252)) },
                    
                    // #7E57C2 - Morado (Correctivo)
                    { (126, 87, 194), new SolidColorBrush(MediaColor.FromRgb(126, 87, 194)) }
                };

                // Obtener la representación en string del XLColor y comparar
                var colorString = xlColor.ToString().ToUpper();
                
                // Iterar y buscar coincidencia aproximada
                foreach (var kvp in colorMap)
                {
                    var expectedHex = $"{kvp.Key.Item1:X2}{kvp.Key.Item2:X2}{kvp.Key.Item3:X2}";
                    if (colorString.Contains(expectedHex) || colorString.EndsWith(expectedHex))
                    {
                        return kvp.Value;
                    }
                }

                // Si es morado (#7E57C2), devolverlo por defecto para Correctivo
                return new SolidColorBrush(MediaColor.FromRgb(126, 87, 194));
            }
            catch
            {
                return new SolidColorBrush(MediaColor.FromRgb(157, 157, 156)); // Gris por defecto
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
