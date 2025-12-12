using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Services.Core.Logging;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma
{
    /// <summary>
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

        // Override para que ComboBox.Text (cuando usa SelectedItem.ToString()) sea solo el Código.
        public override string ToString() => Codigo;
    }

    /// <summary>
    /// ViewModel para el diálogo de creación de planes de cronograma de equipos informáticos
    /// </summary>
    public partial class CrearPlanCronogramaViewModel : ObservableObject
    {
        private readonly IPlanCronogramaService _planCronogramaService;
        private readonly IEquipoInformaticoService _equipoInformaticoService;
        private readonly IGestLogLogger _logger;

        // Siguiendo exactamente el patrón que funciona en AgregarEquipoInformaticoViewModel
        [ObservableProperty]
        private ObservableCollection<EquipoComboItem> equiposDisponibles = new();
        
        [ObservableProperty]
        private ObservableCollection<EquipoComboItem> equiposFiltrados = new();
        
        [ObservableProperty]
        private EquipoComboItem? equipoSeleccionado;
        
        [ObservableProperty]
        private string filtroEquipo = string.Empty;

        // Flag para evitar manejar cambios de filtro cuando se actualiza programáticamente
        private bool _suppressFiltroEquipoChanged = false;

        [ObservableProperty]
        private string codigoEquipo = string.Empty;

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

        [ObservableProperty]
        private string? selectedEquipoCodigo;

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
        }

        /// <summary>
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
                return string.Empty;

            try
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
        /// Cargar equipos informáticos desde el servicio
        /// </summary>
        public async Task CargarEquiposAsync()
        {
            try
            {
                var equipos = await _equipoInformaticoService.GetAllAsync();

                // Obtener códigos de equipos que ya tienen un plan activo para excluirlos del listado
                HashSet<string> codigosConPlanActivo = new(StringComparer.OrdinalIgnoreCase);
                try
                {
                    var todosLosPlanes = await _planCronogramaService.GetAllAsync();
                    if (todosLosPlanes != null)
                    {
                        codigosConPlanActivo = todosLosPlanes
                            .Where(p => p.Activo && !string.IsNullOrWhiteSpace(p.CodigoEquipo))
                            .Select(p => p.CodigoEquipo!)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[CrearPlanCronograma] No se pudo obtener la lista de planes para filtrar equipos; se mostrarán todos los equipos activos");
                    // En caso de error al obtener planes, dejamos codigosConPlanActivo vacío para no bloquear la UX
                    codigosConPlanActivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                
                // Filtrar solo equipos cuyo Estado sea "Activo" y que NO tengan ya un plan activo
                var equiposActivos = equipos
                    .Where(e => string.Equals(e.Estado, "Activo", StringComparison.OrdinalIgnoreCase)
                                && !codigosConPlanActivo.Contains(e.Codigo))
                    .OrderBy(e => e.Codigo)
                    .ToList();

                var equiposCombo = equiposActivos.Select(e => new EquipoComboItem
                {
                    Codigo = e.Codigo ?? string.Empty,
                    NombreEquipo = e.NombreEquipo ?? string.Empty,
                    UsuarioAsignado = e.UsuarioAsignado ?? string.Empty
                }).OrderBy(e => e.Codigo).ToList();

                // Actualizar ambas listas exactamente como PersonasDisponibles y PersonasFiltradas
                EquiposDisponibles.Clear();
                foreach (var equipo in equiposCombo)
                {
                    EquiposDisponibles.Add(equipo);
                }
                
                EquiposFiltrados.Clear();
                foreach (var equipo in equiposCombo)
                {
                    EquiposFiltrados.Add(equipo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CrearPlanCronograma] Error al cargar equipos informáticos");
                StatusMessage = "Error al cargar equipos informáticos";
            }
        }

        /// <summary>
        /// Se ejecuta automáticamente cuando cambia la selección del equipo
        /// Actualiza CodigoEquipo y FiltroEquipo cuando se selecciona un elemento de la lista
        /// </summary>
        partial void OnEquipoSeleccionadoChanged(EquipoComboItem? value)
        {
            _logger.LogDebug("[CrearPlanCronograma] OnEquipoSeleccionadoChanged -> nuevo={nuevo} (null? {esNull})", value?.Codigo ?? "(null)", value == null);
            if (value != null)
            {
                CodigoEquipo = value.Codigo;
                SelectedEquipoCodigo = value.Codigo;
                // Deferimos la asignación del texto para evitar que el ciclo interno de actualización del ComboBox lo sobrescriba.
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _suppressFiltroEquipoChanged = true;
                    FiltroEquipo = value.Codigo; // Mostrar solo el código
                    _suppressFiltroEquipoChanged = false;
                    _logger.LogDebug("[CrearPlanCronograma] FiltroEquipo forzado tras selección (Dispatcher) = {filtro}", FiltroEquipo);
                }), System.Windows.Threading.DispatcherPriority.Background);
                _logger.LogDebug("[CrearPlanCronograma] Equipo seleccionado confirmado: {codigo} - {nombre}", value.Codigo, value.NombreEquipo);
            }
        }

        /// <summary>
        /// Se ejecuta cuando el usuario escribe en el ComboBox para filtrar equipos
        /// </summary>
        partial void OnFiltroEquipoChanged(string value)
        {
            _logger.LogDebug("[CrearPlanCronograma] OnFiltroEquipoChanged -> value='{value}', suppress={suppress}, seleccionado={sel}", value, _suppressFiltroEquipoChanged, EquipoSeleccionado?.Codigo ?? "(null)");
            if (_suppressFiltroEquipoChanged) return;

            var texto = value ?? string.Empty;

            if (EquipoSeleccionado == null)
            {
                CodigoEquipo = texto;
                SelectedEquipoCodigo = texto;
                SincronizarSeleccionPorCodigo(texto);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(texto))
                {
                    _logger.LogDebug("[CrearPlanCronograma] Texto limpiado manualmente; reset selección.");
                    EquipoSeleccionado = null;
                    CodigoEquipo = string.Empty;
                    SelectedEquipoCodigo = null;
                }
                else if (!texto.Equals(EquipoSeleccionado.Codigo, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("[CrearPlanCronograma] Texto ya no coincide con código seleccionado actual ({sel}); se mantiene selección temporalmente.", EquipoSeleccionado.Codigo);
                }
            }

            FiltrarEquipos(texto);
        }

        private void SincronizarSeleccionPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;
            var existente = EquiposDisponibles.FirstOrDefault(e => string.Equals(e.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
            if (existente != null && !object.ReferenceEquals(existente, EquipoSeleccionado))
            {
                EquipoSeleccionado = existente;
            }
        }

        /// <summary>
        /// Filtra la lista de equipos basado en el texto de búsqueda
        /// </summary>
        private void FiltrarEquipos(string? filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro))
            {
                // Si no hay filtro, mostrar todos los equipos
                EquiposFiltrados.Clear();
                foreach (var equipo in EquiposDisponibles)
                {
                    EquiposFiltrados.Add(equipo);
                }
                return;
            }

            // Filtrar equipos que coincidan con el texto
            var equiposFiltrados = EquiposDisponibles.Where(e =>
                (!string.IsNullOrEmpty(e.Codigo) && e.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(e.NombreEquipo) && e.NombreEquipo.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(e.UsuarioAsignado) && e.UsuarioAsignado.Contains(filtro, StringComparison.OrdinalIgnoreCase))
            ).Take(50).ToList(); // Limitar a 50 resultados para rendimiento

            EquiposFiltrados.Clear();
            foreach (var equipo in equiposFiltrados)
            {
                EquiposFiltrados.Add(equipo);
            }
        }

        /// <summary>
        /// Método para establecer un equipo inicial si se proporciona un código
        /// </summary>
        public void EstablecerEquipoInicial(string? codigoEquipo)
        {
            if (string.IsNullOrWhiteSpace(codigoEquipo)) return;
            
            var equipo = EquiposDisponibles.FirstOrDefault(e => 
                string.Equals(e.Codigo, codigoEquipo, StringComparison.OrdinalIgnoreCase));
            
            if (equipo != null)
            {
                EquipoSeleccionado = equipo;
                FiltroEquipo = equipo.Codigo;
            }
            else
            {
                // Si no se encuentra en la lista actual, establecer como texto de filtro
                FiltroEquipo = codigoEquipo;
                CodigoEquipo = codigoEquipo;
            }
        }

        [RelayCommand]
        private async Task CrearPlanAsync()
        {
            if (IsSaving) return;

            try
            {
                IsSaving = true;
                StatusMessage = "Creando plan...";

                // Validación: usar el código del equipo seleccionado o el texto escrito
                var codigoParaUsar = !string.IsNullOrWhiteSpace(CodigoEquipo) ? CodigoEquipo : FiltroEquipo?.Trim();
                
                if (string.IsNullOrWhiteSpace(codigoParaUsar))
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
                }

                // NUEVO: Comprobar si ya existe al menos un plan para este equipo y evitar duplicados
                try
                {
                    var planesExistentes = await _planCronogramaService.GetByCodigoEquipoAsync(codigoParaUsar!);
                    if (planesExistentes != null && planesExistentes.Any())
                    {
                        StatusMessage = "Ya existe un plan para este equipo. Edite el plan existente o desactívelo antes de crear uno nuevo.";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[CrearPlanCronograma] Error al verificar planes existentes para {Codigo}", codigoParaUsar);
                    // En caso de error al verificar, continuar con precaución o abortar según política. Aquí abortamos para evitar duplicados silenciosos.
                    StatusMessage = "No fue posible verificar planes existentes. Intente nuevamente más tarde.";
                    return;
                }

                // Crear el plan
                var nuevoPlan = new PlanCronogramaEquipo
                {
                    PlanId = Guid.NewGuid(),
                    CodigoEquipo = codigoParaUsar,
                    Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? string.Empty : Descripcion.Trim(),
                    Responsable = string.IsNullOrWhiteSpace(Responsable) ? string.Empty : Responsable.Trim(),
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


