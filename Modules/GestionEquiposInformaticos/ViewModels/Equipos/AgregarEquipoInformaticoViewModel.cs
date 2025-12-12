// ✅ MIGRADO A DatabaseAwareViewModel - AUTO-REFRESH CON TIMEOUT ULTRARRÁPIDO
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Messages;
using GestLog.Modules.DatabaseConnection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.Personas.Models;
using Modules.Personas.Interfaces;
using System.Globalization;
using System.Threading;
using GestLog.Services.Equipos;
using GestLog.Services;

// ✅ NUEVAS DEPENDENCIAS PARA AUTO-REFRESH
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base auto-refresh
using GestLog.Services.Interfaces;       // ✅ NUEVO: IDatabaseConnectionService
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages.Equipos;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos
{
    /// <summary>
    /// ViewModel para agregar/editar equipos informáticos.
    /// ✅ MIGRADO: Hereda de DatabaseAwareViewModel para auto-refresh automático con timeout ultrarrápido.
    /// </summary>
    public partial class AgregarEquipoInformaticoViewModel : DatabaseAwareViewModel
    {
        #region ✅ NUEVAS DEPENDENCIAS - AUTO-REFRESH
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        #endregion
        [ObservableProperty]
        private bool isLoadingRam;

        [ObservableProperty]
        private ObservableCollection<SlotRamEntity> listaRam = new();

        [ObservableProperty]
        private string codigo = string.Empty;
        [ObservableProperty]
        private string usuarioAsignado = string.Empty;
        [ObservableProperty]
        private string nombreEquipo = string.Empty;
        [ObservableProperty]
        private string sede = "Administrativa - Barranquilla";
        [ObservableProperty]
        private string marca = string.Empty;
        [ObservableProperty]
        private string modelo = string.Empty;
        [ObservableProperty]
        private string procesador = string.Empty;
        [ObservableProperty]
        private string so = string.Empty;
        [ObservableProperty]
        private string serialNumber = string.Empty;
        [ObservableProperty]
        private string observaciones = string.Empty;

        [ObservableProperty]
        private decimal? costo;
        [ObservableProperty]
        private DateTime? fechaCompra;
        // Minicampos para entrada manual: día / mes / año
        [ObservableProperty]
        private string fechaDia = string.Empty;
        [ObservableProperty]
        private string fechaMes = string.Empty;
        [ObservableProperty]
        private string fechaAno = string.Empty;

        // Cuando cambia FechaCompra (por binding o DatePicker), actualizar los minicampos
        partial void OnFechaCompraChanged(DateTime? value)
        {
            if (value.HasValue)
            {
                FechaDia = value.Value.Day.ToString("D2");
                FechaMes = value.Value.Month.ToString("D2");
                FechaAno = value.Value.Year.ToString();
            }
            else
            {
                FechaDia = string.Empty;
                FechaMes = string.Empty;
                FechaAno = string.Empty;
            }
        }

        // Cuando cualquiera de los minicampos cambia, intentar construir FechaCompra
        partial void OnFechaDiaChanged(string value) => UpdateFechaFromParts();
        partial void OnFechaMesChanged(string value) => UpdateFechaFromParts();
        partial void OnFechaAnoChanged(string value) => UpdateFechaFromParts();

        private void UpdateFechaFromParts()
        {
            // Si los tres están vacíos, limpiar FechaCompra
            if (string.IsNullOrWhiteSpace(FechaDia) && string.IsNullOrWhiteSpace(FechaMes) && string.IsNullOrWhiteSpace(FechaAno))
            {
                if (FechaCompra != null) FechaCompra = null;
                return;
            }

            // Intentar parsear valores numéricos
            if (int.TryParse(FechaDia, out var d) && int.TryParse(FechaMes, out var m) && int.TryParse(FechaAno, out var y))
            {
                // Soporte práctico: si el año es de 2 dígitos, asumimos 2000+ (ej. '23' => 2023)
                if (y >= 0 && y < 100)
                {
                    y += 2000;
                }

                try
                {
                    if (y >= 1 && m >= 1 && m <= 12)
                    {
                        var maxDay = DateTime.DaysInMonth(y, m);
                        if (d >= 1 && d <= maxDay)
                        {
                            var newDate = new DateTime(y, m, d);
                            if (FechaCompra != newDate)
                                FechaCompra = newDate;
                            return;
                        }
                    }
                }
                catch { /* Ignorar errores de validación */ }
            }

            // Si la combinación no es válida, no modificamos FechaCompra (el usuario puede corregir)
        }

        [ObservableProperty]
        private string codigoAnydesk = string.Empty;

        [ObservableProperty]
        private ObservableCollection<DiscoEntity> listaDiscos = new();

        // Modo edición: si true, Guardar actualizará un equipo existente en lugar de crear uno nuevo
        [ObservableProperty]
        private bool isEditing;

        // Código original del equipo (antes de editar) para identificar la entidad en BD
        [ObservableProperty]
        private string originalCodigo = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Persona> personasDisponibles = new();

        [ObservableProperty]
        private Persona? personaAsignada;

        [ObservableProperty]
        private string filtroPersonaAsignada = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Persona> personasFiltradas = new();

        [ObservableProperty]
        private string estado = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ConexionEntity> listaConexiones = new();

        // Gestión de periféricos asignables
        [ObservableProperty]
        private ObservableCollection<PerifericoEquipoInformaticoDto> perifericosDisponibles = new();

        [ObservableProperty]
        private ObservableCollection<PerifericoEquipoInformaticoDto> perifericosAsignados = new();

        public string[] TiposRam { get; } = new[] { "DDR3", "DDR4", "DDR5", "LPDDR4", "LPDDR5" };
        public string[] TiposDisco { get; } = new[] { "HDD", "SSD", "NVMe", "eMMC" };        [ObservableProperty]
        private bool isLoadingDiscos;        
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;
        // ✅ REMOVIDO: _logger ahora se hereda de DatabaseAwareViewModel

        [ObservableProperty]
        private bool canGuardarEquipo;
        [ObservableProperty]
        private bool canObtenerCamposAutomaticos;
        [ObservableProperty]
        private bool canObtenerDiscosAutomaticos;
        [ObservableProperty]
        private bool canAgregarDiscoManual;
        [ObservableProperty]
        private bool canAgregarRam;        
        [ObservableProperty]
        private bool canEliminarDisco;
        [ObservableProperty]
        private bool canEliminarRam;

        [ObservableProperty]
        private bool canAgregarConexionManual;
        [ObservableProperty]
        private bool canEliminarConexion;

        // Propiedades de permisos para gestión de periféricos
        // Eliminado: canAsignarPeriferico. Se reutiliza CanGuardarEquipo para controlar la acción de asignar periféricos.

        [ObservableProperty]
        private bool canDesasignarPeriferico;        private int _isSaving = 0; // 0 = no guardando, 1 = guardando
        
        /// <summary>
        /// ✅ MIGRADO: Constructor actualizado con dependencias para DatabaseAwareViewModel
        /// </summary>
        public AgregarEquipoInformaticoViewModel(
            ICurrentUserService currentUserService,
            IDbContextFactory<GestLogDbContext> dbContextFactory,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger) // ✅ MIGRADO: Llama al constructor base para auto-refresh
        {
            _currentUserService = currentUserService;
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
              RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
        }
        
        #region ✅ MÉTODOS MIGRADOS - DatabaseAwareViewModel
        /// <summary>
        /// ✅ MIGRADO: Implementación requerida para DatabaseAwareViewModel
        /// Recarga las personas disponibles y otros datos necesarios
        /// </summary>
        protected override async Task RefreshDataAsync()
        {
            try
            {
                _logger.LogDebug("[AgregarEquipoInformaticoViewModel] Refrescando datos automáticamente");
                
                await using var context = await _dbContextFactory.CreateDbContextAsync();
                
                // Cargar personas activas
                var personas = await context.Personas
                    .Where(p => p.Activo)
                    .ToListAsync();

                // Actualizar colecciones en el hilo de UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Actualizar PersonasDisponibles preservando referencias enlazadas
                    if (PersonasDisponibles == null)
                        PersonasDisponibles = new ObservableCollection<Persona>(personas);
                    else
                    {
                        PersonasDisponibles.Clear();
                        foreach (var p in personas) PersonasDisponibles.Add(p);
                    }

                    if (PersonasFiltradas == null)
                        PersonasFiltradas = new ObservableCollection<Persona>(personas);
                    else
                    {
                        PersonasFiltradas.Clear();
                        foreach (var p in personas) PersonasFiltradas.Add(p);
                    }
                });

                _logger.LogDebug("[AgregarEquipoInformaticoViewModel] Datos refrescados exitosamente");
            }
            catch (Exception ex)
            {
                // ✅ PATRÓN: Manejo silencioso de errores de conexión (no bloquea UI)
                _logger.LogWarning(ex, "[AgregarEquipoInformaticoViewModel] Error actualizando datos - continuando con datos locales");
            }
        }
        #endregion

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }        
        public void RecalcularPermisos()
        {
            CanGuardarEquipo = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanObtenerCamposAutomaticos = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanObtenerDiscosAutomaticos = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanAgregarDiscoManual = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanAgregarRam = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanEliminarDisco = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanEliminarRam = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanAgregarConexionManual = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanEliminarConexion = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");

            // Permisos para gestión de periféricos
            // Usar CanGuardarEquipo para controlar si se puede asignar un periférico (mismo permiso que crear equipo)
            CanDesasignarPeriferico = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanEliminarRam = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
        }        public async Task InicializarAsync()
        {
            try
            {
                // ✅ MIGRADO: Usar DbContextFactory en lugar de conexión manual
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

                // Ejecutar la consulta en background para no bloquear la UI
                var personas = await Task.Run(async () =>
                {
                    return await dbContext.Personas.Where(p => p.Activo).ToListAsync();
                }).ConfigureAwait(false);

                // Asignar/actualizar las colecciones en el hilo de UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Si la colección no existe, crearla; si existe, actualizarla para preservar referencias enlazadas
                    if (PersonasDisponibles == null)
                        PersonasDisponibles = new ObservableCollection<Persona>(personas);
                    else
                    {
                        PersonasDisponibles.Clear();
                        foreach (var p in personas) PersonasDisponibles.Add(p);
                    }

                    if (PersonasFiltradas == null)
                        PersonasFiltradas = new ObservableCollection<Persona>(personas);
                    else
                    {
                        PersonasFiltradas.Clear();
                        foreach (var p in personas) PersonasFiltradas.Add(p);
                    }

                    // Intentar re-emparejar la persona asignada si ya había una selección previa o texto en el filtro
                    string? objetivoTexto = null;
                    if (PersonaAsignada != null && !string.IsNullOrWhiteSpace(PersonaAsignada.NombreCompleto))
                        objetivoTexto = PersonaAsignada.NombreCompleto;
                    else if (!string.IsNullOrWhiteSpace(FiltroPersonaAsignada))
                        objetivoTexto = FiltroPersonaAsignada;

                    if (!string.IsNullOrWhiteSpace(objetivoTexto) && PersonasDisponibles != null && PersonasDisponibles.Any())
                    {
                        static string NormalizeStringLocal(string s)
                        {
                            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                            var normalized = s.Normalize(System.Text.NormalizationForm.FormD);
                            var chars = normalized.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
                            return new string(chars).Normalize(System.Text.NormalizationForm.FormC).Trim().ToLowerInvariant();
                        }

                        var objetivo = NormalizeStringLocal(objetivoTexto);
                        var encontrada = PersonasDisponibles.FirstOrDefault(p => NormalizeStringLocal(p.NombreCompleto) == objetivo);
                        if (encontrada == null)
                            encontrada = PersonasDisponibles.FirstOrDefault(p => NormalizeStringLocal(p.NombreCompleto).Contains(objetivo) || objetivo.Contains(NormalizeStringLocal(p.NombreCompleto)));

                        if (encontrada != null)
                        {
                            PersonaAsignada = encontrada;
                            FiltroPersonaAsignada = string.Empty;
                        }
                        else
                        {
                            // Mantener el filtro para que el usuario vea el nombre en caso de no encontrar coincidencia exacta
                            FiltroPersonaAsignada = objetivoTexto.Trim();
                        }
                    }                });

                // Cargar periféricos disponibles siempre al inicializar el diálogo.
                // Si Codigo está vacío, la consulta interna devolverá solo periféricos no asignados.
                await CargarPerifericosDisponiblesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar las personas para usuario asignado");
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("No se pudieron cargar los usuarios disponibles.", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }        }
        
        // ✅ REMOVIDO: GetProductionConnectionString() - ya no necesario con DbContextFactory

        [RelayCommand(CanExecute = nameof(CanGuardarEquipo))]
        public async Task GuardarEquipoAsync()
        {
            // Evitar reentrada concurrente (por doble disparo del comando/handler)
            if (Interlocked.Exchange(ref _isSaving, 1) == 1)
            {
                _logger.LogWarning("Intento de guardar ignorado: ya hay una operación de guardado en curso.");
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(Codigo) || string.IsNullOrWhiteSpace(NombreEquipo))
                {                    MessageBox.Show("El código y el nombre del equipo son obligatorios.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✅ MIGRADO: Usar DbContextFactory en lugar de conexión manual
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

                // Si estamos en modo edición, actualizar
                if (IsEditing)
                {
                    // Si se cambió el código, comprobar que no exista otro equipo con ese código
                    if (!string.Equals(OriginalCodigo, Codigo, StringComparison.OrdinalIgnoreCase) && dbContext.EquiposInformaticos.Any(e => e.Codigo == Codigo))
                    {
                        MessageBox.Show("Ya existe un equipo con ese código.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }                    
                    // Cargar la entidad desde el mismo DbContext (incluyendo colecciones)
                    var equipo = dbContext.EquiposInformaticos
                        .Include(e => e.SlotsRam)
                        .Include(e => e.Discos)
                        .Include(e => e.Conexiones)
                        .FirstOrDefault(e => e.Codigo == OriginalCodigo);

                    if (equipo == null)
                    {
                        MessageBox.Show("No se encontró el equipo a actualizar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Si cambió el código primario, realizar flujo seguro: actualizar PK y FKs via SQL dentro de transacción
                    if (!string.Equals(OriginalCodigo, Codigo, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Cambio de PK detectado: {Original} -> {Nuevo}. Usando flujo de actualización segura.", OriginalCodigo, Codigo);                        
                        using var transaction = await dbContext.Database.BeginTransactionAsync();
                        try
                        {
                            _logger.LogInformation("💾 Iniciando actualización de PK con deshabilitar constraints: {Original} -> {Nuevo}", OriginalCodigo, Codigo);                            
                            // SOLUCIÓN ROBUSTA: Deshabilitar temporalmente las constraints FK para permitir la actualización de PK
                            _logger.LogInformation("🔓 Deshabilitando constraints FK temporalmente...");
                            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE SlotsRam NOCHECK CONSTRAINT FK_SlotsRam_EquipoInformatico");
                            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Discos NOCHECK CONSTRAINT FK_Discos_EquipoInformatico");
                            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE ConexionesEquiposInformaticos NOCHECK CONSTRAINT FK_ConexionesEquiposInformaticos_EquipoInformatico");

                            // 1. Actualizar la Primary Key en EquiposInformaticos
                            var rowsEquipo = await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE EquiposInformaticos SET Codigo = {Codigo} WHERE Codigo = {OriginalCodigo}");
                            _logger.LogInformation("📋 Equipo actualizado: {RowsAffected} filas", rowsEquipo);                            
                            // 2. Actualizar las Foreign Keys en tablas relacionadas
                            var rowsSlots = await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE SlotsRam SET CodigoEquipo = {Codigo} WHERE CodigoEquipo = {OriginalCodigo}");
                            _logger.LogInformation("💾 SlotsRam actualizados: {RowsAffected} filas", rowsSlots);

                            var rowsDiscos = await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE Discos SET CodigoEquipo = {Codigo} WHERE CodigoEquipo = {OriginalCodigo}");
                            _logger.LogInformation("💿 Discos actualizados: {RowsAffected} filas", rowsDiscos);

                            var rowsConexiones = await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE ConexionesEquiposInformaticos SET CodigoEquipo = {Codigo} WHERE CodigoEquipo = {OriginalCodigo}");
                            _logger.LogInformation("🌐 Conexiones actualizadas: {RowsAffected} filas", rowsConexiones);

                            // 3. Rehabilitar las constraints FK
                            _logger.LogInformation("🔒 Rehabilitando constraints FK...");
                            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE SlotsRam WITH CHECK CHECK CONSTRAINT FK_SlotsRam_EquipoInformatico");
                            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Discos WITH CHECK CHECK CONSTRAINT FK_Discos_EquipoInformatico");
                            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE ConexionesEquiposInformaticos WITH CHECK CHECK CONSTRAINT FK_ConexionesEquiposInformaticos_EquipoInformatico");

                            // Invalidar cualquier entidad rastreada para evitar conflictos de ChangeTracker
                            try
                            {
                                var tracked = dbContext.ChangeTracker.Entries<EquipoInformaticoEntity>().ToList();
                                foreach (var t in tracked)
                                {
                                    dbContext.Entry(t.Entity).State = EntityState.Detached;
                                }
                                var trackedSlots = dbContext.ChangeTracker.Entries<SlotRamEntity>().ToList();
                                foreach (var t in trackedSlots)
                                {
                                    dbContext.Entry(t.Entity).State = EntityState.Detached;
                                }
                                var trackedDiscos = dbContext.ChangeTracker.Entries<DiscoEntity>().ToList();
                                foreach (var t in trackedDiscos)
                                {
                                    dbContext.Entry(t.Entity).State = EntityState.Detached;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "No se pudo limpiar ChangeTracker antes de recargar (no crítico)");
                            }                            
                            // Volver a cargar la entidad ya con el nuevo código
                            var equipoRecargado = dbContext.EquiposInformaticos
                                .Include(e => e.SlotsRam)
                                .Include(e => e.Discos)
                                .Include(e => e.Conexiones)
                                .FirstOrDefault(e => e.Codigo == Codigo);

                            if (equipoRecargado == null)
                            {
                                await transaction.RollbackAsync();
                                MessageBox.Show("No se pudo localizar el equipo después de renombrar el código.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // Actualizar campos escalares en la entidad recargada
                            equipoRecargado.UsuarioAsignado = PersonaAsignada?.NombreCompleto ?? string.Empty;
                            equipoRecargado.NombreEquipo = NombreEquipo;
                            equipoRecargado.Sede = Sede;
                            equipoRecargado.Marca = Marca;
                            equipoRecargado.Modelo = Modelo;
                            equipoRecargado.Procesador = Procesador;
                            equipoRecargado.SO = So;
                            equipoRecargado.SerialNumber = SerialNumber;
                            equipoRecargado.Observaciones = Observaciones;
                            // Asegurar persistencia de estos campos al actualizar un equipo existente
                            equipoRecargado.Costo = Costo;
                            equipoRecargado.FechaCompra = FechaCompra;
                            equipoRecargado.CodigoAnydesk = CodigoAnydesk;
                            equipoRecargado.FechaModificacion = DateTime.Now;
                            {
                                // Antes: equipoRecargado.Estado = Estado;
                                string _msg; bool _persisted;
                                EquipoEstadoService.SetEstado(equipoRecargado, Estado, dbContext, out _msg, out _persisted);
                            }

                            // Si se marcó como Activo, limpiar la FechaBaja (volver a activo elimina la fecha de baja)
                            if (string.Equals(Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                            {
                                equipoRecargado.FechaBaja = null;
                            }

                            // Reemplazar colecciones: eliminar existentes y añadir nuevas instancias
                            if (equipoRecargado.SlotsRam != null && equipoRecargado.SlotsRam.Any())
                                dbContext.SlotsRam.RemoveRange(equipoRecargado.SlotsRam);
                            if (equipoRecargado.Discos != null && equipoRecargado.Discos.Any())
                                dbContext.Discos.RemoveRange(equipoRecargado.Discos);

                            int slotNumNew = 1;
                            foreach (var slot in ListaRam)
                            {
                                var nuevoSlot = new GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity
                                {
                                    CodigoEquipo = Codigo,
                                    NumeroSlot = slotNumNew++,
                                    CapacidadGB = slot.CapacidadGB,
                                    TipoMemoria = slot.TipoMemoria,
                                    Marca = slot.Marca,
                                    Frecuencia = slot.Frecuencia,
                                    Ocupado = slot.Ocupado,
                                    Observaciones = slot.Observaciones
                                };
                                dbContext.SlotsRam.Add(nuevoSlot);
                            }

                            int discoNumNew = 1;
                            foreach (var disco in ListaDiscos)
                            {
                                var nuevoDisco = new GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity
                                {
                                    CodigoEquipo = Codigo,
                                    NumeroDisco = discoNumNew++,
                                    Tipo = disco.Tipo,
                                    CapacidadGB = disco.CapacidadGB,
                                    Marca = disco.Marca,
                                    Modelo = disco.Modelo
                                };
                                dbContext.Discos.Add(nuevoDisco);
                            }

                            // Intentar guardar cambios con manejo específico de concurrencia y reintento único
                            bool saved = false;
                            int attempts = 0;
                            while (!saved && attempts < 2)
                            {
                                attempts++;
                                try
                                {
                                    await dbContext.SaveChangesAsync();
                                    saved = true;
                                }
                                catch (DbUpdateConcurrencyException dbce)
                                {
                                    _logger.LogWarning(dbce, "DbUpdateConcurrencyException al guardar equipo (intento {Attempt})", attempts);
                                    if (attempts >= 2)
                                    {
                                        MessageBox.Show("No se pudo guardar los cambios por un conflicto de concurrencia. Por favor, recargue y reintente.", "Conflicto", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        try { await transaction.RollbackAsync(); } catch { }
                                        return;
                                    }

                                    // En caso de concurrencia, recargar la entidad y reintentar
                                    try
                                    {
                                        // Limpiar ChangeTracker y recargar
                                        foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
                                            entry.State = EntityState.Detached;                                        
                                        equipoRecargado = dbContext.EquiposInformaticos
                                            .Include(e => e.SlotsRam)
                                            .Include(e => e.Discos)
                                            .Include(e => e.Conexiones)
                                            .FirstOrDefault(e => e.Codigo == Codigo);

                                        if (equipoRecargado == null)
                                        {
                                            MessageBox.Show("Equipo no encontrado al intentar resolver concurrencia.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                            try { await transaction.RollbackAsync(); } catch { }
                                            return;
                                        }

                                        // Re-apply desired scalar updates and collection replacements as above
                                        equipoRecargado.UsuarioAsignado = PersonaAsignada?.NombreCompleto ?? string.Empty;
                                        equipoRecargado.NombreEquipo = NombreEquipo;
                                        equipoRecargado.Sede = Sede;
                                        equipoRecargado.Marca = Marca;
                                        equipoRecargado.Modelo = Modelo;
                                        equipoRecargado.Procesador = Procesador;
                                        equipoRecargado.SO = So;
                                        equipoRecargado.SerialNumber = SerialNumber;
                                        equipoRecargado.Observaciones = Observaciones;
                                        // Asegurar persistencia de estos campos al actualizar un equipo existente
                                        equipoRecargado.Costo = Costo;
                                        equipoRecargado.FechaCompra = FechaCompra;
                                        equipoRecargado.CodigoAnydesk = CodigoAnydesk;
                                        equipoRecargado.FechaModificacion = DateTime.Now;
                                        {
                                            // Antes: equipoRecargado.Estado = Estado;
                                            string _msg; bool _persisted;
                                            EquipoEstadoService.SetEstado(equipoRecargado, Estado, dbContext, out _msg, out _persisted);
                                        }

                                        // Si se marcó como Activo, limpiar la FechaBaja (volver a activo elimina la fecha de baja)
                                        if (string.Equals(Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                                        {
                                            equipoRecargado.FechaBaja = null;
                                        }

                                        if (equipoRecargado.SlotsRam != null && equipoRecargado.SlotsRam.Any())
                                            dbContext.SlotsRam.RemoveRange(equipoRecargado.SlotsRam);
                                        if (equipoRecargado.Discos != null && equipoRecargado.Discos.Any())
                                            dbContext.Discos.RemoveRange(equipoRecargado.Discos);

                                        slotNumNew = 1;
                                        foreach (var slot in ListaRam)
                                        {
                                            var nuevoSlot = new GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity
                                            {
                                                CodigoEquipo = Codigo,
                                                NumeroSlot = slotNumNew++,
                                                CapacidadGB = slot.CapacidadGB,
                                                TipoMemoria = slot.TipoMemoria,
                                                Marca = slot.Marca,
                                                Frecuencia = slot.Frecuencia,
                                                Ocupado = slot.Ocupado,
                                                Observaciones = slot.Observaciones
                                            };
                                            dbContext.SlotsRam.Add(nuevoSlot);
                                        }

                                        discoNumNew = 1;
                                        foreach (var disco in ListaDiscos)
                                        {
                                            var nuevoDisco = new GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity
                                            {
                                                CodigoEquipo = Codigo,
                                                NumeroDisco = discoNumNew++,
                                                Tipo = disco.Tipo,
                                                CapacidadGB = disco.CapacidadGB,
                                                Marca = disco.Marca,
                                                Modelo = disco.Modelo
                                            };
                                            dbContext.Discos.Add(nuevoDisco);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error reintentando tras DbUpdateConcurrencyException");
                                        MessageBox.Show($"Error al intentar resolver conflicto: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        try { await transaction.RollbackAsync(); } catch { }
                                        return;
                                    }
                                }
                            }                            await transaction.CommitAsync();

                            _logger.LogInformation("Actualización de código completada y datos guardados: {Codigo}", Codigo);
                            try { WeakReferenceMessenger.Default.Send(new GestLog.Modules.GestionMantenimientos.Messages.Equipos.EquiposActualizadosMessage()); } catch { }
                            MessageBox.Show("Equipo actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        }                        
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Error en flujo de actualización de PK al cambiar {Original} -> {Nuevo}", OriginalCodigo, Codigo);
                            
                            // CRÍTICO: Rehabilitar constraints FK incluso si hay error
                            try
                            {                                
                                _logger.LogInformation("🔒 Rehabilitando constraints FK tras error...");
                                await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE SlotsRam WITH CHECK CHECK CONSTRAINT FK_SlotsRam_EquipoInformatico");
                                await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Discos WITH CHECK CHECK CONSTRAINT FK_Discos_EquipoInformatico");
                                await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE ConexionesEquiposInformaticos WITH CHECK CHECK CONSTRAINT FK_ConexionesEquiposInformaticos_EquipoInformatico_EquipoInformatico");
                            }
                            catch (Exception constraintEx)
                            {
                                _logger.LogError(constraintEx, "❌ Error rehabilitando constraints FK");
                            }

                            try { await transaction.RollbackAsync(); } catch { }
                            MessageBox.Show($"Error al actualizar el equipo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        // Fin del flujo de cambio de PK
                    }
                    else
                    {
                        // Evitar duplicados en el ChangeTracker: detach de cualquier otra instancia con la misma clave
                        try
                        {
                            var duplicates = dbContext.ChangeTracker.Entries<EquipoInformaticoEntity>()
                                .Where(x => x.Entity.Codigo == equipo.Codigo && !ReferenceEquals(x.Entity, equipo))
                                .ToList();
                            foreach (var dup in duplicates)
                            {
                                _logger.LogWarning("Detaching duplicate tracked EquipoInformaticoEntity with Codigo={Codigo}", dup.Entity.Codigo);
                                dbContext.Entry(dup.Entity).State = EntityState.Detached;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "No se pudo limpiar ChangeTracker antes de actualizar (no crítico)");
                        }

                        // Actualizar campos escalares
                        equipo.UsuarioAsignado = PersonaAsignada?.NombreCompleto ?? string.Empty;
                        equipo.NombreEquipo = NombreEquipo;
                        equipo.Sede = Sede;
                        equipo.Marca = Marca;
                        equipo.Modelo = Modelo;
                        equipo.Procesador = Procesador;
                        equipo.SO = So;
                        equipo.SerialNumber = SerialNumber;
                        equipo.Observaciones = Observaciones;
                        // Asegurar persistencia de estos campos al actualizar un equipo existente
                        equipo.Costo = Costo;
                        equipo.FechaCompra = FechaCompra;
                        equipo.CodigoAnydesk = CodigoAnydesk;
                        equipo.FechaModificacion = DateTime.Now;
                        {
                            // Antes: equipo.Estado = Estado;
                            string _msg; bool _persisted;
                            EquipoEstadoService.SetEstado(equipo, Estado, dbContext, out _msg, out _persisted);
                        }

                        // Si se marcó como Activo, limpiar la FechaBaja (volver a activo elimina la fecha de baja)
                        if (string.Equals(Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                        {
                            equipo.FechaBaja = null;
                        }

                        // Reemplazar colecciones: eliminar los antiguos y añadir los actuales
                        if (equipo.SlotsRam != null && equipo.SlotsRam.Any())
                            dbContext.SlotsRam.RemoveRange(equipo.SlotsRam);
                        if (equipo.Discos != null && equipo.Discos.Any())
                            dbContext.Discos.RemoveRange(equipo.Discos);

                        int slotNum = 1;
                        foreach (var slot in ListaRam)
                        {
                            // Crear una nueva instancia sin referencias navegacionales para evitar conflictos de tracking
                            var nuevoSlot = new GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity
                            {
                                CodigoEquipo = Codigo,
                                NumeroSlot = slotNum++,
                                CapacidadGB = slot.CapacidadGB,
                                TipoMemoria = slot.TipoMemoria,
                                Marca = slot.Marca,
                                Frecuencia = slot.Frecuencia,
                                Ocupado = slot.Ocupado,
                                Observaciones = slot.Observaciones
                            };
                            dbContext.SlotsRam.Add(nuevoSlot);
                        }

                        int discoNum = 1;
                        foreach (var disco in ListaDiscos)
                        {
                            var nuevoDisco = new GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity
                            {
                                CodigoEquipo = Codigo,
                                NumeroDisco = discoNum++,
                                Tipo = disco.Tipo,
                                CapacidadGB = disco.CapacidadGB,
                                Marca = disco.Marca,
                                Modelo = disco.Modelo
                            };                            
                            dbContext.Discos.Add(nuevoDisco);
                        }                        
                        // Reemplazar conexiones: eliminar las antiguas y añadir las actuales
                        if (equipo.Conexiones != null && equipo.Conexiones.Any())
                            dbContext.Conexiones.RemoveRange(equipo.Conexiones);

                        foreach (var conexion in ListaConexiones)
                        {
                            var nuevaConexion = new GestLog.Modules.GestionEquiposInformaticos.Models.Entities.ConexionEntity
                            {
                                CodigoEquipo = Codigo,
                                Adaptador = conexion.Adaptador,
                                DireccionMAC = conexion.DireccionMAC,
                                DireccionIPv4 = conexion.DireccionIPv4,
                                MascaraSubred = conexion.MascaraSubred,
                                PuertoEnlace = conexion.PuertoEnlace
                            };                            dbContext.Conexiones.Add(nuevaConexion);
                        }

                        // Actualizar asignaciones de periféricos (solo para el modo edición sin cambio de código)
                        // Primero, desasignar todos los periféricos que estaban asignados al equipo
                        var perifericosAsignadosAntes = await dbContext.PerifericosEquiposInformaticos
                            .Where(p => p.CodigoEquipoAsignado == Codigo)
                            .ToListAsync();
                        
                        foreach (var p in perifericosAsignadosAntes)
                        {
                            p.CodigoEquipoAsignado = null;
                        }

                        // Luego, asignar los periféricos que están actualmente en la lista
                        foreach (var periferico in PerifericosAsignados)
                        {
                            var entity = await dbContext.PerifericosEquiposInformaticos
                                .FirstOrDefaultAsync(p => p.Codigo == periferico.Codigo);
                            if (entity != null)
                            {
                                entity.CodigoEquipoAsignado = Codigo;
                            }
                        }                        try
                        {
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Equipo '{Codigo}' actualizado correctamente.", Codigo);
                            try { WeakReferenceMessenger.Default.Send(new GestLog.Modules.GestionMantenimientos.Messages.Equipos.EquiposActualizadosMessage()); } catch { }
                            MessageBox.Show("Equipo actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error al guardar cambios de actualización del equipo '{Codigo}'", Codigo);
                            MessageBox.Show($"Error al actualizar el equipo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    // Creación nueva
                    if (dbContext.EquiposInformaticos.Any(e => e.Codigo == Codigo))
                    {
                        MessageBox.Show("Ya existe un equipo con ese código.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var equipo = new EquipoInformaticoEntity
                    {
                        Codigo = Codigo,
                        UsuarioAsignado = PersonaAsignada?.NombreCompleto ?? string.Empty,
                        NombreEquipo = NombreEquipo,
                        Sede = Sede,
                        Marca = Marca,
                        Modelo = Modelo,
                        Procesador = Procesador,
                        SO = So,
                        SerialNumber = SerialNumber,
                        Observaciones = Observaciones,
                        FechaCreacion = DateTime.Now,
                        Costo = Costo,
                        FechaCompra = FechaCompra,
                        CodigoAnydesk = CodigoAnydesk
                    };
                    {
                        // Antes: equipo.Estado = Estado;
                        string _msg; bool _persisted;
                        EquipoEstadoService.SetEstado(equipo, Estado, dbContext, out _msg, out _persisted);
                    }

                    // Antes de añadir, comprobar si ya existe una instancia en el ChangeTracker con la misma clave y detachearla
                    try
                    {
                        var tracked = dbContext.ChangeTracker.Entries<EquipoInformaticoEntity>().FirstOrDefault(e => e.Entity.Codigo == equipo.Codigo && e.State != EntityState.Detached);
                        if (tracked != null)
                        {
                            _logger.LogWarning("Detaching tracked EquipoInformaticoEntity before Add, Codigo={Codigo}", tracked.Entity.Codigo);
                            dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo revisar ChangeTracker antes de Add (no crítico)");
                    }

                    dbContext.EquiposInformaticos.Add(equipo);

                    int slotNum = 1;
                    foreach (var slot in ListaRam)
                    {
                        var nuevoSlot = new GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity
                        {
                            CodigoEquipo = Codigo,
                            NumeroSlot = slotNum++,
                            CapacidadGB = slot.CapacidadGB,
                            TipoMemoria = slot.TipoMemoria,
                            Marca = slot.Marca,
                            Frecuencia = slot.Frecuencia,
                            Ocupado = slot.Ocupado,
                            Observaciones = slot.Observaciones
                        };
                        dbContext.SlotsRam.Add(nuevoSlot);
                    }

                    int discoNum = 1;
                    foreach (var disco in ListaDiscos)
                    {
                        var nuevoDisco = new GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity
                        {
                            CodigoEquipo = Codigo,
                            NumeroDisco = discoNum++,
                            Tipo = disco.Tipo,
                            CapacidadGB = disco.CapacidadGB,
                            Marca = disco.Marca,
                            Modelo = disco.Modelo
                        };                        
                        dbContext.Discos.Add(nuevoDisco);
                    }                    
                    // Agregar conexiones
                    foreach (var conexion in ListaConexiones)
                    {
                        var nuevaConexion = new GestLog.Modules.GestionEquiposInformaticos.Models.Entities.ConexionEntity
                        {
                            CodigoEquipo = Codigo,
                            Adaptador = conexion.Adaptador,
                            DireccionMAC = conexion.DireccionMAC,
                            DireccionIPv4 = conexion.DireccionIPv4,
                            MascaraSubred = conexion.MascaraSubred,
                            PuertoEnlace = conexion.PuertoEnlace                        };
                        dbContext.Conexiones.Add(nuevaConexion);
                    }

                    // Guardar asignaciones de periféricos al equipo actual
                    foreach (var periferico in PerifericosAsignados)
                    {
                        var entity = await dbContext.PerifericosEquiposInformaticos
                            .FirstOrDefaultAsync(p => p.Codigo == periferico.Codigo);
                        if (entity != null)
                        {
                            entity.CodigoEquipoAsignado = Codigo;
                        }
                    }

                    try                {
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Equipo '{Codigo}' creado correctamente.", Codigo);
                        try { WeakReferenceMessenger.Default.Send(new GestLog.Modules.GestionMantenimientos.Messages.Equipos.EquiposActualizadosMessage()); } catch { }
                        MessageBox.Show("Equipo guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al guardar nuevo equipo '{Codigo}'", Codigo);
                        MessageBox.Show($"Error al guardar el equipo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            finally
            {
                // Log de diagnóstico de discos antes de resetear el flag
                _logger.LogDebug($"[DISCOS] Lista final de discos: {string.Join(", ", ListaDiscos.Select(d => $"{d.Tipo} {d.CapacidadGB}GB {d.Marca} {d.Modelo}"))}");
                // Resetear flag de guardado
                Interlocked.Exchange(ref _isSaving, 0);
            }
        }        
        [RelayCommand(CanExecute = nameof(CanGuardarEquipo))]
        public async Task GuardarAsync()
        {
            try
            {
                // Await la operación principal de guardado para que las excepciones sean observadas
                await GuardarEquipoAsync().ConfigureAwait(true);

                // Si la ventana existe y el guardado finalizó sin excepciones, cerrar con DialogResult=true
                var win = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                if (win != null)
                {
                    // Establecer DialogResult en hilo de UI
                    Application.Current.Dispatcher.Invoke(() => win.DialogResult = true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GuardarAsync");
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Error al guardar el equipo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        // Comando para cancelar/ cerrar sin guardar (soporta binding desde XAML)
        [RelayCommand]
        public void Cancelar()
        {
            if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window win)
            {
                win.DialogResult = false;
            }
        }        
        [RelayCommand(CanExecute = nameof(CanObtenerCamposAutomaticos))]
        public async Task ObtenerCamposAutomaticosAsync()
        {
            if (IsLoadingRam) return;
            IsLoadingRam = true;
            try
            {
                _logger.LogInformation("[AgregarEquipoInformaticoViewModel] Iniciando ObtenerCamposAutomaticosAsync (overlay activado)");
                await Task.Run(() => ObtenerCamposAutomaticos());
                // También obtener discos automáticamente
                await ObtenerDiscosAutomaticosAsync();
                
                // También obtener conexiones automáticamente
                _logger.LogInformation("🔍 Iniciando detección automática de conexiones de red");
                var conexionesDetectadas = await Task.Run(() => ObtenerConexionesAutomaticas());
                
                // Limpiar la lista actual y agregar las nuevas conexiones
                ListaConexiones.Clear();
                foreach (var conexion in conexionesDetectadas)
                {
                    ListaConexiones.Add(conexion);
                }
                
                _logger.LogInformation("✅ Detección automática de conexiones completada. {Count} conexiones encontradas", conexionesDetectadas.Count);
                
                // Detección automática del sistema operativo
                if (string.IsNullOrWhiteSpace(So))
                {
                    So = System.Environment.OSVersion.VersionString;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error durante la detección automática");
                MessageBox.Show($"Error durante la detección automática: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoadingRam = false;
                _logger.LogInformation("[AgregarEquipoInformaticoViewModel] ObtenerCamposAutomaticosAsync finalizado (overlay desactivado)");
            }
        }
        private void ObtenerCamposAutomaticos()
        {
            _logger.LogInformation("🔍 Iniciando detección automática de campos del equipo");
            
            // Marca
            Marca = EjecutarCmdWmic("computersystem get manufacturer");
            if (string.IsNullOrWhiteSpace(Marca))
                Marca = EjecutarPowerShell("Get-WmiObject -Class Win32_ComputerSystem | Select-Object -ExpandProperty Manufacturer");
            
            // Modelo
            Modelo = EjecutarCmdWmic("computersystem get model");
            if (string.IsNullOrWhiteSpace(Modelo))
                Modelo = EjecutarPowerShell("Get-WmiObject -Class Win32_ComputerSystem | Select-Object -ExpandProperty Model");
            
            // Nombre equipo
            NombreEquipo = EjecutarCmd("hostname");
            if (string.IsNullOrWhiteSpace(NombreEquipo))
                NombreEquipo = EjecutarPowerShell("$env:COMPUTERNAME");
            
            // Procesador
            Procesador = EjecutarCmdWmic("cpu get name");
            if (string.IsNullOrWhiteSpace(Procesador))
                Procesador = EjecutarPowerShell("Get-WmiObject -Class Win32_Processor | Select-Object -ExpandProperty Name");
            
            // SO
            So = EjecutarCmdWmic("os get caption");
            if (string.IsNullOrWhiteSpace(So))
            {
                So = EjecutarPowerShell("Get-WmiObject -Class Win32_OperatingSystem | Select-Object -ExpandProperty Caption");
            }
            
            if (string.IsNullOrWhiteSpace(So))
            {
                So = System.Environment.OSVersion.VersionString;
            }
            
            // Serial
            SerialNumber = EjecutarCmdWmic("bios get serialnumber");
            if (string.IsNullOrWhiteSpace(SerialNumber))
                SerialNumber = EjecutarPowerShell("Get-WmiObject -Class Win32_BIOS | Select-Object -ExpandProperty SerialNumber");
            
            // RAM
            ObtenerRamAutomatica();
            
            _logger.LogInformation("✅ Detección automática de campos completada");
        }

        private string EjecutarCmdWmic(string args)
        {
            return EjecutarProceso("cmd.exe", $"/c wmic {args}");
        }
        private string EjecutarCmd(string args)
        {
            return EjecutarProceso("cmd.exe", $"/c {args}");
        }
        private string EjecutarPowerShell(string args)
        {
            return EjecutarProceso("powershell.exe", $"-Command \"{args}\"");
        }

        /// <summary>
        /// Ejecuta un comando PowerShell y devuelve la salida completa sin filtrar.
        /// Usado para comandos que requieren toda la salida (como CSV).
        /// </summary>
        private string EjecutarPowerShellCompleto(string args)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-Command \"{args}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                
                _logger.LogDebug($"[PowerShell] Ejecutando comando completo: {args}");
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (!string.IsNullOrWhiteSpace(error))
                {
                    _logger.LogWarning($"[PowerShell] Error en comando: {error}");
                }
                
                _logger.LogDebug($"[PowerShell] Salida completa recibida ({output?.Length ?? 0} caracteres)");
                return output ?? string.Empty;
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, $"[PowerShell] Error ejecutando comando: {args}");
                return string.Empty; 
            }
        }

        private string EjecutarProceso(string fileName, string arguments)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (!string.IsNullOrWhiteSpace(error))
                {
                    _logger.LogDebug("⚠️ Error del proceso {FileName}: {Error}", fileName, error);
                }
                
                if (string.IsNullOrWhiteSpace(output))
                {
                    return string.Empty;
                }
                
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                
                if (lines.Length > 1 && !lines[1].ToLower().Contains("manufacturer") && !lines[1].ToLower().Contains("model") && !lines[1].ToLower().Contains("name") && !lines[1].ToLower().Contains("caption") && !lines[1].ToLower().Contains("serialnumber"))
                {
                    return lines[1].Trim();
                }
                if (lines.Length > 0)
                {
                    return lines[0].Trim();
                }
                
                return string.Empty;
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "❌ Error ejecutando proceso {FileName} {Arguments}", fileName, arguments);
                return string.Empty; 
            }
        }        
        private void ObtenerRamAutomatica()
        {
            _logger.LogDebug("[RAM] Iniciando detección con WMI clásico");
            Application.Current.Dispatcher.Invoke(() => ListaRam.Clear());
            int totalSlotsFisicos = 0;
            try
            {
                // Intentar obtener el número total de ranuras físicas (MemoryDevices)
                try
                {
                    var arrOutput = EjecutarPowerShellCompleto("Get-WmiObject Win32_PhysicalMemoryArray | Select-Object MemoryDevices | ConvertTo-Csv -NoTypeInformation");
                    _logger.LogDebug($"[RAM] Resultado PowerShell Win32_PhysicalMemoryArray CSV: '{arrOutput}'");
                    if (!string.IsNullOrWhiteSpace(arrOutput))
                    {
                        var arrLines = arrOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (arrLines.Count > 1)
                        {
                            var parts = ParseCsvLine(arrLines[1]);
                            if (parts.Length > 0)
                            {
                                var memDevicesStr = parts[0].Trim('"');
                                if (int.TryParse(memDevicesStr, out var md))
                                {
                                    totalSlotsFisicos = md;
                                }
                            }
                        }
                    }
                }
                catch (Exception exArr)
                {
                    _logger.LogWarning(exArr, "[RAM] No se pudo obtener MemoryDevices desde Win32_PhysicalMemoryArray (no crítico)");
                }

                // Usar Get-WmiObject Win32_PhysicalMemory para obtener módulos instalados
                var output = EjecutarPowerShellCompleto("Get-WmiObject Win32_PhysicalMemory | Select-Object SMBIOSMemoryType, Capacity, Manufacturer, PartNumber, Speed | ConvertTo-Csv -NoTypeInformation");
                _logger.LogDebug($"[RAM] Resultado PowerShell Win32_PhysicalMemory CSV: '{output}'");

                if (string.IsNullOrWhiteSpace(output))
                {
                    _logger.LogWarning("[RAM] La salida de PowerShell está vacía");
                    throw new Exception("PowerShell no devolvió ningún resultado");
                }

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                _logger.LogDebug($"[RAM] Total de líneas obtenidas: {lines.Count}");

                if (lines.Count <= 1)
                {
                    _logger.LogWarning("[RAM] Solo se obtuvo la cabecera CSV, sin datos de módulos");
                    // No módulos detectados, proceder más abajo a añadir slots vacíos según totalSlotsFisicos
                }

                int slotsOcupados = 0;
                for (int i = 1; i < lines.Count; i++)
                {
                    var csv = lines[i];
                    _logger.LogDebug($"[RAM] Procesando línea CSV {i}: '{csv}'");
                    var parts = ParseCsvLine(csv);
                    _logger.LogDebug($"[RAM] Partes parseadas: {string.Join(" | ", parts)}");

                    if (parts.Length < 5)
                    {
                        _logger.LogDebug($"[RAM] Línea ignorada por longitud ({parts.Length} campos): '{csv}'");
                        continue;
                    }
                    var tipoStr = parts[0].Trim('"'); // SMBIOSMemoryType
                    var capacidadStr = parts[1].Trim('"');
                    var fabricante = parts[2].Trim('"');
                    var modelo = parts[3].Trim('"'); // PartNumber
                    var velocidad = parts[4].Trim('"');
                    _logger.LogDebug($"[RAM] Datos extraídos - Slot: {slotsOcupados+1}, Capacidad: {capacidadStr}, Marca: {fabricante}, Frecuencia: {velocidad}, Modelo: {modelo}, Tipo: {tipoStr}");

                    long capacidadBytes = 0;
                    int capacidadGB = 0;
                    if (long.TryParse(capacidadStr, out capacidadBytes))
                        capacidadGB = (int)(capacidadBytes / (1024 * 1024 * 1024));

                    var tipoMemoriaTraducido = TraducirTipoMemoria(tipoStr);
                    var tipoCombo = ObtenerTipoMemoriaCombo(tipoMemoriaTraducido);

                    var slot = new SlotRamEntity
                    {
                        NumeroSlot = ++slotsOcupados,
                        CapacidadGB = capacidadGB,
                        Marca = fabricante,
                        Frecuencia = velocidad,
                        TipoMemoria = tipoCombo ?? $"Desconocido ({tipoStr})",
                        Ocupado = true,
                        Observaciones = modelo
                    };

                    _logger.LogDebug($"[RAM] Creando slot: {slot.NumeroSlot} - {slot.CapacidadGB}GB {slot.TipoMemoria} {slot.Marca}");
                    Application.Current.Dispatcher.Invoke(() => ListaRam.Add(slot));
                }

                // Si WMI nos indicó un número total de ranuras, añadir slots vacíos para las ranuras no ocupadas
                if (totalSlotsFisicos > 0)
                {
                    _logger.LogDebug($"[RAM] Ranuras físicas detectadas: {totalSlotsFisicos}. Módulos detectados: {slotsOcupados}");
                    int start = slotsOcupados + 1;
                    for (int n = start; n <= totalSlotsFisicos; n++)
                    {
                        var emptySlot = new SlotRamEntity
                        {
                            NumeroSlot = n,
                            CapacidadGB = null,
                            Marca = string.Empty,
                            Frecuencia = string.Empty,
                            TipoMemoria = string.Empty,
                            Ocupado = false,
                            Observaciones = "Vacío"
                        };
                        Application.Current.Dispatcher.Invoke(() => ListaRam.Add(emptySlot));
                    }
                }
                else
                {
                    // Si no hay información de ranuras y no detectamos módulos, añadir un slot vacío para edición manual
                    if (ListaRam.Count == 0)
                    {
                        _logger.LogWarning("[RAM] No se detectaron módulos de RAM automáticamente y no hay información de ranuras. Añadiendo un slot vacío.");
                        Application.Current.Dispatcher.Invoke(() => ListaRam.Add(new SlotRamEntity
                        {
                            NumeroSlot = 1,
                            CapacidadGB = null,
                            Marca = string.Empty,
                            Frecuencia = string.Empty,
                            TipoMemoria = string.Empty,
                            Ocupado = false,
                            Observaciones = "Manual"
                        }));
                    }
                }

                string resumen = $"Slots ocupados: {ListaRam.Count(l => l.Ocupado)} / { (totalSlotsFisicos>0? totalSlotsFisicos.ToString(): ListaRam.Count.ToString()) }";
                _logger.LogDebug($"[RAM] Resumen: {resumen}");

                if (ListaRam.Count > 0)
                {
                    _logger.LogInformation($"[RAM] ✅ Detectados {ListaRam.Count(l => l.Ocupado)} módulos de RAM correctamente. Ranuras totales: { (totalSlotsFisicos>0? totalSlotsFisicos.ToString(): "desconocido") }");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RAM] Error en la detección con WMI clásico");
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Error al detectar la RAM automáticamente. Se ha añadido un slot vacío para edición manual.", "RAM automática", MessageBoxButton.OK, MessageBoxImage.Error));
                Application.Current.Dispatcher.Invoke(() => ListaRam.Add(new SlotRamEntity
                {
                    NumeroSlot = 1,
                    CapacidadGB = null,
                    Marca = string.Empty,
                    Frecuencia = string.Empty,
                    TipoMemoria = string.Empty,
                    Ocupado = false,
                    Observaciones = "Manual"
                }));
            }            
            _logger.LogDebug($"[RAM] Lista final de RAM: {string.Join(", ", ListaRam.Select(r => $"Slot {r.NumeroSlot}: {(r.CapacidadGB.HasValue? r.CapacidadGB.ToString()+"GB":"vacío")} {r.TipoMemoria} {r.Marca} (Ocupado={r.Ocupado})"))}");
        }

        private string[] ParseCsvLine(string line)
        {
            // Parser robusto para CSV con comillas y comas en campos
            var result = new List<string>();
            bool inQuotes = false;
            int start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"') inQuotes = !inQuotes;
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(line.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(line.Substring(start));
            return result.Select(s => s.Trim()).ToArray();
        }

        private string TraducirTipoMemoria(string memoryType)
        {
            switch (memoryType)
            {
                case "24": return "DDR3";
                case "26": return "DDR4";
                case "32": return "DDR5";
                case "30": return "LPDDR4";
                case "31": return "LPDDR5";
                default: return string.Empty;
            }
        }
        private string ObtenerTipoMemoriaCombo(string tipoTraducido)
        {
            if (!string.IsNullOrWhiteSpace(tipoTraducido) && TiposRam.Contains(tipoTraducido))
                return tipoTraducido;
            return string.Empty;
        }

        [RelayCommand(CanExecute = nameof(CanObtenerDiscosAutomaticos))]
        public async Task ObtenerDiscosAutomaticosAsync()
        {
            if (IsLoadingDiscos) return;
            IsLoadingDiscos = true;
            try
            {
                await Task.Run(() => ObtenerDiscosAutomaticos());
            }
            finally
            {
                IsLoadingDiscos = false;
            }
        }
        private void ObtenerDiscosAutomaticos()
        {
            _logger.LogDebug("[DISCOS] Iniciando detección automática de discos");
            Application.Current.Dispatcher.Invoke(() => ListaDiscos.Clear());
            try
            {
                // Usar Get-WmiObject Win32_DiskDrive para máxima compatibilidad
                var output = EjecutarPowerShellCompleto("Get-WmiObject Win32_DiskDrive | Select-Object Model, Size, MediaType, Manufacturer | ConvertTo-Csv -NoTypeInformation");
                _logger.LogDebug($"[DISCOS] Resultado PowerShell Win32_DiskDrive CSV: '{output}'");
                
                if (string.IsNullOrWhiteSpace(output))
                {
                    _logger.LogWarning("[DISCOS] La salida de PowerShell está vacía");
                    throw new Exception("PowerShell no devolvió ningún resultado");
                }
                
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                _logger.LogDebug($"[DISCOS] Total de líneas obtenidas: {lines.Count}");
                
                if (lines.Count <= 1)
                {
                    _logger.LogWarning("[DISCOS] Solo se obtuvo la cabecera CSV, sin datos de discos");
                    throw new Exception("No hay datos de discos en la salida");
                }
                int discoNum = 1;
                for (int i = 1; i < lines.Count; i++)
                {
                    var csv = lines[i];
                    var parts = ParseCsvLine(csv);
                    if (parts.Length < 4) continue;
                    var modelo = parts[0].Trim('"');
                    var sizeStr = parts[1].Trim('"');
                    var interfaz = parts[2].Trim('"');
                    var fabricante = parts[3].Trim('"');
                    long sizeBytes = 0;
                    int capacidadGB = 0;
                    if (long.TryParse(sizeStr, out sizeBytes))
                    {
                        // Cálculo comercial: dividir por 1,000,000,000 y redondear
                        capacidadGB = (int)Math.Round(sizeBytes / 1_000_000_000.0);
                        // Si la capacidad está entre 480 y 512, mostrar 512
                        if (capacidadGB >= 480 && capacidadGB < 520) capacidadGB = 512;
                    }
                    // Inferir tipo
                    string tipo = "HDD";
                    if (modelo.ToUpper().Contains("NVME")) tipo = "NVMe";
                    else if (modelo.ToUpper().Contains("SSD")) tipo = "SSD";
                    else if (modelo.ToUpper().Contains("EMMC")) tipo = "eMMC";
                    else if (interfaz == "SSD" || interfaz == "NVMe" || interfaz == "eMMC") tipo = interfaz;
                    // Extraer marca del modelo
                    string marca = fabricante;
                    var palabras = modelo.Split(' ');
                    foreach (var palabra in palabras)
                    {
                        if (palabra.ToUpper().Contains("MICRON") || palabra.ToUpper().Contains("SAMSUNG") || palabra.ToUpper().Contains("KINGSTON") || palabra.ToUpper().Contains("SEAGATE") || palabra.ToUpper().Contains("WD") || palabra.ToUpper().Contains("TOSHIBA") || palabra.ToUpper().Contains("CRUCIAL"))
                        {
                            marca = palabra;
                            break;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(marca) || marca == "(Unidades de disco estándar)") marca = modelo;
                    var disco = new DiscoEntity
                    {
                        NumeroDisco = discoNum, // asignar número secuencial (1..N)
                        Tipo = tipo,
                        CapacidadGB = capacidadGB,
                        Marca = marca,
                        Modelo = modelo
                    };
                    Application.Current.Dispatcher.Invoke(() => ListaDiscos.Add(disco));
                    discoNum++;
                }
                if (ListaDiscos.Count == 0)
                {
                    _logger.LogWarning("[DISCOS] No se detectaron discos automáticamente. Permitiendo edición manual.");
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("No se detectaron discos automáticamente. Se ha añadido un disco vacío para edición manual.", "Discos automáticos", MessageBoxButton.OK, MessageBoxImage.Warning));
                    Application.Current.Dispatcher.Invoke(() => ListaDiscos.Add(new DiscoEntity
                    {
                        NumeroDisco = ListaDiscos.Count + 1,
                        Tipo = "HDD",
                        CapacidadGB = null,
                        Marca = string.Empty,
                        Modelo = string.Empty
                    }));
                }
                else
                {
                    _logger.LogInformation($"[DISCOS] ✅ Detectados {ListaDiscos.Count} discos correctamente");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DISCOS] Error en la detección automática");
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Error al detectar los discos automáticamente. Se ha añadido un disco vacío para edición manual.", "Discos automáticos", MessageBoxButton.OK, MessageBoxImage.Error));
                Application.Current.Dispatcher.Invoke(() => ListaDiscos.Add(new DiscoEntity
                {
                    NumeroDisco = ListaDiscos.Count + 1,
                    Tipo = "HDD",
                    CapacidadGB = null,
                    Marca = string.Empty,
                    Modelo = string.Empty
                }));
            }
            _logger.LogDebug($"[DISCOS] Lista final de discos: {string.Join(", ", ListaDiscos.Select(d => $"{d.Tipo} {d.CapacidadGB}GB {d.Marca} {d.Modelo}"))}");
        }

        [RelayCommand(CanExecute = nameof(CanAgregarDiscoManual))]
        public void AgregarDiscoManual()
        {
            var nuevoDisco = new DiscoEntity
            {
                NumeroDisco = ListaDiscos.Count + 1, // Asignar número único basado en la posición (1..N)
                Tipo = "SSD",
                CapacidadGB = 256,
                Marca = string.Empty,
                Modelo = string.Empty
            };
            ListaDiscos.Add(nuevoDisco);
        }

        // Comando para agregar un slot de RAM manualmente desde la UI
        [RelayCommand(CanExecute = nameof(CanAgregarRam))]
        public void AgregarRam()
        {
            var nuevoSlot = new SlotRamEntity
            {
                NumeroSlot = ListaRam.Count + 1,
                CapacidadGB = null,
                Marca = string.Empty,
                Frecuencia = string.Empty,
                TipoMemoria = string.Empty,
                Ocupado = false,
                Observaciones = "Manual"
            };
            ListaRam.Add(nuevoSlot);
        }

        // Comando para eliminar un slot de RAM desde la UI
        [RelayCommand(CanExecute = nameof(CanEliminarRam))]
        public void EliminarRam(SlotRamEntity slot)
        {
            if (slot == null) return;
            if (ListaRam.Contains(slot))
            {
                ListaRam.Remove(slot);
                // Recalcular numeración de slots para mantener secuencia 1..N
                int idx = 1;
                foreach (var s in ListaRam)
                {
                    s.NumeroSlot = idx++;
                }
            }
        }

        // Comando para eliminar un disco desde la UI
        [RelayCommand(CanExecute = nameof(CanEliminarDisco))]
        public void EliminarDisco(DiscoEntity disco)
        {
            if (disco == null) return;
            if (ListaDiscos.Contains(disco))
            {
                ListaDiscos.Remove(disco);
                // Recalcular numeración de discos para mantener secuencia 1..N
                int idx = 1;
                foreach (var d in ListaDiscos)
                {
                    d.NumeroDisco = idx++;
                }
            }
        }

        // Comandos para gestión de conexiones de red
        private List<ConexionEntity> ObtenerConexionesAutomaticas()
        {
            var conexiones = new List<ConexionEntity>();
            
            try
            {
                // Script PowerShell mejorado para obtener solo conexiones activas (excluyendo VMware)
                var script = @"
function Convert-PrefixLengthToSubnetMask {
    param ([int]$prefixLength)

    $mask = [math]::Pow(2, 32) - [math]::Pow(2, 32 - $prefixLength)
    $bytes = [BitConverter]::GetBytes([uint32]$mask)

    if ([BitConverter]::IsLittleEndian) {
        [array]::Reverse($bytes)
    }

    return ($bytes | ForEach-Object { $_ }) -join '.'
}

Get-NetIPConfiguration | Where-Object {
    $_.IPv4Address -ne $null -and 
    (Get-NetAdapter -InterfaceDescription $_.InterfaceDescription).Status -eq 'Up' -and
    $_.InterfaceDescription -notmatch 'VMware'
} | ForEach-Object {
    $adapter = Get-NetAdapter -InterfaceDescription $_.InterfaceDescription
    $mac = $adapter.MacAddress
    $ipObj = $_.IPv4Address
    $subnet = Convert-PrefixLengthToSubnetMask $ipObj.PrefixLength

    [PSCustomObject]@{
        Adaptador     = $_.InterfaceAlias
        IPv4          = $ipObj.IPAddress
        MascaraSubred = $subnet
        Gateway       = $_.IPv4DefaultGateway.NextHop
        MAC           = $mac
    }
} | ConvertTo-Json -Depth 2
";

                var output = EjecutarPowerShellCompleto(script);
                
                if (!string.IsNullOrWhiteSpace(output))
                {
                    // Si hay un solo objeto, PowerShell no devuelve un array JSON
                    if (!output.Trim().StartsWith("["))
                    {
                        output = "[" + output + "]";
                    }
                    
                    // Parsear el JSON manualmente para mayor control
                    var lineas = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    var conexionActual = new ConexionEntity();
                    bool dentroDeObjeto = false;
                    
                    foreach (var linea in lineas)
                    {
                        var lineaLimpia = linea.Trim();
                        
                        if (lineaLimpia == "{")
                        {
                            dentroDeObjeto = true;
                            conexionActual = new ConexionEntity();
                            continue;
                        }
                        
                        if (lineaLimpia == "}" || lineaLimpia == "},")
                        {
                            if (dentroDeObjeto && !string.IsNullOrWhiteSpace(conexionActual.Adaptador))
                            {
                                conexiones.Add(conexionActual);
                            }
                            dentroDeObjeto = false;
                            continue;
                        }
                        
                        if (dentroDeObjeto && lineaLimpia.Contains(":"))
                        {
                            var partes = lineaLimpia.Split(new[] { ':' }, 2);
                            if (partes.Length == 2)
                            {
                                var clave = partes[0].Trim().Trim('"');
                                var valor = partes[1].Trim().TrimEnd(',').Trim('"');                                
                                switch (clave)
                                {
                                    case "Adaptador":
                                        conexionActual.Adaptador = valor;
                                        break;
                                    case "MAC":
                                        conexionActual.DireccionMAC = valor;
                                        break;
                                    case "IPv4":
                                        conexionActual.DireccionIPv4 = valor;
                                        break;
                                    case "MascaraSubred":
                                        conexionActual.MascaraSubred = valor;
                                        break;
                                    case "Gateway":
                                        conexionActual.PuertoEnlace = valor;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al ejecutar script PowerShell para detectar conexiones");
            }
            
            return conexiones;
        }        
        [RelayCommand(CanExecute = nameof(CanAgregarConexionManual))]
        public void AgregarConexionManual()
        {
            var nuevaConexion = new ConexionEntity
            {
                Adaptador = "Nueva Conexión",
                DireccionMAC = "",
                DireccionIPv4 = "",
                MascaraSubred = "",
                PuertoEnlace = ""
            };
            ListaConexiones.Add(nuevaConexion);
        }

        [RelayCommand(CanExecute = nameof(CanEliminarConexion))]
        public void EliminarConexion(ConexionEntity conexion)
        {
            if (conexion != null && ListaConexiones.Contains(conexion))
            {
                ListaConexiones.Remove(conexion);
            }
        }

        // Método público para cargar conexiones en modo edición
        public void CargarConexionesParaEdicion(ObservableCollection<ConexionEntity> conexiones)
        {
            ListaConexiones.Clear();
            foreach (var conexion in conexiones)
            {
                ListaConexiones.Add(conexion);
            }
        }        // Métodos para gestión de periféricos
        public async Task CargarPerifericosDisponiblesAsync()
        {
            try
            {
                // ✅ MIGRADO: Usar DbContextFactory en lugar de conexión manual
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                
                // Construir la consulta: mostrar sólo periféricos almacenados y funcionando cuando no están asignados.
                var perifericosQuery = dbContext.PerifericosEquiposInformaticos.AsQueryable();

                if (string.IsNullOrWhiteSpace(Codigo))
                {
                    // Modo "Nuevo": solo periféricos no asignados y en estado AlmacenadoFuncionando
                    perifericosQuery = perifericosQuery.Where(p => p.CodigoEquipoAsignado == null && p.Estado == EstadoPeriferico.AlmacenadoFuncionando);
                }
                else
                {
                    // Modo Edición/Existente: incluir los periféricos asignados a este equipo (cualquier estado)
                    // y además los periféricos no asignados pero que estén AlmacenadoFuncionando
                    perifericosQuery = perifericosQuery.Where(p => p.CodigoEquipoAsignado == Codigo || (p.CodigoEquipoAsignado == null && p.Estado == EstadoPeriferico.AlmacenadoFuncionando));
                }

                perifericosQuery = perifericosQuery.OrderBy(p => p.Codigo);

                var perifericos = await perifericosQuery.ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    PerifericosDisponibles.Clear();
                    PerifericosAsignados.Clear();

                    foreach (var entity in perifericos)
                    {
                        var dto = new PerifericoEquipoInformaticoDto(entity);
                        
                        if (entity.CodigoEquipoAsignado == Codigo)
                        {
                            PerifericosAsignados.Add(dto);
                        }
                        else
                        {
                            PerifericosDisponibles.Add(dto);
                        }
                    }
                });

                _logger.LogInformation("Cargados {DisponiblesCount} periféricos disponibles y {AsignadosCount} asignados", 
                    PerifericosDisponibles.Count, PerifericosAsignados.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar periféricos disponibles");
                MessageBox.Show("Error al cargar periféricos disponibles", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        [RelayCommand(CanExecute = nameof(CanGuardarEquipo))]
        public async Task AsignarPerifericoAsync(PerifericoEquipoInformaticoDto periferico)
        {
            if (periferico == null) return;

            try
            {
                // Si estamos creando un equipo (Codigo aún vacío), sólo actualizar las colecciones en memoria.
                if (string.IsNullOrWhiteSpace(Codigo))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (PerifericosDisponibles.Contains(periferico))
                        {
                            PerifericosDisponibles.Remove(periferico);
                            // Dejar CodigoEquipoAsignado tal como está (se guardará al crear el equipo)
                            periferico.CodigoEquipoAsignado = Codigo; // normalmente empty
                            // Establecer nombre del equipo si está disponible para que la UI muestre la asignación inmediata
                            periferico.NombreEquipoAsignado = !string.IsNullOrWhiteSpace(NombreEquipo) ? NombreEquipo : Codigo;
                            // Marcar como en uso al asignar
                            periferico.Estado = EstadoPeriferico.EnUso;
                            PerifericosAsignados.Add(periferico);
                        }
                    });

                    _logger.LogInformation("Periférico {Codigo} asignado en memoria (nuevo equipo, sin guardar)", periferico.Codigo);
                    return;
                }

                // Modo edición: actualizar en base de datos inmediatamente
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                var entity = await dbContext.PerifericosEquiposInformaticos
                    .FirstOrDefaultAsync(p => p.Codigo == periferico.Codigo);

                if (entity != null)
                {
                    entity.CodigoEquipoAsignado = Codigo;
                    // Marcar entidad como en uso al asignar
                    entity.Estado = EstadoPeriferico.EnUso;
                    await dbContext.SaveChangesAsync();

                    // Actualizar las listas localmente y DTO para refrescar la UI
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PerifericosDisponibles.Remove(periferico);
                        periferico.CodigoEquipoAsignado = Codigo;
                        periferico.NombreEquipoAsignado = !string.IsNullOrWhiteSpace(NombreEquipo) ? NombreEquipo : Codigo;
                        periferico.Estado = EstadoPeriferico.EnUso;
                        PerifericosAsignados.Add(periferico);
                    });

                    _logger.LogInformation("Periférico {Codigo} asignado al equipo {CodigoEquipo}", periferico.Codigo, Codigo);

                    // Notificar a otros ViewModels que los periféricos fueron actualizados (para recarga global)
                    try
                    {
                        WeakReferenceMessenger.Default.Send(new PerifericosActualizadosMessage(Codigo));
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar periférico {Codigo}", periferico.Codigo);
                MessageBox.Show($"Error al asignar periférico: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        [RelayCommand(CanExecute = nameof(CanDesasignarPeriferico))]
        public async Task DesasignarPerifericoAsync(PerifericoEquipoInformaticoDto periferico)
        {
            if (periferico == null) return;

            try
            {
                // Si estamos en modo creación (Codigo vacío), sólo actualizar colecciones en memoria
                if (string.IsNullOrWhiteSpace(Codigo))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PerifericosAsignados.Remove(periferico);
                        periferico.CodigoEquipoAsignado = null;
                        // Limpiar nombre del equipo y marcar como almacenado al desasignar
                        periferico.NombreEquipoAsignado = null;
                        periferico.Estado = EstadoPeriferico.AlmacenadoFuncionando;
                        PerifericosDisponibles.Add(periferico);
                    });

                    _logger.LogInformation("Periférico {Codigo} desasignado en memoria (nuevo equipo)", periferico.Codigo);
                    return;
                }

                // Modo edición: persistir en BD
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                var entity = await dbContext.PerifericosEquiposInformaticos
                    .FirstOrDefaultAsync(p => p.Codigo == periferico.Codigo);

                if (entity != null)
                {
                    entity.CodigoEquipoAsignado = null;
                    // Marcar como almacenado al desasignar
                    entity.Estado = EstadoPeriferico.AlmacenadoFuncionando;
                    await dbContext.SaveChangesAsync();

                    // Actualizar las listas localmente
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PerifericosAsignados.Remove(periferico);
                        periferico.CodigoEquipoAsignado = null;
                        periferico.NombreEquipoAsignado = null;
                        periferico.Estado = EstadoPeriferico.AlmacenadoFuncionando;
                        PerifericosDisponibles.Add(periferico);
                    });

                    _logger.LogInformation("Periférico {Codigo} desasignado del equipo", periferico.Codigo);

                    // Notificar a otros ViewModels que los periféricos fueron actualizados (para recarga global)
                    try
                    {
                        WeakReferenceMessenger.Default.Send(new PerifericosActualizadosMessage(Codigo));
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desasignar periférico {Codigo}", periferico.Codigo);
                MessageBox.Show($"Error al desasignar periférico: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        [RelayCommand(CanExecute = nameof(CanGuardarEquipo))]
        public async Task DarDeBajaAsync()
        {
            try
            {
                if (!IsEditing)
                {
                    MessageBox.Show("Dar de baja solo está disponible al editar un equipo existente.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(Codigo))
                {
                    MessageBox.Show("Código inválido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Mostrar diálogo modal para solicitar observación de baja
                string? observacionBaja = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var dlg = new GestLog.Modules.GestionEquiposInformaticos.Views.Equipos.DarDeBajaDialog();
                        dlg.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
                        var dr = dlg.ShowDialog();
                        if (dr == true)
                        {
                            observacionBaja = dlg.Observacion ?? string.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        // En caso de error al abrir diálogo, registrar y continuar con cancelación
                        _logger.LogError(ex, "Error al abrir el diálogo de observación de baja");
                    }
                });

                // Si el usuario canceló el diálogo o no proporcionó observación, cancelar la operación
                if (observacionBaja == null)
                {
                    // Usuario canceló
                    return;
                }

                // Opcional: validar que la observación no esté vacía. Si lo prefieres, elimina este bloque para permitir observaciones vacías.
                if (string.IsNullOrWhiteSpace(observacionBaja))
                {
                    var r = MessageBox.Show("La observación está vacía. ¿Desea continuar sin observación?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (r != MessageBoxResult.Yes) return;
                }

                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                var equipo = await dbContext.EquiposInformaticos.FirstOrDefaultAsync(e => e.Codigo == Codigo);
                if (equipo == null)
                {
                    MessageBox.Show("No se encontró el equipo en la base de datos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Anexar la observación de baja en el campo Observaciones preservando el contenido previo
                try
                {
                    var sb = new System.Text.StringBuilder();
                    if (!string.IsNullOrWhiteSpace(equipo.Observaciones))
                    {
                        sb.AppendLine(equipo.Observaciones?.Trim());
                    }
                    sb.AppendLine($"Observación de baja ({DateTime.Now:dd/MM/yyyy HH:mm}): {observacionBaja}");
                    equipo.Observaciones = sb.ToString().Trim();
                }
                catch
                {
                    // Si por alguna razón no se puede anexar, al menos asignar la observación sola
                    equipo.Observaciones = observacionBaja;
                }

                equipo.Estado = "Dado de baja";
                equipo.FechaModificacion = DateTime.Now;
                equipo.FechaBaja = DateTime.Now;

                // Desasignar periféricos
                var perifericosAsignados = await dbContext.PerifericosEquiposInformaticos.Where(p => p.CodigoEquipoAsignado == Codigo).ToListAsync();
                foreach (var p in perifericosAsignados)
                {
                    p.CodigoEquipoAsignado = null;
                    p.UsuarioAsignado = null;
                    p.Estado = GestLog.Modules.GestionEquiposInformaticos.Models.Enums.EstadoPeriferico.AlmacenadoFuncionando;
                    p.FechaModificacion = DateTime.Now;
                }                await dbContext.SaveChangesAsync();

                try { WeakReferenceMessenger.Default.Send(new GestLog.Modules.GestionMantenimientos.Messages.Equipos.EquiposActualizadosMessage()); } catch { }
                try { WeakReferenceMessenger.Default.Send(new PerifericosActualizadosMessage(Codigo)); } catch { }

                MessageBox.Show("Equipo dado de baja correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // Cerrar ventana de edición si existe
                var win = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                if (win != null) Application.Current.Dispatcher.Invoke(() => win.DialogResult = true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al dar de baja el equipo {Codigo}", Codigo);
                MessageBox.Show($"Error al dar de baja el equipo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

