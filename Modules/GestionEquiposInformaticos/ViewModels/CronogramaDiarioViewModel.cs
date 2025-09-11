using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Interfaces; // Reutilizamos servicios existentes de cronograma
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels
{
    /// <summary>
    /// ViewModel para el cronograma diario (vista semanal detallada L-V) correspondiente al módulo GestionEquiposInformaticos.
    /// Respeta SRP: solo coordina carga y organización semanal diaria de mantenimientos planificados.
    /// </summary>
    public partial class CronogramaDiarioViewModel : ObservableObject
    {
        private readonly ICronogramaService _cronogramaService;
        private readonly IPlanCronogramaService _planCronogramaService;
        private readonly IGestLogLogger _logger;

        public CronogramaDiarioViewModel(ICronogramaService cronogramaService, IPlanCronogramaService planCronogramaService, IGestLogLogger logger)
        {
            _cronogramaService = cronogramaService;
            _planCronogramaService = planCronogramaService;
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
                
                // Cargar cronogramas de mantenimiento existentes
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

                // Cargar planes de cronograma de equipos
                var planesEquipos = await _planCronogramaService.GetAllAsync();
                var planesActivos = planesEquipos.Where(p => p.Activo).ToList();
                
                foreach (var plan in planesActivos)
                {
                    // DiaProgramado: 1=Lunes, 2=Martes, ..., 7=Domingo
                    // Solo mostramos L-V (1-5)
                    if (plan.DiaProgramado >= 1 && plan.DiaProgramado <= 5)
                    {
                        // Crear un DTO temporal para mostrar en la vista
                        var planDto = new CronogramaMantenimientoDto
                        {
                            Codigo = plan.CodigoEquipo,
                            Nombre = plan.Descripcion,
                            Marca = "Plan Semanal",
                            Sede = plan.Responsable,
                            Anio = SelectedYear
                        };
                        
                        int dayIndex = plan.DiaProgramado - 1; // Convertir a 0-based index
                        Days[dayIndex].Items.Add(planDto);
                    }
                }

                int totalItems = Planificados.Count + planesActivos.Count(p => p.DiaProgramado >= 1 && p.DiaProgramado <= 5);
                StatusMessage = $"Semana {SelectedWeek}: {Planificados.Count} mantenimientos, {planesActivos.Count(p => p.DiaProgramado >= 1 && p.DiaProgramado <= 5)} planes.";
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
            
            // Verificar si es un plan semanal o mantenimiento tradicional
            if (mantenimiento.Marca == "Plan Semanal")
            {
                StatusMessage = $"Ejecutar plan semanal para {mantenimiento.Codigo}";
                // Aquí podríamos abrir un diálogo específico para ejecutar planes semanales
            }
            else
            {
                StatusMessage = $"Registrar mantenimiento para {mantenimiento.Codigo}";
                // Aquí se abre el diálogo tradicional de registro de mantenimiento
            }
        }

        [RelayCommand]
        private async Task GestionarPlanesAsync()
        {
            try
            {
                // Abrir diálogo para gestionar planes existentes
                var dialog = new GestLog.Views.Tools.GestionEquipos.GestionarPlanesDialog(
                    _planCronogramaService, _logger);
                
                // Obtener la ventana padre actual
                var parentWindow = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;
                
                if (parentWindow != null)
                {
                    dialog.Owner = parentWindow;
                }

                var result = dialog.ShowDialog();
                if (result == true)
                {
                    StatusMessage = "Gestión de planes completada";
                    _logger.LogInformation("[CronogramaDiarioViewModel] Gestión de planes completada");
                    
                    // Refrescar la vista para mostrar cambios
                    await RefreshAsync(CancellationToken.None);
                }
                else
                {
                    StatusMessage = "Gestión de planes cancelada";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaDiarioViewModel] Error al gestionar planes");
                StatusMessage = "Error al abrir la gestión de planes";
            }
        }

        [RelayCommand]
        private async Task CrearPlanAsync()
        {
            try
            {
                // Abrir diálogo para crear un nuevo plan
                var dialog = new GestLog.Views.Tools.GestionEquipos.CrearPlanCronogramaDialog();
                
                // Obtener la ventana padre actual
                var parentWindow = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;
                
                if (parentWindow != null)
                {
                    dialog.Owner = parentWindow;
                }

                var result = dialog.ShowDialog();
                if (result == true && dialog.PlanCreado != null)
                {
                    StatusMessage = $"Plan creado exitosamente para equipo {dialog.PlanCreado.CodigoEquipo}";
                    _logger.LogInformation("[CronogramaDiarioViewModel] Plan creado exitosamente: {PlanId}", dialog.PlanCreado.PlanId);
                    
                    // Refrescar la vista para mostrar el nuevo plan
                    await RefreshAsync(CancellationToken.None);
                }
                else
                {
                    StatusMessage = "Creación de plan cancelada";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaDiarioViewModel] Error al crear plan");
                StatusMessage = "Error al abrir el diálogo de creación de plan";
            }
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
