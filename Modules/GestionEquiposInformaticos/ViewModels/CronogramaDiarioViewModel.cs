using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Interfaces; // Reutilizamos servicios existentes de cronograma
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Interfaces;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels
{
    /// <summary>
    /// ViewModel para el cronograma diario (vista semanal detallada L-V) correspondiente al módulo GestionEquiposInformaticos.
    /// Respeta SRP: solo coordina carga y organización semanal diaria de mantenimientos planificados.
    /// </summary>
    public partial class CronogramaDiarioViewModel : ObservableObject
    {        private readonly ICronogramaService _cronogramaService;
        private readonly IPlanCronogramaService _planCronogramaService;
        private readonly IGestLogLogger _logger;
        private readonly IEquipoInformaticoService _equipoInformaticoService;
        private readonly IUsuarioService _usuarioService;public CronogramaDiarioViewModel(ICronogramaService cronogramaService, IPlanCronogramaService planCronogramaService, IGestLogLogger logger, IEquipoInformaticoService equipoInformaticoService, IUsuarioService usuarioService)
        {
            _cronogramaService = cronogramaService;
            _planCronogramaService = planCronogramaService;
            _logger = logger;
            _equipoInformaticoService = equipoInformaticoService;
            _usuarioService = usuarioService;
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
                for (int i = 1; i <= 53; i++) Weeks.Add(i);
            if (Years.Count == 0)
            {
                var currentYear = DateTime.Now.Year;
                for (int y = currentYear - 1; y <= currentYear + 1; y++)
                    Years.Add(y);
            }
            if (SelectedWeek == 0)
            {
                var hoy = System.DateTime.Now;
                // Usar ISOWeek para obtener la semana actual de forma consistente
                SelectedWeek = System.Globalization.ISOWeek.GetWeekOfYear(hoy);
            }
            await RefreshAsync(ct);
        }

        private async Task RefreshAsync(CancellationToken ct)
        {
            if (SelectedWeek <= 0 || SelectedWeek > 53 || IsLoading) return;
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
                }                // Cargar planes de cronograma de equipos CON navegación de equipo
                var planesEquipos = await _planCronogramaService.GetAllAsync();
                var planesActivos = planesEquipos.Where(p => p.Activo).ToList();                // Caches locales para evitar múltiples requests
                var equipoNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var usuarioNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                
                // Mostrar planes sólo a partir de su semana efectiva (semana ISO de FechaCreacion)
                foreach (var plan in planesActivos)
                {
                    // DiaProgramado: 1=Lunes, 2=Martes, ..., 7=Domingo
                    // Solo mostramos L-V (1-5)
                    if (plan.DiaProgramado < 1 || plan.DiaProgramado > 5) continue;

                    // Calcular semana ISO y año de la FechaCreacion del plan (más robusto: ISOWeek)
                    int semanaCreacion = System.Globalization.ISOWeek.GetWeekOfYear(plan.FechaCreacion);
                    int anioCreacion = System.Globalization.ISOWeek.GetYear(plan.FechaCreacion);

                    int semanaEfectiva = semanaCreacion;
                    int anioEfectivo = anioCreacion;

                    // Determinar si el plan debe mostrarse en la semana actual seleccionada
                    bool mostrar = false;
                    if (SelectedYear > anioEfectivo) mostrar = true;
                    else if (SelectedYear == anioEfectivo && SelectedWeek >= semanaEfectiva) mostrar = true;

                    if (!mostrar) continue;

                    // Normalizar código de equipo para keys
                    var codigoEquipo = plan.CodigoEquipo;
                    var codigoKey = string.IsNullOrWhiteSpace(codigoEquipo) ? string.Empty : codigoEquipo!;                    // Resolver nombre del equipo (navegación si existe, o servicio si no)
                    string? equipoNombre = plan.Equipo?.NombreEquipo;
                    string? usuarioAsignado = plan.Equipo?.UsuarioAsignado;// Si no tenemos los datos del equipo cargados, intentar obtenerlos del contexto
                    if ((string.IsNullOrWhiteSpace(equipoNombre) || string.IsNullOrWhiteSpace(usuarioAsignado)) && !string.IsNullOrWhiteSpace(codigoKey))
                    {
                        if (!equipoNameCache.TryGetValue(codigoKey, out var cachedEquipoName))
                        {
                            try
                            {
                                // Obtener el equipo informático directamente
                                var equipoInfo = await _equipoInformaticoService.GetByCodigoAsync(codigoKey);
                                
                                if (equipoInfo != null)
                                {
                                    // Priorizar datos del equipo sobre datos del plan
                                    if (string.IsNullOrWhiteSpace(equipoNombre))
                                        equipoNombre = equipoInfo.NombreEquipo;
                                    if (string.IsNullOrWhiteSpace(usuarioAsignado))
                                        usuarioAsignado = equipoInfo.UsuarioAsignado;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "[CronogramaDiarioViewModel] Error al obtener equipo {codigo}", codigoKey);
                            }

                            // Guardar en cache solo si tenemos una clave válida
                            if (!string.IsNullOrWhiteSpace(codigoKey))
                            {
                                equipoNameCache[codigoKey] = equipoNombre ?? string.Empty;
                            }
                        }
                        else
                        {
                            equipoNombre = cachedEquipoName;
                        }
                    }

                    // Usar el nombre del equipo solo si está disponible, sino el código
                    var nombreFinal = !string.IsNullOrWhiteSpace(equipoNombre) ? equipoNombre : codigoKey;
                      // Usar el usuario asignado solo si está disponible, sino vacío
                    var usuarioFinal = usuarioAsignado ?? string.Empty;

                    // Resolver usuario asignado: preferir el usuario asignado en el equipo; si no existe, usar el Responsable del plan
                    string usuarioAsignadoKey = usuarioFinal?.Trim() ?? string.Empty;

                    string usuarioResolved = string.Empty;
                    if (!string.IsNullOrWhiteSpace(usuarioAsignadoKey))
                    {
                        if (usuarioNameCache.TryGetValue(usuarioAsignadoKey, out var cachedUser))
                        {
                            usuarioResolved = cachedUser;
                        }
                        else
                        {
                            try
                            {
                                if (Guid.TryParse(usuarioAsignadoKey, out var uid))
                                {
                                    var usuario = await _usuarioService.ObtenerUsuarioPorIdAsync(uid);
                                    usuarioResolved = usuario?.NombrePersona ?? usuario?.NombreUsuario ?? usuarioAsignadoKey;
                                }
                                else
                                {
                                    var encontrados = await _usuarioService.BuscarUsuariosAsync(usuarioAsignadoKey);
                                    var first = encontrados?.FirstOrDefault();
                                    usuarioResolved = first?.NombrePersona ?? first?.NombreUsuario ?? usuarioAsignadoKey;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "[CronogramaDiarioViewModel] Error al resolver usuario {res}", usuarioAsignadoKey);
                                usuarioResolved = usuarioAsignadoKey;
                            }

                            usuarioNameCache[usuarioAsignadoKey] = usuarioResolved ?? usuarioAsignadoKey;
                        }
                    }

                    // Crear un DTO temporal para mostrar en la vista
                    var planDto = new CronogramaMantenimientoDto
                    {
                        Codigo = plan.CodigoEquipo,
                        // Nombre = Equipo amigable
                        Nombre = nombreFinal,
                        Marca = "Plan Semanal",
                        // Usuario asignado
                        Sede = usuarioResolved,
                        Anio = SelectedYear
                    };

                    int dayIndex = plan.DiaProgramado - 1; // Convertir a 0-based index
                    if (dayIndex >= 0 && dayIndex < Days.Count)
                        Days[dayIndex].Items.Add(planDto);
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
                // Mostrar nombre de equipo y usuario asignado en lugar del código
                var nombreEquipo = !string.IsNullOrWhiteSpace(mantenimiento.Nombre) ? mantenimiento.Nombre : mantenimiento.Codigo;
                var usuario = !string.IsNullOrWhiteSpace(mantenimiento.Sede) ? mantenimiento.Sede : "(sin asignar)";
                StatusMessage = $"Ejecutar plan semanal para {nombreEquipo} - Usuario: {usuario}";
                // Aquí podríamos abrir un diálogo específico para ejecutar planes semanales
            }
            else
            {
                var nombreEquipo = !string.IsNullOrWhiteSpace(mantenimiento.Nombre) ? mantenimiento.Nombre : mantenimiento.Codigo;
                StatusMessage = $"Registrar mantenimiento para {nombreEquipo}";
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
