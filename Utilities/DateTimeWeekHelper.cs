// filepath: e:\Softwares\GestLog\Utilities\DateTimeWeekHelper.cs
using System;

namespace GestLog.Utilities
{
    /// <summary>
    /// Utilidades para cálculos relacionados con semanas ISO 8601.
    /// </summary>
    public static class DateTimeWeekHelper
    {
        /// <summary>
        /// Obtiene el primer día (lunes) de la semana ISO 8601 indicada.
        /// </summary>
        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var weekNum = weekOfYear;
            if (firstWeek <= 1)
                weekNum -= 1;
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3); // Regresar al lunes
        }
    }
}
