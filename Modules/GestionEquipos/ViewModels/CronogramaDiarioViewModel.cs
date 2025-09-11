using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Interfaces; // Reutilizamos servicios existentes de cronograma
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquipos.ViewModels
{
    /// <summary>
    /// ViewModel para el cronograma diario (vista semanal detallada L-V) correspondiente al módulo GestionEquipos.
    /// Respeta SRP: solo coordina carga y organización semanal diaria de mantenimientos planificados.
    /// </summary>
    public partial class CronogramaDiarioViewModel : ObservableObject
    {
        private readonly ICronogramaService _cronogramaService;
        private readonly IGestLogLogger _logger;

        public CronogramaDiarioViewModel(ICronogramaService cronogramaService, IGestLogLogger logger)
        {
            _cronogramaService = cronogramaService;
            _logger = logger;
            SelectedYear = System.DateTime.Now.Year;
        }

        [ObservableProperty]
        private ObservableCollection<int> weeks = new();
        [ObservableProperty]
        private int selectedWeek;
        [ObservableProperty]
        private ObservableCollection<int> years = new();
        [ObservableProperty]
        private int selectedYear;
        [ObservableProperty]
        private ObservableCollection<CronogramaMantenimientoDto> planificados = new();
        [ObservableProperty]
        private ObservableCollection<DayScheduleViewModel> days = new();
        [ObservableProperty]
        private bool isLoading;
        [ObservableProperty]
        private string? statusMessage;

        partial void OnSelectedWeekChanged(int value) => _ = RefreshAsync(CancellationToken.None);
        partial void OnSelectedYearChanged(int value) => _ = RefreshAsync(CancellationToken.None);

        public async Task LoadAsync(CancellationToken ct)
        {
            if (Weeks.Count == 0)
                for (int i = 1; i <= 52; i++) Weeks.Add(i);
            if (Years.Count == 0)
                Years.Add(SelectedYear);
            if (SelectedWeek == 0)
            {
                var hoy = System.DateTime.Now;
                var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                SelectedWeek = cal.GetWeekOfYear(hoy, System.Globalization.CalendarWeekRule.FirstFourDayWeek, System.DayOfWeek.Monday);
            }
            await RefreshAsync(ct);
        }

        private async Task RefreshAsync(CancellationToken ct)
        {
            if (SelectedWeek <= 0 || SelectedWeek > 52 || IsLoading) return;
            try
            {
                IsLoading = true;
                StatusMessage = $"Cargando semana {SelectedWeek}...";
                Planificados.Clear();
                Days.Clear();
                var dias = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };
                foreach (var d in dias) Days.Add(new DayScheduleViewModel(d));
                var cronogramas = await _cronogramaService.GetCronogramasAsync();
                foreach (var c in cronogramas)
                {
                    if (c.Anio == SelectedYear && c.Semanas != null && c.Semanas.Length >= SelectedWeek && c.Semanas[SelectedWeek - 1])
                    {
                        Planificados.Add(c);
                        int index = 0;
                        if (!string.IsNullOrWhiteSpace(c.Codigo))
                            index = (System.Math.Abs(c.Codigo.GetHashCode()) % 5);
                        Days[index].Items.Add(c);
                    }
                }
                StatusMessage = $"Semana {SelectedWeek}: {Planificados.Count} mantenimientos.";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "[CronogramaDiarioViewModel] Error al refrescar cronograma diario");
                StatusMessage = "Error cargando cronograma diario";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void OpenRegistrar(CronogramaMantenimientoDto? mantenimiento)
        {
            if (mantenimiento == null)
            {
                StatusMessage = "Elemento no válido";
                return;
            }
            StatusMessage = $"Registrar mantenimiento para {mantenimiento.Codigo}";
        }
    }

    public partial class DayScheduleViewModel : ObservableObject
    {
        public DayScheduleViewModel(string name) => Name = name;
        public string Name { get; }
        [ObservableProperty]
        private ObservableCollection<CronogramaMantenimientoDto> items = new();
    }
}
