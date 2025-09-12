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
using System.ComponentModel;
using System.Collections.Generic;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels
{    /// <summary>
    /// Clase para representar una tarea del plan de cronograma (plantilla de tareas a realizar)
    /// </summary>
    public partial class TareaChecklistViewModel : ObservableObject
    {
        [ObservableProperty]
        private string descripcion = string.Empty;
        
        public int Id { get; set; }
        
        public TareaChecklistViewModel(int id, string descripcion)
        {
            Id = id;
            Descripcion = descripcion;
        }
    }

    /// <summary>
    /// Clase para representar un equipo en el ComboBox con información completa
    /// </summary>
    public class EquipoComboItem
    {
        public string Codigo { get; set; } = string.Empty;
        public string NombreEquipo { get; set; } = string.Empty;
        public string UsuarioAsignado { get; set; } = string.Empty;
        
        /// <summary>
        /// Texto completo mostrado en el ComboBox
        /// </summary>
        public string DisplayText => 
            $"{Codigo} - {(!string.IsNullOrWhiteSpace(NombreEquipo) ? NombreEquipo : "Sin nombre")} " +
            $"({(!string.IsNullOrWhiteSpace(UsuarioAsignado) ? UsuarioAsignado : "Sin asignar")})";
            
        /// <summary>
        /// Texto para búsqueda (incluye todos los campos)
        /// </summary>
        public string SearchText => 
            $"{Codigo} {NombreEquipo} {UsuarioAsignado}".ToLowerInvariant();
    }

    /// <summary>
    /// ViewModel para el diálogo de creación de planes de cronograma de equipos informáticos
    /// </summary>
    public partial class CrearPlanCronogramaViewModel : ObservableObject
    {
        private readonly IPlanCronogramaService _planCronogramaService;
        private readonly IEquipoInformaticoService _equipoInformaticoService;
        private readonly IGestLogLogger _logger;        // Propiedad calculada para obtener el código del equipo seleccionado
        public string CodigoEquipo => EquipoSeleccionado?.Codigo ?? string.Empty;

        // Listas para el ComboBox de equipos
        private List<EquipoComboItem> _todosLosEquipos = new();
        
        [ObservableProperty]
        private ObservableCollection<EquipoComboItem> equiposDisponibles = new();
        
        [ObservableProperty]
        private EquipoComboItem? equipoSeleccionado;
          [ObservableProperty]
        private string filtroEquipo = string.Empty;

        [ObservableProperty]
        private string descripcion = string.Empty;

        [ObservableProperty]
        private string responsable = string.Empty;

        [ObservableProperty]
        private int diaEjecucion = 1; // Lunes por defecto

        // Lista de tareas del checklist (reemplaza el JSON directo)
        [ObservableProperty]
        private ObservableCollection<TareaChecklistViewModel> tareasChecklist = new();
        
        [ObservableProperty]
        private string nuevaTareaTexto = string.Empty;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isSaving = false;

        // Propiedad calculada para generar el JSON automáticamente
        public string ChecklistJson => GenerarChecklistJson();

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

        public CrearPlanCronogramaViewModel(IPlanCronogramaService planCronogramaService, IEquipoInformaticoService equipoInformaticoService, IGestLogLogger logger)
        {
            _planCronogramaService = planCronogramaService ?? throw new ArgumentNullException(nameof(planCronogramaService));
            _equipoInformaticoService = equipoInformaticoService ?? throw new ArgumentNullException(nameof(equipoInformaticoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Cargar equipos informáticos
            _ = CargarEquiposAsync();
            
            // Configurar tareas por defecto del checklist
            ConfigurarTareasChecklistPorDefecto();
        }        /// <summary>
        /// Configura las tareas por defecto del checklist
        /// </summary>
        private void ConfigurarTareasChecklistPorDefecto()
        {
            TareasChecklist.Clear();
            TareasChecklist.Add(new TareaChecklistViewModel(1, "Limpieza del Software con antivirus"));
            TareasChecklist.Add(new TareaChecklistViewModel(2, "Eliminación de archivos temporales"));
            TareasChecklist.Add(new TareaChecklistViewModel(3, "Respaldo de archivos digitales"));
            TareasChecklist.Add(new TareaChecklistViewModel(4, "Actualizaciones del sistema"));
            TareasChecklist.Add(new TareaChecklistViewModel(5, "Limpieza superficial de tarjetas"));
            TareasChecklist.Add(new TareaChecklistViewModel(6, "Limpieza de disipadores de calor"));
            TareasChecklist.Add(new TareaChecklistViewModel(7, "Limpieza de contactos de memoria RAM"));
            TareasChecklist.Add(new TareaChecklistViewModel(8, "Limpieza de teclado"));
            TareasChecklist.Add(new TareaChecklistViewModel(9, "Limpieza de mouse"));
            TareasChecklist.Add(new TareaChecklistViewModel(10, "Limpieza de monitor"));
        }

        /// <summary>
        /// Genera el JSON del checklist a partir de la lista de tareas
        /// </summary>
        private string GenerarChecklistJson()
        {
            if (!TareasChecklist.Any())
                return string.Empty;            try
            {
                var items = TareasChecklist.Select(t => new
                {
                    id = t.Id,
                    descripcion = t.Descripcion
                });

                var checklist = new { items };
                return System.Text.Json.JsonSerializer.Serialize(checklist, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Agrega una nueva tarea al checklist
        /// </summary>
        [RelayCommand]
        private void AgregarTarea()
        {
            if (string.IsNullOrWhiteSpace(NuevaTareaTexto)) return;

            var nuevoId = TareasChecklist.Any() ? TareasChecklist.Max(t => t.Id) + 1 : 1;
            TareasChecklist.Add(new TareaChecklistViewModel(nuevoId, NuevaTareaTexto.Trim()));
            NuevaTareaTexto = string.Empty;
        }

        /// <summary>
        /// Elimina una tarea del checklist
        /// </summary>
        [RelayCommand]
        private void EliminarTarea(TareaChecklistViewModel? tarea)
        {
            if (tarea != null)
            {
                TareasChecklist.Remove(tarea);
            }
        }

        /// <summary>
        /// Carga todos los equipos informáticos disponibles
        /// </summary>
        private async Task CargarEquiposAsync()
        {
            try
            {
                var equipos = await _equipoInformaticoService.GetAllAsync();
                
                _todosLosEquipos = equipos.Select(e => new EquipoComboItem
                {
                    Codigo = e.Codigo ?? string.Empty,
                    NombreEquipo = e.NombreEquipo ?? string.Empty,
                    UsuarioAsignado = e.UsuarioAsignado ?? string.Empty
                }).OrderBy(e => e.Codigo).ToList();

                // Inicializar la lista filtrada con todos los equipos
                AplicarFiltroEquipos();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CrearPlanCronograma] Error al cargar equipos informáticos");
                StatusMessage = "Error al cargar equipos informáticos";
            }
        }

        /// <summary>
        /// Aplica el filtro a la lista de equipos disponibles
        /// </summary>
        private void AplicarFiltroEquipos()
        {
            EquiposDisponibles.Clear();
            
            var filtro = FiltroEquipo.ToLowerInvariant().Trim();
            var equiposFiltrados = string.IsNullOrWhiteSpace(filtro)
                ? _todosLosEquipos
                : _todosLosEquipos.Where(e => e.SearchText.Contains(filtro));

            foreach (var equipo in equiposFiltrados.Take(50)) // Limitar a 50 resultados
            {
                EquiposDisponibles.Add(equipo);
            }
        }

        /// <summary>
        /// Método llamado cuando cambia el filtro de equipo
        /// </summary>
        partial void OnFiltroEquipoChanged(string value)
        {
            AplicarFiltroEquipos();
        }        /// <summary>
        /// Método para establecer un equipo inicial si se proporciona un código
        /// </summary>
        public async Task EstablecerEquipoInicialAsync(string? codigoEquipo)
        {
            if (string.IsNullOrWhiteSpace(codigoEquipo)) return;
            
            // Esperar a que se carguen los equipos si es necesario
            var intentos = 0;
            while (_todosLosEquipos.Count == 0 && intentos < 10)
            {
                await Task.Delay(100);
                intentos++;
            }
            
            var equipo = _todosLosEquipos.FirstOrDefault(e => 
                string.Equals(e.Codigo, codigoEquipo, StringComparison.OrdinalIgnoreCase));
            
            if (equipo != null)
            {
                EquipoSeleccionado = equipo;
                FiltroEquipo = equipo.Codigo; // Mostrar el código en el filtro
            }
        }

        /// <summary>
        /// Método para establecer un equipo inicial si se proporciona un código (síncrono)
        /// </summary>
        public void EstablecerEquipoInicial(string? codigoEquipo)
        {
            if (string.IsNullOrWhiteSpace(codigoEquipo)) return;
            
            var equipo = _todosLosEquipos.FirstOrDefault(e => 
                string.Equals(e.Codigo, codigoEquipo, StringComparison.OrdinalIgnoreCase));
            
            if (equipo != null)
            {
                EquipoSeleccionado = equipo;
                FiltroEquipo = equipo.Codigo; // Mostrar el código en el filtro
            }
        }

        [RelayCommand]
        private async Task CrearPlanAsync()
        {
            if (IsSaving) return;

            try
            {
                IsSaving = true;
                StatusMessage = "Creando plan...";                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(CodigoEquipo))
                {
                    StatusMessage = "El código del equipo es obligatorio";
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
                    Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim(),
                    Responsable = string.IsNullOrWhiteSpace(Responsable) ? null : Responsable.Trim(),
                    DiaProgramado = (byte)DiaEjecucion,
                    ChecklistJson = string.IsNullOrWhiteSpace(ChecklistJson) ? null : ChecklistJson.Trim(),
                    FechaCreacion = DateTime.Now,
                    Activo = true
                };

                var planCreado = await _planCronogramaService.CreateAsync(nuevoPlan);
                _logger.LogInformation("[CrearPlanCronograma] Plan creado exitosamente: {PlanId} para equipo {CodigoEquipo}", 
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
