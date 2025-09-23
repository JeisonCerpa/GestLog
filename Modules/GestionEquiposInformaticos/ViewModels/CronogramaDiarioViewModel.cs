using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Interfaces; // Reutilizamos servicios existentes de cronograma
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Services.Core.Logging;
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base auto-refresh
using GestLog.Services.Interfaces;       // ✅ NUEVO: IDatabaseConnectionService
using Modules.Usuarios.Interfaces;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using GestLog.Modules.Usuarios.Interfaces; // añadido para ICurrentUserService
using GestLog.Utilities; // NUEVO helper semanas centralizado
using System.Text.Json; // para parse checklist

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels
{    /// <summary>
    /// ViewModel para el cronograma diario (vista semanal detallada L-V) correspondiente al módulo GestionEquiposInformaticos.
    /// Respeta SRP: solo coordina carga y organización semanal diaria de mantenimientos planificados.
    /// </summary>
    public partial class CronogramaDiarioViewModel : DatabaseAwareViewModel    {        
        private readonly ICronogramaService _cronogramaService;
        private readonly IPlanCronogramaService _planCronogramaService;
        private readonly IEquipoInformaticoService _equipoInformaticoService;
        private readonly IUsuarioService _usuarioService;
        private readonly ISeguimientoService _seguimientoService; // NUEVO: para registrar ejecuciones
        private readonly ICurrentUserService _currentUserService; // NUEVO: usuario actual
        private readonly IRegistroMantenimientoEquipoDialogService? _registroDialogService; // nuevo servicio desacoplado
        private readonly IRegistroEjecucionPlanDialogService? _registroEjecucionPlanDialogService; // nuevo servicio ejecucion plan
        private readonly Dictionary<CronogramaMantenimientoDto, PlanCronogramaEquipo> _planMap = new(); // mapa plan

        public CronogramaDiarioViewModel(
            ICronogramaService cronogramaService, 
            IPlanCronogramaService planCronogramaService, 
            IEquipoInformaticoService equipoInformaticoService, 
            IUsuarioService usuarioService, 
            ISeguimientoService seguimientoService, 
            ICurrentUserService currentUserService,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger,
            IRegistroMantenimientoEquipoDialogService? registroDialogService = null, 
            IRegistroEjecucionPlanDialogService? registroEjecucionPlanDialogService = null)
            : base(databaseService, logger)
        {
            _cronogramaService = cronogramaService;
            _planCronogramaService = planCronogramaService;
            _equipoInformaticoService = equipoInformaticoService;
            _usuarioService = usuarioService;
            _seguimientoService = seguimientoService; // asignar servicio
            _currentUserService = currentUserService; // asignar usuario actual
            _registroDialogService = registroDialogService; // puede ser null (fallback al diálogo antiguo si existiera)
            _registroEjecucionPlanDialogService = registroEjecucionPlanDialogService;
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
        [ObservableProperty] private bool mostrarDetallePlan; // panel detalle visible
        [ObservableProperty] private CronogramaMantenimientoDto? selectedPlanDetalle; // dto seleccionado
        [ObservableProperty] private ObservableCollection<PlanDetalleChecklistItem> detalleChecklist = new();
        [ObservableProperty] private string? detalleEstadoTexto;
        [ObservableProperty] private DateTime? detalleFechaObjetivo;
        [ObservableProperty] private DateTime? detalleFechaEjecucion;        [ObservableProperty] private string? detalleResumen;

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
                _planMap.Clear();
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
                        Nombre = nombreFinal,
                        Marca = "Plan Semanal",
                        Sede = usuarioResolved,
                        Anio = SelectedYear,
                        EsPlanSemanal = true, // marcar
                        PlanEjecutadoSemana = plan.Ejecuciones?.Any(e => e.AnioISO == SelectedYear && e.SemanaISO == SelectedWeek && e.Estado == 2) == true, // completado
                        // Es atrasado solo si la fecha objetivo ya pasó (fecha objetivo < hoy) y no ejecutado
                        EsAtrasadoSemana = DateTimeWeekHelper.IsPlanAtrasado(SelectedYear, SelectedWeek, plan.DiaProgramado, 
                                                plan.Ejecuciones?.Any(e => e.AnioISO == SelectedYear && e.SemanaISO == SelectedWeek && e.Estado == 2) == true)
                    };

                    int dayIndex = plan.DiaProgramado - 1; // Convertir a 0-based index
                    if (dayIndex >= 0 && dayIndex < Days.Count)
                    {
                        Days[dayIndex].Items.Add(planDto);
                        _planMap[planDto] = plan; // asociar
                    }                }

                // Contar planes totales y realizados en la semana seleccionada
                int totalPlanes = planesActivos.Count(p => p.DiaProgramado >= 1 && p.DiaProgramado <= 5);
                int planesRealizados = planesActivos.Count(p => 
                    p.DiaProgramado >= 1 && p.DiaProgramado <= 5 && 
                    p.Ejecuciones?.Any(e => e.AnioISO == SelectedYear && e.SemanaISO == SelectedWeek && e.Estado == 2) == true);
                
                StatusMessage = $"{totalPlanes} planes, {planesRealizados} realizados";
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
        private async Task OpenRegistrar(CronogramaMantenimientoDto? mantenimiento)
        {
            if (mantenimiento == null)
            {
                StatusMessage = "Elemento no válido";
                return;
            }

            // Si corresponde a un plan semanal, abrir nuevo flujo de ejecución
            if (_planMap.TryGetValue(mantenimiento, out var planEntity))
            {
                try
                {
                    var hoy = DateTime.Now;
                    int semanaActual = System.Globalization.ISOWeek.GetWeekOfYear(hoy);
                    int anioActual = System.Globalization.ISOWeek.GetYear(hoy);
                    if (SelectedYear > anioActual || (SelectedYear == anioActual && SelectedWeek > semanaActual))
                    {
                        StatusMessage = "No se puede registrar una ejecución en semana futura";
                        return;
                    }
                    if (_registroEjecucionPlanDialogService == null)
                    {
                        StatusMessage = "Servicio de ejecución semanal no disponible";
                        return;
                    }
                    var responsableActual = _currentUserService?.GetCurrentUserFullName() ?? Environment.UserName;
                    var ok = await _registroEjecucionPlanDialogService.TryShowAsync(planEntity.PlanId, SelectedYear, SelectedWeek, responsableActual);
                    if (ok)
                    {
                        StatusMessage = "Ejecución registrada";
                        await RefreshAsync(CancellationToken.None);
                    }
                    else
                    {
                        StatusMessage = "Registro cancelado";
                    }
                    return; // no continuar con flujo antiguo
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CronogramaDiarioViewModel] Error en ejecución semanal");
                    StatusMessage = "Error al registrar ejecución semanal";
                    return;
                }
            }

            try
            {
                // Validar que no se intente registrar en una semana futura
                var hoy = DateTime.Now;
                int semanaActual = System.Globalization.ISOWeek.GetWeekOfYear(hoy);
                int anioActual = System.Globalization.ISOWeek.GetYear(hoy);
                if (SelectedYear > anioActual || (SelectedYear == anioActual && SelectedWeek > semanaActual))
                {
                    StatusMessage = "No se puede registrar en una semana futura";
                    return;
                }

                // Resolver responsable: prioridad usuario actual autenticado
                string responsableActual = string.Empty;
                try
                {
                    if (_currentUserService?.IsAuthenticated == true)
                    {
                        responsableActual = _currentUserService.GetCurrentUserFullName();
                        if (string.IsNullOrWhiteSpace(responsableActual))
                        {
                            var userId = _currentUserService.GetCurrentUserId();
                            responsableActual = userId.HasValue ? userId.ToString()! : Environment.UserName;
                        }
                    }
                }
                catch { responsableActual = Environment.UserName; }

                if (string.IsNullOrWhiteSpace(responsableActual))
                    responsableActual = Environment.UserName;

                // Preparar DTO de seguimiento
                var seguimiento = new SeguimientoMantenimientoDto
                {
                    Codigo = mantenimiento.Codigo ?? string.Empty,
                    Nombre = !string.IsNullOrWhiteSpace(mantenimiento.Nombre) ? mantenimiento.Nombre : mantenimiento.Codigo,
                    TipoMtno = TipoMantenimiento.Preventivo, // Default, se puede ajustar en el diálogo
                    Descripcion = string.Empty,
                    Responsable = responsableActual, // actualizado
                    FechaRegistro = DateTime.Now,
                    FechaRealizacion = DateTime.Now,
                    Semana = SelectedWeek,
                    Anio = SelectedYear,
                    Estado = EstadoSeguimientoMantenimiento.Pendiente,
                    Observaciones = mantenimiento.Marca == "Plan Semanal" ? $"Generado desde plan semanal (Semana {SelectedWeek})" : string.Empty
                };

                // Abrir diálogo propio desacoplado (si está registrado) en lugar del SeguimientoDialog
                SeguimientoMantenimientoDto? result = null;
                bool confirmado = false;
                if (_registroDialogService != null)
                {
                    confirmado = _registroDialogService.TryShowRegistroDialog(seguimiento, out result);
                }
                else
                {
                    // Fallback temporal: usar diálogo antiguo si el servicio no está disponible
                    var dialogFallback = new GestLog.Views.Tools.GestionMantenimientos.SeguimientoDialog(seguimiento, modoRestringido: true);
                    var parentWindowFallback = System.Windows.Application.Current.Windows
                        .OfType<System.Windows.Window>()
                        .FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;
                    if (parentWindowFallback != null)
                        dialogFallback.Owner = parentWindowFallback;
                    if (dialogFallback.ShowDialog() == true)
                        result = dialogFallback.Seguimiento;
                    confirmado = result != null;
                }

                if (confirmado && result != null)
                {
                    // Validar duplicado antes de persistir (codigo + semana + año)
                    var existentes = await _seguimientoService.GetAllAsync();
                    if (existentes.Any(s => s.Codigo == result.Codigo && s.Semana == result.Semana && s.Anio == result.Anio))
                    {
                        StatusMessage = "Ya existe un registro para este código en la semana seleccionada";
                        return;
                    }

                    await _seguimientoService.AddAsync(result);
                    WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
                    StatusMessage = "Mantenimiento registrado";
                }
                else
                {
                    StatusMessage = "Registro cancelado";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaDiarioViewModel] Error al registrar mantenimiento");
                StatusMessage = "Error al registrar mantenimiento";
            }
        }

        // Utilidad local para calcular primer día de semana ISO (duplicado ligero para evitar dependencia circular)
        private static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var weekNum = weekOfYear;
            if (firstWeek <= 1)
                weekNum -= 1;
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
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
        }        [RelayCommand]
        private void VerDetallePlan(CronogramaMantenimientoDto? dto)
        {
            if (dto == null) return;
            if (!_planMap.TryGetValue(dto, out var planEntity)) return; // solo planes
            
            // Actualizar propiedades de detalle
            SelectedPlanDetalle = dto;
            DetalleChecklist.Clear();
            
            // Estado y fechas
            var fechaObjetivo = DateTimeWeekHelper.GetFechaObjetivoSemana(SelectedYear, SelectedWeek, planEntity.DiaProgramado);
            DetalleFechaObjetivo = fechaObjetivo;
            var ejecucion = planEntity.Ejecuciones?.FirstOrDefault(e => e.AnioISO == SelectedYear && e.SemanaISO == SelectedWeek);
            DetalleFechaEjecucion = ejecucion?.FechaEjecucion;
            bool ejecutado = ejecucion?.Estado == 2;
            bool atrasado = !ejecutado && fechaObjetivo.Date < DateTime.Today;
            DetalleEstadoTexto = ejecutado ? "Ejecutado" : atrasado ? "Atrasado" : "Pendiente";
            
            // Parse checklist si existe
            if (!string.IsNullOrWhiteSpace(ejecucion?.ResultadoJson))
            {
                try
                {
                    var doc = JsonDocument.Parse(ejecucion.ResultadoJson);
                    if (doc.RootElement.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        int total = 0, ok = 0, obs = 0, pend = 0;
                        foreach (var it in arr.EnumerateArray())
                        {
                            bool comp = it.TryGetProperty("Completado", out var cEl) && cEl.ValueKind == JsonValueKind.True;
                            string desc = it.TryGetProperty("Descripcion", out var dEl) ? (dEl.GetString() ?? string.Empty) :
                                           it.TryGetProperty("descripcion", out var d2El) ? (d2El.GetString() ?? string.Empty) : string.Empty;
                            string? ob = it.TryGetProperty("Observacion", out var oEl) ? oEl.GetString() :
                                          it.TryGetProperty("observacion", out var o2El) ? o2El.GetString() : null;
                            int? id = it.TryGetProperty("Id", out var idEl) ? idEl.GetInt32() :
                                       it.TryGetProperty("id", out var id2El) ? id2El.GetInt32() : (int?)null;
                            DetalleChecklist.Add(new PlanDetalleChecklistItem { Id = id, Descripcion = desc, Completado = comp, Observacion = ob });
                            total++; if (comp) ok++; else if (!string.IsNullOrWhiteSpace(ob)) obs++; else pend++;
                        }
                        DetalleResumen = $"{ok}/{total} ítems OK, Observados {obs}, Pendientes {pend}";
                    }
                }
                catch { /* ignorar parse errors */ }
            }
            else
            {
                DetalleResumen = ejecutado ? "Ejecutado sin checklist" : "Sin ejecución registrada";
            }            // Abrir ventana modal
            try
            {
                var modalWindow = new GestLog.Views.Tools.GestionEquipos.PlanDetalleModalWindow
                {
                    DataContext = this
                };

                // Obtener la ventana padre actual
                var parentWindow = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;

                // Configurar la ventana modal para que siga a la ventana padre
                modalWindow.ConfigurarParaVentanaPadre(parentWindow);

                modalWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaDiarioViewModel] Error al abrir ventana de detalle");
                // Fallback al overlay si falla la ventana modal
                MostrarDetallePlan = true;
            }
        }

        [RelayCommand]
        private void CerrarDetallePlan()
        {
            MostrarDetallePlan = false;
        }

        public class PlanDetalleChecklistItem : ObservableObject
        {
            public int? Id { get; set; }
            public string Descripcion { get; set; } = string.Empty;
            public bool Completado { get; set; }
            public string? Observacion { get; set; }            public string Estado => Completado ? "OK" : string.IsNullOrWhiteSpace(Observacion) ? "Pendiente" : "Observado";
        }        /// <summary>
        /// Implementación del método abstracto para auto-refresh automático
        /// </summary>
        protected override async Task RefreshDataAsync()
        {
            try
            {
                _logger.LogInformation("[CronogramaDiarioViewModel] Refrescando datos automáticamente");
                await LoadAsync(CancellationToken.None);
                _logger.LogInformation("[CronogramaDiarioViewModel] Datos refrescados exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaDiarioViewModel] Error al refrescar datos");
                throw;
            }
        }

        /// <summary>
        /// Override para manejar cuando se pierde la conexión específicamente para cronograma diario
        /// </summary>
        protected override void OnConnectionLost()
        {
            // El ViewModel no tiene StatusMessage, usar logging en su lugar
            _logger.LogWarning("[CronogramaDiarioViewModel] Sin conexión - Cronograma diario no disponible");
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
