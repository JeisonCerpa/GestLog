// filepath: e:\Softwares\GestLog\Modules\GestionEquiposInformaticos\ViewModels\RegistroEjecucionPlanViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Services.Core.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces; // añadido para IPlanCronogramaService

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels
{
    public class ChecklistItemExecution : ObservableObject
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        private bool _completado;
        public bool Completado { get => _completado; set => SetProperty(ref _completado, value); }
        private string? _observacion;
        public string? Observacion { get => _observacion; set => SetProperty(ref _observacion, value); }
    }

    public partial class RegistroEjecucionPlanViewModel : ObservableObject
    {
        private readonly IPlanCronogramaService _planService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty] private Guid planId;
        [ObservableProperty] private string codigoEquipo = string.Empty;
        [ObservableProperty] private string descripcionPlan = string.Empty;
        [ObservableProperty] private string responsablePlan = string.Empty;
        [ObservableProperty] private int anioISO;
        [ObservableProperty] private int semanaISO;
        [ObservableProperty] private DateTime fechaObjetivo;
        [ObservableProperty] private DateTime fechaEjecucion = DateTime.Now;
        [ObservableProperty] private ObservableCollection<ChecklistItemExecution> checklist = new();
        [ObservableProperty] private string statusMessage = string.Empty;

        public RegistroEjecucionPlanViewModel(IPlanCronogramaService planService, IGestLogLogger logger)
        {
            _planService = planService;
            _logger = logger;
        }

        public void Load(PlanCronogramaEquipo plan, int anio, int semana, string usuario)
        {
            PlanId = plan.PlanId;
            CodigoEquipo = plan.CodigoEquipo;
            DescripcionPlan = plan.Descripcion;
            ResponsablePlan = string.IsNullOrWhiteSpace(plan.Responsable) ? usuario : plan.Responsable;
            AnioISO = anio;
            SemanaISO = semana;
            // Calcular fecha objetivo (lunes de la semana + diaProgramado-1)
            var jan1 = new DateTime(anio,1,1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var weekNum = semana;
            if (firstWeek <= 1) weekNum -= 1;
            var result = firstThursday.AddDays(weekNum * 7).AddDays(-3); // lunes
            FechaObjetivo = result.AddDays(plan.DiaProgramado -1);
            FechaEjecucion = DateTime.Now;
            Checklist.Clear();
            if (!string.IsNullOrWhiteSpace(plan.ChecklistJson))
            {
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(plan.ChecklistJson);
                    if (doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var it in items.EnumerateArray())
                        {
                            var id = it.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
                            var desc = it.TryGetProperty("descripcion", out var dEl) ? dEl.GetString() ?? string.Empty : string.Empty;
                            Checklist.Add(new ChecklistItemExecution { Id = id, Descripcion = desc });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[RegistroEjecucionPlanViewModel] Error parseando checklist JSON");
                }
            }
        }

        public string BuildResultadoJson()
        {
            var payload = new
            {
                fechaEjecucion = FechaEjecucion,
                items = Checklist.Select(c => new { c.Id, c.Descripcion, c.Completado, c.Observacion })
            };
            return System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        [RelayCommand]
        private async Task GuardarAsync()
        {
            try
            {
                var resultado = BuildResultadoJson();
                await _planService.RegistrarEjecucionAsync(PlanId, AnioISO, SemanaISO, FechaEjecucion, ResponsablePlan, resultado);
                StatusMessage = "Ejecución guardada";
                OnEjecucionRegistrada?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistroEjecucionPlanViewModel] Error guardando ejecución");
                StatusMessage = "Error al guardar";
            }
        }

        public event EventHandler? OnEjecucionRegistrada;
    }
}
