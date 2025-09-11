using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Services.Core.Logging;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels
{
    /// <summary>
    /// ViewModel para el diálogo de creación de planes de cronograma de equipos informáticos
    /// </summary>
    public partial class CrearPlanCronogramaViewModel : ObservableObject
    {
        private readonly IPlanCronogramaService _planCronogramaService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private string codigoEquipo = string.Empty;

        [ObservableProperty]
        private string descripcion = string.Empty;

        [ObservableProperty]
        private string responsable = string.Empty;        [ObservableProperty]
        private int diaEjecucion = 1; // Lunes por defecto

        [ObservableProperty]
        private string checklistJson = string.Empty;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isSaving = false;

        // Opciones para el día de la semana
        public ObservableCollection<DiaOpcion> DiasDisponibles { get; } = new ObservableCollection<DiaOpcion>
        {
            new DiaOpcion { Valor = 1, Nombre = "Lunes" },
            new DiaOpcion { Valor = 2, Nombre = "Martes" },
            new DiaOpcion { Valor = 3, Nombre = "Miércoles" },
            new DiaOpcion { Valor = 4, Nombre = "Jueves" },
            new DiaOpcion { Valor = 5, Nombre = "Viernes" },
            new DiaOpcion { Valor = 6, Nombre = "Sábado" },
            new DiaOpcion { Valor = 0, Nombre = "Domingo" }
        };

        public CrearPlanCronogramaViewModel(IPlanCronogramaService planCronogramaService, IGestLogLogger logger)
        {
            _planCronogramaService = planCronogramaService ?? throw new ArgumentNullException(nameof(planCronogramaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configurar checklist por defecto
            ChecklistJson = """
            {
              "items": [
                { "id": 1, "descripcion": "Verificar estado del hardware", "completado": false },
                { "id": 2, "descripcion": "Limpiar archivos temporales", "completado": false },
                { "id": 3, "descripcion": "Actualizar software crítico", "completado": false },
                { "id": 4, "descripcion": "Verificar funcionamiento de periféricos", "completado": false },
                { "id": 5, "descripcion": "Revisar espacio en disco", "completado": false }
              ]
            }
            """;
        }

        [RelayCommand]
        private async Task CrearPlanAsync()
        {
            if (IsSaving) return;

            try
            {
                IsSaving = true;
                StatusMessage = "Creando plan...";

                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(CodigoEquipo))
                {
                    StatusMessage = "El código del equipo es obligatorio";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Descripcion))
                {
                    StatusMessage = "La descripción es obligatoria";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Responsable))
                {
                    StatusMessage = "El responsable es obligatorio";
                    return;
                }

                // Validar JSON del checklist
                if (!string.IsNullOrWhiteSpace(ChecklistJson))
                {
                    try
                    {
                        System.Text.Json.JsonDocument.Parse(ChecklistJson);
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        StatusMessage = $"El formato del checklist no es válido: {ex.Message}";
                        return;
                    }
                }                // Crear el plan
                var nuevoPlan = new PlanCronogramaEquipo
                {
                    PlanId = Guid.NewGuid(),
                    CodigoEquipo = CodigoEquipo.Trim(),
                    Descripcion = Descripcion.Trim(),
                    Responsable = Responsable.Trim(),
                    DiaProgramado = (byte)DiaEjecucion,
                    ChecklistJson = string.IsNullOrWhiteSpace(ChecklistJson) ? null : ChecklistJson.Trim(),
                    FechaCreacion = DateTime.Now,
                    Activo = true
                };

                var planCreado = await _planCronogramaService.CreateAsync(nuevoPlan);                _logger.LogInformation("[CrearPlanCronograma] Plan creado exitosamente: {PlanId} para equipo {CodigoEquipo}", 
                    planCreado.PlanId, planCreado.CodigoEquipo);

                StatusMessage = "Plan creado exitosamente";
                
                // Notificar éxito
                PlanCreado?.Invoke(planCreado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CrearPlanCronograma] Error al crear plan para equipo {CodigoEquipo}", CodigoEquipo);
                StatusMessage = $"Error al crear el plan: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Evento que se dispara cuando se crea exitosamente un plan
        /// </summary>
        public event Action<PlanCronogramaEquipo>? PlanCreado;

        /// <summary>
        /// Método para calcular información de la semana ISO 8601 actual para mostrar al usuario
        /// </summary>
        public string ObtenerInfoSemanaActual()
        {
            var hoy = DateTime.Today;
            var cultura = CultureInfo.CurrentCulture;
            var calendario = cultura.Calendar;
            
            var semanaISO = calendario.GetWeekOfYear(hoy, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var anioISO = hoy.Year;
            
            // Ajustar año ISO si estamos en la primera semana del año
            if (hoy.Month == 1 && semanaISO > 50)
            {
                anioISO = hoy.Year - 1;
            }
            else if (hoy.Month == 12 && semanaISO == 1)
            {
                anioISO = hoy.Year + 1;
            }

            return $"Año ISO: {anioISO}, Semana: {semanaISO}";
        }
    }

    /// <summary>
    /// Clase auxiliar para representar opciones de días de la semana
    /// </summary>
    public class DiaOpcion
    {
        public int Valor { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
