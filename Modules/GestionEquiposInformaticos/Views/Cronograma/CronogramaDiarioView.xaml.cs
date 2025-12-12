using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows;
using SystemUri = System.Windows.Application;
using System;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Cronograma
{
    public class WeekInfo
    {
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public string DateRange { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public partial class CronogramaDiarioView : UserControl
    {
        private DateTime _currentDisplayDate = DateTime.Now;
          // Referencias a los controles
        private System.Windows.Controls.ListBox? _weekSelector;
        private Popup? _calendarPopup;
        private TextBlock? _monthYearText;
        private TextBlock? _selectedWeekText;
        
        private ObservableCollection<WeekInfo> _weeks = new();

        public CronogramaDiarioView()
        {
            try
            {
                System.Windows.Application.LoadComponent(this, new Uri("/GestLog;component/Modules/GestionEquiposInformaticos/Views/Cronograma/CronogramaDiarioView.xaml", UriKind.Relative));
            }
            catch { }
            this.Loaded += CronogramaDiarioView_Loaded;
        }

        private async void CronogramaDiarioView_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {                if (DataContext == null)
                {
                    var sp = LoggingService.GetServiceProvider();
                    var vm = sp.GetService(typeof(GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma.CronogramaDiarioViewModel)) as GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma.CronogramaDiarioViewModel;
                    if (vm != null)
                        DataContext = vm;
                }

                if (DataContext is GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma.CronogramaDiarioViewModel vm2 && vm2.Planificados.Count == 0)
                {
                    await vm2.LoadAsync(System.Threading.CancellationToken.None);
                }

                // Inicializar referencias a controles
                InitializeControlReferences();
                
                // Inicializar selector de semanas
                InitializeWeekSelector();
            }
            catch { }
        }        private void InitializeControlReferences()
        {
            try
            {
                _weekSelector = FindName("WeekSelector") as System.Windows.Controls.ListBox;
                _calendarPopup = FindName("CalendarPopup") as Popup;
                _monthYearText = FindName("MonthYearText") as TextBlock;
                _selectedWeekText = FindName("SelectedWeekText") as TextBlock;
            }
            catch { }
        }        private void InitializeWeekSelector()
        {
            try
            {
                GenerateWeeksForYear(_currentDisplayDate.Year);
                
                // Seleccionar la semana actual
                var currentWeek = ISOWeek.GetWeekOfYear(_currentDisplayDate);
                var currentWeekInfo = _weeks.FirstOrDefault(w => w.WeekNumber == currentWeek && w.Year == _currentDisplayDate.Year);
                
                if (_weekSelector != null && currentWeekInfo != null)
                {
                    _weekSelector.SelectedItem = currentWeekInfo;
                    
                    // Hacer scroll hasta la semana actual usando Dispatcher para asegurar que el ListBox esté renderizado
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ScrollToCurrentWeek(currentWeekInfo);
                    }), DispatcherPriority.Loaded);
                }
                
                UpdateMonthYearText();
                UpdateSelectedWeekText(_currentDisplayDate);
            }
            catch { }
        }

        private void GenerateWeeksForYear(int year)
        {
            try
            {
                _weeks.Clear();
                
                // Obtener el número de semanas en el año
                var weeksInYear = ISOWeek.GetWeeksInYear(year);
                
                for (int weekNumber = 1; weekNumber <= weeksInYear; weekNumber++)
                {
                    // Calcular el primer día de la semana ISO
                    var firstDayOfWeek = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Monday);
                    var lastDayOfWeek = firstDayOfWeek.AddDays(6);
                    
                    // Formatear el rango de fechas
                    var dateRange = $"{firstDayOfWeek:dd/MM} - {lastDayOfWeek:dd/MM}";
                    
                    _weeks.Add(new WeekInfo
                    {
                        WeekNumber = weekNumber,
                        Year = year,
                        DateRange = dateRange,
                        StartDate = firstDayOfWeek,
                        EndDate = lastDayOfWeek
                    });
                }
                
                if (_weekSelector != null)
                {
                    _weekSelector.ItemsSource = _weeks;
                }
            }
            catch { }
        }        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_calendarPopup != null)
                {
                    _calendarPopup.IsOpen = !_calendarPopup.IsOpen;
                    
                    // Si se está abriendo el popup, hacer scroll hasta la semana actual
                    if (_calendarPopup.IsOpen)
                    {
                        // Usar Dispatcher para asegurar que el ListBox esté renderizado antes del scroll
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ScrollToSelectedWeek();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
            }
            catch { }
        }

        private void ScrollToCurrentWeek(WeekInfo weekInfo)
        {
            try
            {
                if (_weekSelector != null && weekInfo != null)
                {
                    _weekSelector.ScrollIntoView(weekInfo);
                }
            }
            catch { }
        }

        private void ScrollToSelectedWeek()
        {
            try
            {
                if (_weekSelector?.SelectedItem is WeekInfo selectedWeek)
                {
                    _weekSelector.ScrollIntoView(selectedWeek);
                }
            }
            catch { }
        }        private void PrevMonthButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentDisplayDate = _currentDisplayDate.AddYears(-1);
                GenerateWeeksForYear(_currentDisplayDate.Year);
                UpdateMonthYearText();
                
                // Seleccionar primera semana del año y hacer scroll
                var firstWeek = _weeks.FirstOrDefault();
                if (_weekSelector != null && firstWeek != null)
                {
                    _weekSelector.SelectedItem = firstWeek;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _weekSelector.ScrollIntoView(firstWeek);
                    }), DispatcherPriority.Loaded);
                    
                    UpdateSelectedWeekText(firstWeek.StartDate);
                    UpdateViewModelWeek(firstWeek.StartDate);
                }
            }
            catch { }
        }

        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentDisplayDate = _currentDisplayDate.AddYears(1);
                GenerateWeeksForYear(_currentDisplayDate.Year);
                UpdateMonthYearText();
                
                // Seleccionar primera semana del año y hacer scroll
                var firstWeek = _weeks.FirstOrDefault();
                if (_weekSelector != null && firstWeek != null)
                {
                    _weekSelector.SelectedItem = firstWeek;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _weekSelector.ScrollIntoView(firstWeek);
                    }), DispatcherPriority.Loaded);
                    
                    UpdateSelectedWeekText(firstWeek.StartDate);
                    UpdateViewModelWeek(firstWeek.StartDate);
                }
            }
            catch { }
        }

        private void WeekSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_weekSelector?.SelectedItem is WeekInfo selectedWeek)
                {
                    UpdateSelectedWeekText(selectedWeek.StartDate);
                    UpdateViewModelWeek(selectedWeek.StartDate);
                    if (_calendarPopup != null)
                    {
                        _calendarPopup.IsOpen = false;
                    }
                }
            }
            catch { }
        }

        private void UpdateMonthYearText()
        {
            try
            {
                if (_monthYearText != null)
                {
                    _monthYearText.Text = $"Año {_currentDisplayDate.Year}";
                }
            }
            catch { }
        }

        private void UpdateSelectedWeekText(DateTime date)
        {
            try
            {
                if (_selectedWeekText != null)
                {
                    var weekNumber = ISOWeek.GetWeekOfYear(date);
                    var year = ISOWeek.GetYear(date);
                    _selectedWeekText.Text = $"Semana {weekNumber}, {year}";
                }
            }
            catch { }
        }

        private void UpdateViewModelWeek(DateTime date)
        {            try
            {
                if (DataContext is GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma.CronogramaDiarioViewModel vm)
                {
                    var weekNumber = ISOWeek.GetWeekOfYear(date);
                    var year = ISOWeek.GetYear(date);
                    
                    // Actualizar ViewModel de forma segura sin crear ciclos
                    vm.SelectedWeek = weekNumber;
                    vm.SelectedYear = year;
                }
            }            catch { }
        }
        
        // Manejadores para el overlay modal
        private void DetalleOverlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Cerrar overlay al hacer clic en el fondo
            if (DataContext is GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma.CronogramaDiarioViewModel vm)
            {
                vm.CerrarDetallePlanCommand?.Execute(null);
            }
        }        private void DetallePanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Evitar que el clic en el panel cierre el overlay
            e.Handled = true;
        }
        
        private void CronogramaDiarioView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Cerrar overlay con Escape
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (DataContext is GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma.CronogramaDiarioViewModel vm)
                {
                    vm.CerrarDetallePlanCommand?.Execute(null);
                }
            }
        }
    }
}

