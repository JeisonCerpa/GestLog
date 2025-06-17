using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace GestLog.Converters;

/// <summary>
/// Convertidor que combina múltiples valores booleanos usando operadores lógicos
/// y convierte el resultado a Visibility
/// </summary>
public class MultiBooleanToVisibilityConverter : IMultiValueConverter
{
    /// <summary>
    /// Combina múltiples valores booleanos y los convierte a Visibility
    /// </summary>
    /// <param name="values">Array de valores booleanos</param>
    /// <param name="targetType">Tipo de destino (Visibility)</param>
    /// <param name="parameter">Parámetro que especifica la operación: "AND" (default) o "OR"</param>
    /// <param name="culture">Cultura</param>
    /// <returns>Visibility.Visible si la condición se cumple, Visibility.Collapsed si no</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length == 0)
            return Visibility.Collapsed;

        // Convertir todos los valores a booleanos
        var boolValues = values.Select(value => 
        {
            if (value is bool b) return b;
            if (value is string s) return !string.IsNullOrWhiteSpace(s);
            return value != null;
        }).ToArray();

        // Determinar la operación (AND por defecto)
        var operation = parameter?.ToString()?.ToUpper() ?? "AND";
        
        bool result = operation switch
        {
            "OR" => boolValues.Any(b => b),
            "AND" => boolValues.All(b => b),
            _ => boolValues.All(b => b) // Default: AND
        };

        return result ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// No implementado - conversión de vuelta no es necesaria para este caso de uso
    /// </summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("MultiBooleanToVisibilityConverter no admite conversión bidireccional.");
    }
}
