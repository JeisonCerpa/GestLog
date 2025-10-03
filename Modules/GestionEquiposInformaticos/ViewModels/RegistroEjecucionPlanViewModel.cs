// filepath: e:\Softwares\GestLog\Modules\GestionEquiposInformaticos\ViewModels\RegistroEjecucionPlanViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Services.Core.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces; // añadido para IPlanCronogramaService
using System.Collections.Specialized; // para CollectionChanged
using System.ComponentModel; // para PropertyChanged
using System.Text;
using System.Globalization;

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
        // JSON bruto del checklist (solo para diagnóstico)
        [ObservableProperty] private string checklistJsonRaw = string.Empty;

        // Nuevas propiedades para contador dinámico
        [ObservableProperty] private int totalItems;
        [ObservableProperty] private int completedItems;

        // Resumen en vivo: porcentaje completado y texto resumen con observaciones
        [ObservableProperty] private int percentComplete;
        [ObservableProperty] private string resumenEnVivo = string.Empty;

        public RegistroEjecucionPlanViewModel(IPlanCronogramaService planService, IGestLogLogger logger)
        {
            _planService = planService;
            _logger = logger;

            // Suscribirse a cambios en la colección para gestionar suscripciones a los items
            Checklist.CollectionChanged += Checklist_CollectionChanged;
        }

        private void Checklist_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (ChecklistItemExecution? oldItem in e.OldItems)
                {
                    if (oldItem is not null)
                        oldItem.PropertyChanged -= Item_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (ChecklistItemExecution? newItem in e.NewItems)
                {
                    if (newItem is not null)
                        newItem.PropertyChanged += Item_PropertyChanged;
                }
            }
            RecalculateCounts();
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Recalcular tanto cuando cambie el estado de completado como cuando cambie la observación
            if (e.PropertyName == nameof(ChecklistItemExecution.Completado) || e.PropertyName == nameof(ChecklistItemExecution.Observacion))
            {
                RecalculateCounts();
            }
        }

        private void RecalculateCounts()
        {
            TotalItems = Checklist?.Count ?? 0;
            CompletedItems = Checklist?.Count(c => c.Completado) ?? 0;

            // Calcular porcentaje (si no hay items, consideramos 100% para evitar dividir por cero)
            if (TotalItems == 0)
                PercentComplete = 100;
            else
                PercentComplete = (int)Math.Round(CompletedItems * 100.0 / TotalItems);

            // Construir resumen en vivo: línea con X/Y (P%) y hasta 3 observaciones no vacías
            var observaciones = (Checklist ?? new System.Collections.ObjectModel.ObservableCollection<ChecklistItemExecution>())
                .Where(c => !string.IsNullOrWhiteSpace(c.Observacion))
                .Select(c => c.Observacion!.Trim())
                .ToList();
            var topObs = observaciones.Take(3).ToList();
            var obsText = topObs.Any() ? ("Observaciones: " + string.Join("; ", topObs)) : string.Empty;

            ResumenEnVivo = $"Completado: {CompletedItems}/{TotalItems} ({PercentComplete}%)" + (string.IsNullOrEmpty(obsText) ? string.Empty : "\n" + obsText);
        }

        public void Load(PlanCronogramaEquipo plan, int anio, int semana, string usuario)
        {
            // Evitar desreferencia si plan es null
            if (plan == null)
            {
                _logger.LogWarning("[RegistroEjecucionPlanViewModel] Load recibido con plan null");
                return;
            }

            // Log inicial para depuración: confirmar que Load es invocado y valores principales
            _logger.LogInformation("[RegistroEjecucionPlanViewModel] Load llamado para PlanId={PlanId}, CodigoEquipo={Codigo}", new object[] { plan.PlanId, plan.CodigoEquipo ?? string.Empty });

            PlanId = plan.PlanId;
            CodigoEquipo = plan.CodigoEquipo ?? string.Empty;
            DescripcionPlan = plan.Descripcion ?? string.Empty;
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

            // Exponer el JSON bruto en la propiedad diagnóstica
            ChecklistJsonRaw = plan.ChecklistJson ?? string.Empty;

            _logger.LogInformation("[RegistroEjecucionPlanViewModel] DescripcionPlan exists: {HasDesc}, ResponsablePlan set to: {Responsable}", new object[] { !string.IsNullOrWhiteSpace(DescripcionPlan), ResponsablePlan ?? string.Empty });

            if (!string.IsNullOrWhiteSpace(plan.ChecklistJson))
            {
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(plan.ChecklistJson);
                    var root = doc.RootElement;

                    // Detectar el array de items: soportar { "items": [...] } o directamente un array en raíz
                    System.Text.Json.JsonElement itemsElement = default;
                    bool hasItems = false;

                    if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        itemsElement = root;
                        hasItems = true;
                    }
                    else if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        // Intentar varias claves comunes con diferentes casing
                        string[] candidateKeys = new[] { "items", "Items" };
                        foreach (var k in candidateKeys)
                        {
                            if (root.TryGetProperty(k, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                itemsElement = el;
                                hasItems = true;
                                break;
                            }
                        }
                    }

                    int added = 0;
                    if (hasItems)
                    {
                        foreach (var it in itemsElement.EnumerateArray())
                        {
                            try
                            {
                                int id = 0;
                                string desc = string.Empty;

                                if (it.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    // Id puede venir como 'id' o 'Id'
                                    if (it.TryGetProperty("id", out var idEl) && idEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                                        id = idEl.GetInt32();
                                    else if (it.TryGetProperty("Id", out var idEl2) && idEl2.ValueKind == System.Text.Json.JsonValueKind.Number)
                                        id = idEl2.GetInt32();

                                    // Descripción con variantes de casing
                                    if (it.TryGetProperty("descripcion", out var dEl) && dEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                        desc = dEl.GetString() ?? string.Empty;
                                    else if (it.TryGetProperty("Descripcion", out var dEl2) && dEl2.ValueKind == System.Text.Json.JsonValueKind.String)
                                        desc = dEl2.GetString() ?? string.Empty;
                                    else if (it.TryGetProperty("description", out var dEl3) && dEl3.ValueKind == System.Text.Json.JsonValueKind.String)
                                        desc = dEl3.GetString() ?? string.Empty;
                                }
                                else if (it.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    // Formato alternativo: array de strings
                                    desc = it.GetString() ?? string.Empty;
                                }

                                // Si no hay descripción, intentar recuperar de alguna propiedad conocida
                                if (string.IsNullOrWhiteSpace(desc))
                                {
                                    // intentar convertir el elemento completo a string
                                    desc = it.ToString();
                                }

                                Checklist.Add(new ChecklistItemExecution { Id = id, Descripcion = desc });
                                added++;
                            }
                            catch (Exception innerEx)
                            {
                                _logger.LogWarning(innerEx, "[RegistroEjecucionPlanViewModel] Error parseando item de checklist, se omite item");
                                // continuar con siguiente item
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[RegistroEjecucionPlanViewModel] No se encontró un array de items en ChecklistJson (se esperaba 'items' o raíz array)");
                    }

                    // Si la BD contenía 10 items pero algún item quedó sin descripción, registrar advertencia
                    if (added > 0 && root.ValueKind == System.Text.Json.JsonValueKind.Array && added < root.GetArrayLength())
                    {
                        _logger.LogWarning("[RegistroEjecucionPlanViewModel] Se agregaron {Added} items desde JSON pero el array original tiene diferente longitud.", new object[] { added });
                    }

                    _logger.LogInformation("[RegistroEjecucionPlanViewModel] ChecklistJson parseado, items agregados: {Count}", new object[] { Checklist.Count });

                    // Se removió logging detallado por item para reducir ruido en producción.

                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[RegistroEjecucionPlanViewModel] Error parseando checklist JSON");
                }
            }

            // Inicializar items por defecto como completados si la descripción coincide
            var defaultDescriptions = new[]
            {
                "Limpieza del Software con Antivirus",
                "Eliminación de Archivos Temporales",
                "Respaldo de archivos digitales"
            };

            string NormalizeForCompare(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                var normalized = s.Normalize(NormalizationForm.FormD);
                var sb = new StringBuilder();
                foreach (var ch in normalized)
                {
                    var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (cat != UnicodeCategory.NonSpacingMark) sb.Append(ch);
                }
                return sb.ToString().ToLowerInvariant().Trim();
            }

            var defaultsSet = new HashSet<string>(defaultDescriptions.Select(d => NormalizeForCompare(d)));
            foreach (var item in Checklist)
            {
                if (!item.Completado)
                {
                    var norm = NormalizeForCompare(item.Descripcion);
                    if (defaultsSet.Contains(norm))
                    {
                        item.Completado = true; // esto disparará PropertyChanged y actualizará los contadores
                    }
                }
            }

            // Asegurar que el contador refleja lo cargado
            RecalculateCounts();

            _logger.LogInformation("[RegistroEjecucionPlanViewModel] Load completo. FechaObjetivo={FechaObjetivo}, FechaEjecucion={FechaEjecucion}, TotalItems={Total}", new object[] { FechaObjetivo, FechaEjecucion, TotalItems });
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
