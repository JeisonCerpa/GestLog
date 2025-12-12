// ✅ MIGRADO A DatabaseAwareViewModel - AUTO-REFRESH CON TIMEOUT ULTRARRÁPIDO
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Messages;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages.Equipos;
using GestLog.ViewModels.Base;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos
{
    /// <summary>
    /// ViewModel para mostrar detalles de un equipo informático específico.
    /// ✅ MIGRADO: Hereda de DatabaseAwareViewModel para auto-refresh automático con timeout ultrarrápido.
    /// </summary>
    public class DetallesEquipoInformaticoViewModel : DatabaseAwareViewModel
    {
        #region ✅ NUEVAS DEPENDENCIAS - AUTO-REFRESH
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestionEquiposInformaticosSeguimientoCronogramaService _seguimientoService;
        #endregion

        #region Propiedades del Equipo
        /// <summary>
        /// Entidad del equipo informático principal
        /// </summary>
        public EquipoInformaticoEntity Equipo { get; private set; }

        /// <summary>
        /// Colección de slots de RAM del equipo
        /// </summary>
        public ObservableCollection<SlotRamEntity> SlotsRam { get; private set; }

        /// <summary>
        /// Colección de discos del equipo
        /// </summary>
        public ObservableCollection<DiscoEntity> Discos { get; private set; }

        /// <summary>
        /// Colección de conexiones de red del equipo
        /// </summary>
        public ObservableCollection<ConexionEntity> Conexiones { get; private set; }

        /// <summary>
        /// Colección de periféricos asignados al equipo
        /// </summary>
        public ObservableCollection<PerifericoEquipoInformaticoEntity> Perifericos { get; private set; }
        #endregion

        #region Propiedades Calculadas (Headers dinámicos)
        public string RamHeader => $"Memoria RAM ({SlotsRam?.Count ?? 0})";
        public string DiscoHeader => $"Discos ({Discos?.Count ?? 0})";
        public string ConexionesHeader => $"Conexiones de Red ({Conexiones?.Count ?? 0})";
        public string PerifericosHeader => $"Periféricos Asignados ({Perifericos?.Count ?? 0})";
        #endregion

        #region Propiedades de Conveniencia (Passthrough del Equipo)
        public string Codigo => Equipo.Codigo;
        public string? NombreEquipo => Equipo.NombreEquipo;
        public string? UsuarioAsignado => Equipo.UsuarioAsignado;
        public string? Marca => Equipo.Marca;
        public string? Modelo => Equipo.Modelo;
        public string? SO => Equipo.SO;
        public string? SerialNumber => Equipo.SerialNumber;
        public string? Procesador => Equipo.Procesador;
        public string? CodigoAnydesk => Equipo.CodigoAnydesk;
        public decimal? Costo => Equipo.Costo;
        public DateTime? FechaCompra => Equipo.FechaCompra;
        public DateTime? FechaBaja => Equipo.FechaBaja;
        public string? Observaciones => Equipo.Observaciones;
        public DateTime FechaCreacion => Equipo.FechaCreacion;
        public DateTime? FechaModificacion => Equipo.FechaModificacion;
        public string? Estado => Equipo.Estado;
        public string? Sede => Equipo.Sede;
        #endregion

        #region Propiedades Formateadas (Cultura Española)
        private static readonly CultureInfo SpanishCulture = new CultureInfo("es-ES");
        public string FechaCreacionFormatted => $"Creado: {Equipo.FechaCreacion.ToString("f", SpanishCulture)}";
        public string FechaModificacionFormatted => Equipo.FechaModificacion.HasValue 
            ? $"Modificado: {Equipo.FechaModificacion.Value.ToString("g", SpanishCulture)}" 
            : "Modificado: -";
        #endregion

        #region ✅ CONSTRUCTOR MIGRADO - DatabaseAwareViewModel
        /// <summary>
        /// Constructor migrado a DatabaseAwareViewModel con auto-refresh automático
        /// </summary>
        public DetallesEquipoInformaticoViewModel(
            EquipoInformaticoEntity equipo,
            IDbContextFactory<GestLogDbContext> dbContextFactory,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger,
            IGestionEquiposInformaticosSeguimientoCronogramaService seguimientoService)
            : base(databaseService, logger) // ✅ MIGRADO: Llama al constructor base para auto-refresh
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _seguimientoService = seguimientoService ?? throw new ArgumentNullException(nameof(seguimientoService));
            Equipo = equipo ?? throw new ArgumentNullException(nameof(equipo));

            // Inicializar colecciones vacías
            SlotsRam = new ObservableCollection<SlotRamEntity>();
            Discos = new ObservableCollection<DiscoEntity>();
            Conexiones = new ObservableCollection<ConexionEntity>();
            Perifericos = new ObservableCollection<PerifericoEquipoInformaticoEntity>();

            // ✅ MIGRADO: Configurar eventos de cambio de colección para headers dinámicos
            ConfigurarEventosColecciones();

            // ✅ MIGRADO: Carga inicial de datos de forma asíncrona (auto-refresh se activa automáticamente)
            _ = InicializarAsync();
        }
        #endregion

        #region ✅ MÉTODOS MIGRADOS - DatabaseAwareViewModel        /// <summary>
        /// ✅ MIGRADO: Implementación requerida para DatabaseAwareViewModel
        /// Carga los datos del equipo desde la base de datos de forma segura
        /// </summary>
        protected override async Task RefreshDataAsync()
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync();

                // Cargar equipo actualizado con todas las relaciones
                var equipoActualizado = await context.EquiposInformaticos
                    .Include(e => e.SlotsRam)
                    .Include(e => e.Discos)
                    .Include(e => e.Conexiones)
                    .FirstOrDefaultAsync(e => e.Codigo == Equipo.Codigo);

                if (equipoActualizado != null)
                {
                    // Actualizar entidad principal
                    ActualizarEquipoPrincipal(equipoActualizado);

                    // Actualizar colecciones relacionadas
                    await ActualizarColeccionesAsync(context);
                }

                // Nota: se removió LogDebug muy verboso para reducir ruido de logs en producción
            }
            catch (Exception ex)
            {
                // ✅ PATRÓN: Manejo silencioso de errores de conexión (no bloquea UI)
                _logger.LogWarning(ex, "[DetallesEquipoInformaticoViewModel] Error actualizando datos para equipo {Codigo} - continuando con datos locales", Equipo.Codigo);
            }
        }        /// <summary>
        /// ✅ NUEVO: Método de inicialización asíncrona para carga inicial
        /// </summary>
        private async Task InicializarAsync()
        {
            try
            {
                // Carga inicial de datos
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DetallesEquipoInformaticoViewModel] Error en inicialización para equipo {Codigo}", Equipo.Codigo);
            }
        }
        #endregion

        #region Métodos Auxiliares de Actualización

        /// <summary>
        /// Actualiza las propiedades del equipo principal manteniendo la referencia original
        /// </summary>
        private void ActualizarEquipoPrincipal(EquipoInformaticoEntity equipoActualizado)
        {
            // Actualizar propiedades escalares sin cambiar la referencia del objeto
            Equipo.NombreEquipo = equipoActualizado.NombreEquipo;
            Equipo.UsuarioAsignado = equipoActualizado.UsuarioAsignado;
            Equipo.Marca = equipoActualizado.Marca;
            Equipo.Modelo = equipoActualizado.Modelo;
            Equipo.SO = equipoActualizado.SO;
            Equipo.SerialNumber = equipoActualizado.SerialNumber;
            Equipo.Procesador = equipoActualizado.Procesador;
            Equipo.CodigoAnydesk = equipoActualizado.CodigoAnydesk;
            Equipo.Costo = equipoActualizado.Costo;
            Equipo.FechaCompra = equipoActualizado.FechaCompra;
            Equipo.FechaBaja = equipoActualizado.FechaBaja;
            Equipo.Observaciones = equipoActualizado.Observaciones;
            Equipo.FechaModificacion = equipoActualizado.FechaModificacion;
            Equipo.Estado = equipoActualizado.Estado;
            Equipo.Sede = equipoActualizado.Sede;

            // Notificar cambios en propiedades calculadas
            OnPropertyChanged(nameof(NombreEquipo));
            OnPropertyChanged(nameof(UsuarioAsignado));
            OnPropertyChanged(nameof(Marca));
            OnPropertyChanged(nameof(Modelo));
            OnPropertyChanged(nameof(SO));
            OnPropertyChanged(nameof(SerialNumber));
            OnPropertyChanged(nameof(Procesador));
            OnPropertyChanged(nameof(CodigoAnydesk));
            OnPropertyChanged(nameof(Costo));
            OnPropertyChanged(nameof(FechaCompra));
            OnPropertyChanged(nameof(FechaBaja));
            OnPropertyChanged(nameof(Observaciones));
            OnPropertyChanged(nameof(FechaModificacion));
            OnPropertyChanged(nameof(Estado));
            OnPropertyChanged(nameof(Sede));
            OnPropertyChanged(nameof(FechaModificacionFormatted));
        }

        /// <summary>
        /// Actualiza las colecciones relacionadas del equipo
        /// </summary>
        private async Task ActualizarColeccionesAsync(GestLogDbContext context)
        {
            // Actualizar Slots de RAM
            var slotsActualizados = await context.SlotsRam
                .Where(s => s.CodigoEquipo == Equipo.Codigo)
                .ToListAsync();
            ActualizarColeccion(SlotsRam, slotsActualizados);

            // Actualizar Discos
            var discosActualizados = await context.Discos
                .Where(d => d.CodigoEquipo == Equipo.Codigo)
                .ToListAsync();
            ActualizarColeccion(Discos, discosActualizados);

            // Actualizar Conexiones
            var conexionesActualizadas = await context.Conexiones
                .Where(c => c.CodigoEquipo == Equipo.Codigo)
                .ToListAsync();
            ActualizarColeccion(Conexiones, conexionesActualizadas);

            // Actualizar Periféricos Asignados
            var perifericosAsignados = await context.PerifericosEquiposInformaticos
                .Where(p => p.CodigoEquipoAsignado == Equipo.Codigo)
                .ToListAsync();
            ActualizarColeccion(Perifericos, perifericosAsignados);
        }

        /// <summary>
        /// Método genérico para actualizar colecciones de forma eficiente
        /// </summary>
        private void ActualizarColeccion<T>(ObservableCollection<T> coleccionLocal, 
            System.Collections.Generic.List<T> datosActualizados) where T : class
        {
            // Limpiar y repoblar la colección
            coleccionLocal.Clear();
            foreach (var item in datosActualizados)
            {
                coleccionLocal.Add(item);
            }
        }

        /// <summary>
        /// Configura eventos de cambio de colección para actualizar headers dinámicos
        /// </summary>
        private void ConfigurarEventosColecciones()
        {
            SlotsRam.CollectionChanged += (s, e) => OnPropertyChanged(nameof(RamHeader));
            Discos.CollectionChanged += (s, e) => OnPropertyChanged(nameof(DiscoHeader));
            Conexiones.CollectionChanged += (s, e) => OnPropertyChanged(nameof(ConexionesHeader));
            Perifericos.CollectionChanged += (s, e) => OnPropertyChanged(nameof(PerifericosHeader));
        }
        #endregion

        #region Métodos de Negocio (Estados del Equipo)        /// <summary>
        /// Intenta dar de baja el equipo: si hay DbContext persiste los cambios; si no, actualiza la entidad en memoria.
        /// Devuelve true si la operación se completó (aunque sea solo en memoria), false si hubo error fatal.
        /// </summary>
        public bool DarDeBaja(out string mensaje, out bool persistedToDb)
        {
            try
            {
                return SetEstado("Dado de baja", out mensaje, out persistedToDb);
            }
            catch (Exception ex)
            {
                mensaje = $"Error al dar de baja: {ex.Message}";
                persistedToDb = false;
                _logger.LogError(ex, "[DetallesEquipoInformaticoViewModel] Error al dar de baja equipo {Codigo}", Equipo.Codigo);
                return false;
            }
        }

        /// <summary>
        /// Establece un nuevo estado al equipo. Si el estado es 'Activo', limpia FechaBaja; si es 'Dado de baja', establece FechaBaja a ahora.
        /// Persiste en DB si hay DbContext disponible.
        /// </summary>
        public bool SetEstado(string nuevoEstado, out string mensaje, out bool persistedToDb)
        {
            mensaje = string.Empty;
            persistedToDb = false;

            try
            {
                bool esActivo = string.Equals(nuevoEstado, "Activo", StringComparison.OrdinalIgnoreCase);
                bool esDadoBaja = string.Equals(nuevoEstado, "Dado de baja", StringComparison.OrdinalIgnoreCase) || 
                                 string.Equals(nuevoEstado, "DadoDeBaja", StringComparison.OrdinalIgnoreCase);

                // Intentar persitir en base de datos
                using (var context = _dbContextFactory.CreateDbContext())
                {
                    var equipoRef = context.EquiposInformaticos.FirstOrDefault(e => e.Codigo == Equipo.Codigo);
                    if (equipoRef != null)
                    {
                        equipoRef.Estado = nuevoEstado;
                        equipoRef.FechaModificacion = DateTime.Now;

                        if (esActivo)
                            equipoRef.FechaBaja = null;

                        if (esDadoBaja)
                            equipoRef.FechaBaja = DateTime.Now;

                        // Si damos de baja el equipo, desasignar periféricos asociados y marcarlos como AlmacenadoFuncionando
                        if (esDadoBaja)
                        {
                            try
                            {
                                var perifAsignados = context.PerifericosEquiposInformaticos
                                    .Where(p => p.CodigoEquipoAsignado == Equipo.Codigo)
                                    .ToList();

                                foreach (var p in perifAsignados)
                                {
                                    p.CodigoEquipoAsignado = null;
                                    // además de desasignar el código del equipo, limpiar el usuario asignado
                                    p.UsuarioAsignado = null;
                                    p.Estado = EstadoPeriferico.AlmacenadoFuncionando;
                                    p.FechaModificacion = DateTime.Now;
                                }
                            }
                            catch (Exception exPer)
                            {
                                _logger.LogWarning(exPer, "[DetallesEquipoInformaticoViewModel] Error desasignando periféricos al dar de baja el equipo {Codigo}", Equipo.Codigo);
                            }
                        }

                        context.SaveChanges();

                        // Sincronizar entidad en memoria
                        Equipo.Estado = equipoRef.Estado;
                        Equipo.FechaModificacion = equipoRef.FechaModificacion;
                        Equipo.FechaBaja = equipoRef.FechaBaja;

                        // Si la operación fue una baja persistida, iniciar en background la desactivación de planes y eliminación de seguimientos futuros
                        if (esDadoBaja)
                        {
                            try
                            {
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await _seguimientoService.DeletePendientesFuturasByEquipoCodigoAsync(Equipo.Codigo);
                                    }
                                    catch (Exception exSvc)
                                    {
                                        _logger.LogWarning(exSvc, "[DetallesEquipoInformaticoViewModel] Error ejecutando limpieza de cronograma para {Codigo}", Equipo.Codigo);
                                    }
                                });
                            }
                            catch (Exception exTask)
                            {
                                _logger.LogWarning(exTask, "[DetallesEquipoInformaticoViewModel] No se pudo iniciar la tarea de limpieza de cronograma para {Codigo}", Equipo.Codigo);
                            }
                        }

                        // Forzar recarga de periféricos desde DB para asegurar actualización de la UI
                        try
                        {
                            // Ejecutar recarga en segundo plano (no bloquear el hilo que llamó a SetEstado)
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await using var reloadContext = await _dbContextFactory.CreateDbContextAsync();
                                    await ActualizarColeccionesAsync(reloadContext);

                                    // Notificar cambios en el hilo UI
                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        OnPropertyChanged(nameof(PerifericosHeader));
                                        OnPropertyChanged(nameof(Perifericos));
                                    });

                                    // Se elimina el envío redundante y el log informativo interno para reducir verbosidad.
                                    // El envío se realizará una sola vez al final del método SetEstado.
                                }
                                catch (Exception exBg)
                                {
                                    _logger.LogWarning(exBg, "[DetallesEquipoInformaticoViewModel] Error en recarga en background de periféricos tras dar de baja {Codigo}", Equipo.Codigo);
                                }
                            });
                        }
                        catch (Exception exUi)
                        {
                            _logger.LogWarning(exUi, "[DetallesEquipoInformaticoViewModel] No se pudo iniciar recarga en background tras dar de baja {Codigo}, intentando actualizar en memoria", Equipo.Codigo);

                            // Fallback: intentar actualizar los periféricos en memoria si la recarga falla
                            try
                            {
                                var perifericosEnUi = Perifericos?.Where(p => p.CodigoEquipoAsignado == Equipo.Codigo).ToList();
                                if (perifericosEnUi != null && perifericosEnUi.Any())
                                {
                                    foreach (var p in perifericosEnUi)
                                    {
                                        p.CodigoEquipoAsignado = null;
                                        // limpiar usuario en la colección en memoria
                                        p.UsuarioAsignado = null;
                                        p.Estado = EstadoPeriferico.AlmacenadoFuncionando;
                                        p.FechaModificacion = DateTime.Now;
                                    }
                                    // Notificar cambio de header y colección
                                    OnPropertyChanged(nameof(PerifericosHeader));
                                    OnPropertyChanged(nameof(Perifericos));
                                }
                            }
                            catch (Exception exMem)
                            {
                                _logger.LogWarning(exMem, "[DetallesEquipoInformaticoViewModel] Error fallback actualizando periféricos en memoria tras dar de baja {Codigo}", Equipo.Codigo);
                            }
                        }

                        // Notificar cambios
                        OnPropertyChanged(nameof(Estado));
                        OnPropertyChanged(nameof(FechaModificacion));
                        OnPropertyChanged(nameof(FechaBaja));
                        OnPropertyChanged(nameof(FechaModificacionFormatted));                        // Enviar mensaje de actualización
                        try 
                        { 
                            WeakReferenceMessenger.Default.Send(new EquiposActualizadosMessage()); 
                        } 
                        catch (Exception msgEx) 
                        { 
                            _logger.LogWarning(msgEx, "[DetallesEquipoInformaticoViewModel] Error enviando mensaje de actualización");
                        }

                        // Enviar mensaje específico de periféricos actualizados para notificar vistas que listan periféricos
                        try
                        {
                            WeakReferenceMessenger.Default.Send(new PerifericosActualizadosMessage(Equipo.Codigo));
                            // Cambiado a LogDebug para no saturar los logs, warnings/errors se conservan
                            _logger.LogDebug("[DetallesEquipoInformaticoViewModel] PerifericosActualizadosMessage enviado para {Codigo}", Equipo.Codigo);
                        }
                        catch (Exception exMsgPer)
                        {
                            _logger.LogWarning(exMsgPer, "[DetallesEquipoInformaticoViewModel] Error enviando PerifericosActualizadosMessage para {Codigo}", Equipo.Codigo);
                        }

                        mensaje = esActivo ? "Equipo marcado como activo y fecha de baja eliminada." 
                            : (esDadoBaja ? "Equipo dado de baja correctamente. Se inició la desactivación de planes y eliminación de seguimientos futuros (soft-disable)." : "Estado actualizado correctamente.");
                        persistedToDb = true;
                        return true;
                    }
                }

                // Fallback en memoria si no se puede persistir
                Equipo.Estado = nuevoEstado;
                Equipo.FechaModificacion = DateTime.Now;
                if (esActivo)
                    Equipo.FechaBaja = null;
                if (esDadoBaja)
                    Equipo.FechaBaja = DateTime.Now;

                // Si damos de baja y no se pudo persistir, actualizar periféricos en memoria
                if (esDadoBaja)
                {
                    try
                    {
                        var perifMem = Perifericos?.Where(p => p.CodigoEquipoAsignado == Equipo.Codigo).ToList();
                        if (perifMem != null && perifMem.Any())
                        {
                            foreach (var p in perifMem)
                            {
                                p.CodigoEquipoAsignado = null;
                                p.UsuarioAsignado = null;
                                p.Estado = EstadoPeriferico.AlmacenadoFuncionando;
                                p.FechaModificacion = DateTime.Now;
                            }
                            OnPropertyChanged(nameof(PerifericosHeader));
                            OnPropertyChanged(nameof(Perifericos));
                        }
                    }
                    catch (Exception exMem)
                    {
                        _logger.LogWarning(exMem, "[DetallesEquipoInformaticoViewModel] Error actualizando periféricos en memoria tras marcar baja para {Codigo}", Equipo.Codigo);
                    }
                }

                // Notificar cambios
                OnPropertyChanged(nameof(Estado));
                OnPropertyChanged(nameof(FechaModificacion));
                OnPropertyChanged(nameof(FechaBaja));
                OnPropertyChanged(nameof(FechaModificacionFormatted));

                mensaje = esActivo ? "Estado actualizado en memoria: equipo marcado como activo y fecha de baja eliminada." 
                    : (esDadoBaja ? "Estado actualizado en memoria: equipo marcado como dado de baja." : "Estado actualizado en memoria.");
                persistedToDb = false;
                return true;
            }            catch (Exception ex)
            {
                mensaje = $"Error al actualizar estado: {ex.Message}";
                persistedToDb = false;
                _logger.LogError(ex, "[DetallesEquipoInformaticoViewModel] Error al actualizar estado del equipo {Codigo} a {Estado}", Equipo.Codigo, nuevoEstado);
                return false;
            }
        }
        #endregion
    }
}
