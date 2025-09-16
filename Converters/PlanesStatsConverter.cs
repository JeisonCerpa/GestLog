using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter que toma la colección de días y calcula estadísticas de planes
    /// </summary>
    public class PlanesStatsConverter : IValueConverter
    {        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is ObservableCollection<DayScheduleViewModel> days)
                {
                    int totalPlanes = 0;
                    int planesRealizados = 0;
                    int totalItems = 0; // Debug: contar todos los items

                    foreach (var day in days)
                    {
                        foreach (var item in day.Items)
                        {
                            totalItems++; // Debug: contar todos
                            
                            // Cambiar lógica: contar todos los items que tengan "Plan Semanal" en Marca
                            if (item.Marca == "Plan Semanal" || item.EsPlanSemanal)
                            {
                                totalPlanes++;
                                if (item.PlanEjecutadoSemana)
                                {
                                    planesRealizados++;
                                }
                            }
                        }
                    }

                    // Debug: mostrar también el total de items para troubleshooting
                    return $"{totalPlanes} planes, {planesRealizados} realizados ({totalItems} items)";
                }
            }
            catch (Exception ex)
            {
                // En caso de error, mostrar el error para debug
                return $"Error: {ex.Message}";
            }

            return "0 planes, 0 realizados";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
