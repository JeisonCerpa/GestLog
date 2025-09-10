using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
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

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public partial class AgregarEquipoInformaticoViewModel : ObservableObject
    {
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
        private string estado = "Activo";

        public string[] TiposRam { get; } = new[] { "DDR3", "DDR4", "DDR5", "LPDDR4", "LPDDR5" };
        public string[] TiposDisco { get; } = new[] { "HDD", "SSD", "NVMe", "eMMC" };

        [ObservableProperty]
        private bool isLoadingDiscos;        
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private bool canGuardarEquipo;
        [ObservableProperty]
        private bool canObtenerCamposAutomaticos;
        [ObservableProperty]
        private bool canObtenerDiscosAutomaticos;
        [ObservableProperty]
        private bool canAgregarDiscoManual;
        [ObservableProperty]
        private bool canEliminarDisco;        
        private int _isSaving = 0; // 0 = no guardando, 1 = guardando
        public AgregarEquipoInformaticoViewModel(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            
            // Obtener logger del service provider
            var serviceProvider = LoggingService.GetServiceProvider();
            _logger = serviceProvider.GetRequiredService<IGestLogLogger>();
            
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
        }

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
            CanEliminarDisco = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
        }

        public async Task InicializarAsync()
        {
            try
            {
                var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<GestLogDbContext>()
                    .UseSqlServer(GetProductionConnectionString())
                    .Options;

                // Ejecutar la consulta en background para no bloquear la UI
                var personas = await Task.Run(() =>
                {
                    using var dbContext = new GestLogDbContext(options);
                    return dbContext.Personas.Where(p => p.Activo).ToList();
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
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar las personas para usuario asignado");
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("No se pudieron cargar los usuarios disponibles.", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private string GetProductionConnectionString()
        {
            // Configuración de conexión de producción
            return "Server=SIMICSGROUPWKS1\\SIMICSBD;Database=BD_ Pruebas;User Id=sa;Password=REMOVED_SECRET;TrustServerCertificate=True;Connection Timeout=30;";
        }

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
                {
                    MessageBox.Show("El código y el nombre del equipo son obligatorios.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<GestLogDbContext>()
                    .UseSqlServer(GetProductionConnectionString())
                    .Options;

                _logger.LogInformation("Iniciando GuardarEquipoAsync. IsEditing={IsEditing} OriginalCodigo={OriginalCodigo} Codigo={Codigo}", IsEditing, OriginalCodigo, Codigo);

                using var dbContext = new GestLogDbContext(options);

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
                        .FirstOrDefault(e => e.Codigo == OriginalCodigo);

                    if (equipo == null)
                    {
                        MessageBox.Show("No se encontró el equipo a actualizar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Si cambió el código primario, realizar flujo seguro: actualizar PK y FKs via SQL dentro de transacción
                    if (!string.Equals(OriginalCodigo, Codigo, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Cambio de PK detectado: {Original} -> {Nuevo}. Usando flujo de actualización segura.", OriginalCodigo, Codigo);                        using var transaction = await dbContext.Database.BeginTransactionAsync();
                        try
                        {
                            _logger.LogInformation("💾 Iniciando actualización de PK con deshabilitar constraints: {Original} -> {Nuevo}", OriginalCodigo, Codigo);

                            // SOLUCIÓN ROBUSTA: Deshabilitar temporalmente las constraints FK para permitir la actualización de PK
                            _logger.LogInformation("🔓 Deshabilitando constraints FK temporalmente...");
                            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE SlotsRam NOCHECK CONSTRAINT FK_SlotsRam_EquipoInformatico");
                            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Discos NOCHECK CONSTRAINT FK_Discos_EquipoInformatico");

                            // 1. Actualizar la Primary Key en EquiposInformaticos
                            var rowsEquipo = await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE EquiposInformaticos SET Codigo = {Codigo} WHERE Codigo = {OriginalCodigo}");
                            _logger.LogInformation("📋 Equipo actualizado: {RowsAffected} filas", rowsEquipo);

                            // 2. Actualizar las Foreign Keys en tablas relacionadas
                            var rowsSlots = await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE SlotsRam SET CodigoEquipo = {Codigo} WHERE CodigoEquipo = {OriginalCodigo}");
                            _logger.LogInformation("💾 SlotsRam actualizados: {RowsAffected} filas", rowsSlots);

                            var rowsDiscos = await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE Discos SET CodigoEquipo = {Codigo} WHERE CodigoEquipo = {OriginalCodigo}");
                            _logger.LogInformation("💿 Discos actualizados: {RowsAffected} filas", rowsDiscos);

                            // 3. Rehabilitar las constraints FK
                            _logger.LogInformation("🔒 Rehabilitando constraints FK...");
                            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE SlotsRam WITH CHECK CHECK CONSTRAINT FK_SlotsRam_EquipoInformatico");
                            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Discos WITH CHECK CHECK CONSTRAINT FK_Discos_EquipoInformatico");

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
                            }

                            await transaction.CommitAsync();

                            _logger.LogInformation("Actualización de código completada y datos guardados: {Codigo}", Codigo);
                            MessageBox.Show("Equipo actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        }                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Error en flujo de actualización de PK al cambiar {Original} -> {Nuevo}", OriginalCodigo, Codigo);
                            
                            // CRÍTICO: Rehabilitar constraints FK incluso si hay error
                            try
                            {
                                _logger.LogInformation("🔒 Rehabilitando constraints FK tras error...");
                                await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE SlotsRam WITH CHECK CHECK CONSTRAINT FK_SlotsRam_EquipoInformatico");
                                await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Discos WITH CHECK CHECK CONSTRAINT FK_Discos_EquipoInformatico");
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

                        try
                        {
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Equipo '{Codigo}' actualizado correctamente.", Codigo);
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

                    try
                    {
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Equipo '{Codigo}' creado correctamente.", Codigo);
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
                await Task.Run(() => ObtenerCamposAutomaticos());
                // También obtener discos automáticamente
                await ObtenerDiscosAutomaticosAsync();                
                // Detección automática del sistema operativo
                if (string.IsNullOrWhiteSpace(So))
                {
                    So = System.Environment.OSVersion.VersionString;
                }
            }
            finally
            {
                IsLoadingRam = false;
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
            try
            {
                // Usar Get-WmiObject Win32_PhysicalMemory para máxima compatibilidad
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
                    throw new Exception("No hay datos de módulos de RAM en la salida");
                }
                
                int slotsOcupados = 0;
                for (int i = 1; i < lines.Count; i++)
                {
                    var csv = lines[i];
                    _logger.LogDebug($"[RAM] Procesando línea CSV {i}: '{csv}'");
                    // El CSV puede tener comas en PartNumber, así que usar un parser robusto
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
                
                string resumen = $"Slots ocupados: {slotsOcupados}";
                _logger.LogDebug($"[RAM] Resumen: {resumen}");
                
                if (ListaRam.Count == 0)
                {
                    _logger.LogWarning("[RAM] No se detectaron módulos de RAM automáticamente. Permitiendo edición manual.");
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("No se detectaron módulos de RAM automáticamente. Se ha añadido un slot vacío para edición manual.\n" + resumen, "RAM automática", MessageBoxButton.OK, MessageBoxImage.Warning));
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
                else
                {
                    _logger.LogInformation($"[RAM] ✅ Detectados {ListaRam.Count} módulos de RAM correctamente");
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
            _logger.LogDebug($"[RAM] Lista final de RAM: {string.Join(", ", ListaRam.Select(r => $"Slot {r.NumeroSlot}: {r.CapacidadGB}GB {r.TipoMemoria} {r.Marca}"))}");
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
                Tipo = "SSD",
                CapacidadGB = 256,
                Marca = string.Empty,
                Modelo = string.Empty
            };
            ListaDiscos.Add(nuevoDisco);
        }
    }
}
