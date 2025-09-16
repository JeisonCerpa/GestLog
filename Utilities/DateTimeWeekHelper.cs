// filepath: e:\Softwares\GestLog\Utilities\DateTimeWeekHelper.cs
using System;
using System.Globalization;

namespace GestLog.Utilities
{
    /// <summary>
    /// Utilidades para cálculos relacionados con semanas ISO 8601.
    /// Centraliza toda la lógica de semanas para evitar discrepancias.
    /// </summary>
    public static class DateTimeWeekHelper
    {
        /// <summary>
        /// Obtiene el primer día (lunes) de la semana ISO 8601 indicada.
        /// </summary>
        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            // Algoritmo estándar ISO 8601 basado en el jueves de la primera semana
            var jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.InvariantCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var weekNum = weekOfYear;
            if (firstWeek <= 1)
                weekNum -= 1;
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3); // Lunes
        }

        /// <summary>
        /// Devuelve el (anioISO, semanaISO) para una fecha dada siguiendo ISO 8601.
        /// </summary>
        public static (int anio, int semana) GetIsoYearWeek(DateTime date)
        {
            var cal = CultureInfo.InvariantCulture.Calendar;
            int semana = cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            int anio = date.Year;
            // Ajuste de cruces de año ISO
            if (date.Month == 1 && semana >= 52)
                anio -= 1;
            else if (date.Month == 12 && semana == 1)
                anio += 1;
            return (anio, semana);
        }

        /// <summary>
        /// Calcula la fecha objetivo (día específico) dentro de una semana ISO para el día programado (1=Lunes..7=Domingo).
        /// </summary>
        public static DateTime GetFechaObjetivoSemana(int anioISO, int semanaISO, int diaProgramado)
        {
            if (diaProgramado < 1 || diaProgramado > 7) throw new ArgumentOutOfRangeException(nameof(diaProgramado));
            var monday = FirstDateOfWeekISO8601(anioISO, semanaISO);
            return monday.AddDays(diaProgramado - 1);
        }

        /// <summary>
        /// Determina si un plan está atrasado: no ejecutado y la fecha objetivo ya pasó (estrictamente < hoy).
        /// </summary>
        public static bool IsPlanAtrasado(int anioISO, int semanaISO, int diaProgramado, bool ejecutado, DateTime? hoyOverride = null)
        {
            if (ejecutado) return false;
            var hoy = (hoyOverride ?? DateTime.Now).Date;
            var fechaObjetivo = GetFechaObjetivoSemana(anioISO, semanaISO, diaProgramado).Date;
            return fechaObjetivo < hoy;
        }
    }
}
