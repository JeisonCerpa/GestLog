using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using GestLog.Services.Core.Logging;
using System;
using System.Windows;
using GestLog.Views.Usuarios; // IModalService
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionMantenimientos.ViewModels
{
    public partial class CronogramaDiarioViewModel : ObservableObject
    {
        private readonly IMantenimientoService _mantenimientoService;
        private readonly IGestLogLogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IModalService _modalService;

        public Action<SeguimientoMantenimiento?>? RequestOpenRegistrar { get; set; }

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        // Nueva: selección por semana y año
        public ObservableCollection<int> Weeks { get; } = new ObservableCollection<int>();
        public ObservableCollection<int> Years { get; } = new ObservableCollection<int>();

        [ObservableProperty]
        private int _selectedWeek;

        [ObservableProperty]
        private int _selectedYear;

        // Cuando cambian SelectedWeek o SelectedYear, recargar automáticamente el cronograma
        partial void OnSelectedWeekChanged(int value)
        {
            // No esperar (fire-and-forget), LoadAsync maneja sus errores internamente y registra.
            _ = LoadAsync();
        }

        partial void OnSelectedYearChanged(int value)
        {
            _ = LoadAsync();
        }

        public ObservableCollection<SeguimientoMantenimiento> Planificados { get; } = new ObservableCollection<SeguimientoMantenimiento>();
        public ObservableCollection<DayViewModel> Days { get; } = new ObservableCollection<DayViewModel>();

        public class DayViewModel
        {
            public string Name { get; set; } = string.Empty;
            public ObservableCollection<SeguimientoMantenimiento> Items { get; } = new ObservableCollection<SeguimientoMantenimiento>();
        }

        public CronogramaDiarioViewModel(IMantenimientoService mantenimientoService, IGestLogLogger logger, IServiceProvider serviceProvider, IModalService modalService)
        {
            _mantenimientoService = mantenimientoService;
            _logger = logger;
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _modalService = modalService ?? throw new ArgumentNullException(nameof(modalService));

            // Inicializar semanas y años
            for (int i = 1; i <= 53; i++) Weeks.Add(i);
            int yearNow = DateTime.Today.Year;
            for (int y = yearNow - 2; y <= yearNow + 1; y++) Years.Add(y);

            // Semana actual
            var cal = CultureInfo.CurrentCulture.Calendar;
            SelectedWeek = cal.GetWeekOfYear(DateTime.Today, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            SelectedYear = yearNow;
        }

        [RelayCommand]
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Planificados.Clear();
                Days.Clear();

                // Calcular la fecha (lunes) correspondiente a SelectedWeek/SelectedYear
                DateTime mondayOfWeek = GetFirstDateOfWeekISO(SelectedYear, SelectedWeek);

                var items = await _mantenimientoService.GetPlannedForDateAsync(mondayOfWeek, cancellationToken);
                foreach (var s in items)
                    Planificados.Add(s);

                // Construir Days (Lunes a Viernes) usando mondayOfWeek
                var names = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };
                for (int i = 0; i < 5; i++)
                {
                    var d = new DayViewModel { Name = names[i] };
                    Days.Add(d);
                }

                foreach (var s in Planificados)
                {
                    var fecha = s.FechaRegistro ?? mondayOfWeek;
                    var dow = ((int)fecha.DayOfWeek + 6) % 7; // convert Sunday=0.. to Monday=0
                    if (dow >= 0 && dow <= 4)
                    {
                        Days[dow].Items.Add(s);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando cronograma diario");
            }
        }

        private DateTime GetFirstDateOfWeekISO(int year, int weekOfYear)
        {
            // Algoritmo estándar para obtener el lunes de la semana ISO
            DateTime jan1 = new DateTime(year, 1, 1);
            // Encontrar primer jueves del año
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var weekNum = weekOfYear;
            var result = firstThursday.AddDays((weekNum - firstWeek) * 7);
            // El lunes de esa semana
            return result.AddDays(-3);
        }

        [RelayCommand(CanExecute = nameof(CanOpenRegistrar))]
        public void OpenRegistrar(SeguimientoMantenimiento? seguimiento)
        {
            // Compatibilidad: invocar el callback si está registrado (host antiguo)
            RequestOpenRegistrar?.Invoke(seguimiento);

            if (seguimiento == null) return;

            try
            {
                // Resolver el ViewModel del registrador desde DI
                var vm = _serviceProvider.GetService<RegistrarMantenimientoViewModel>();
                if (vm == null)
                {
                    _logger.LogWarning("RegistrarMantenimientoViewModel no pudo resolverse desde DI");
                    return;
                }

                // Pasar el seguimiento seleccionado al VM
                vm.Seguimiento = seguimiento;

                // Asegurar Semana/Año en el seguimiento (por si es nuevo)
                if (vm.Seguimiento.Semana == 0)
                    vm.Seguimiento.Semana = SelectedWeek;
                if (vm.Seguimiento.Anio == 0)
                    vm.Seguimiento.Anio = SelectedYear;

                // Mostrar modal usando el ModalService
                _modalService.MostrarRegistrarMantenimiento(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error abriendo registrador de mantenimiento");
            }
        }

        public bool CanOpenRegistrar(SeguimientoMantenimiento? seguimiento)
        {
            return seguimiento != null;
        }
    }
}
