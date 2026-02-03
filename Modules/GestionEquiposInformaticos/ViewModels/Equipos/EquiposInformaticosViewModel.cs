using System.Collections.ObjectModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Views.Equipos;
using GestLog.Modules.GestionEquiposInformaticos.Views.Cronograma;
using GestLog.Modules.GestionEquiposInformaticos.Views.Perifericos;
using GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento;
using GestLog.Modules.GestionEquiposInformaticos.Messages;
using System.Windows;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Win32;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using GestLog.Services.Equipos;
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base auto-refresh
using GestLog.Services.Interfaces;       // ✅ NUEVO: IDatabaseConnectionService
using GestLog.Services.Core.Logging;    // ✅ NUEVO: IGestLogLogger
using System;
using System.Threading.Tasks;
using System.Threading;
using ClosedXML.Excel;                   // ✅ NUEVO: Para exportación a Excel
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;  // Desambiguar SaveFileDialog

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos
{
    public partial class EquiposInformaticosViewModel : DatabaseAwareViewModel
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;

        public ObservableCollection<EquipoInformaticoEntity> ListaEquiposInformaticos { get; set; } = new();

        [ObservableProperty]
        private bool canCrearEquipo;
        [ObservableProperty]
        private bool canEditarEquipo;
        [ObservableProperty]
        private bool canDarDeBajaEquipo;
        [ObservableProperty]
        private bool canVerHistorial;
        [ObservableProperty]
        private bool canExportarDatos;

        [ObservableProperty]
        private string filtroEquipo = string.Empty;

        [ObservableProperty]
        private bool showDadoDeBaja = false;

        [ObservableProperty]
        private ICollectionView? equiposView;

        // Estadísticas de estados (contadores) mostrados en la barra
        [ObservableProperty]
        private int equiposActivos;
        [ObservableProperty]
        private int equiposEnMantenimiento;
        [ObservableProperty]
        private int equiposEnReparacion;
        [ObservableProperty]
        private int equiposInactivos;
        [ObservableProperty]
        private int equiposDadosBaja;        public EquiposInformaticosViewModel(
            IDbContextFactory<GestLogDbContext> dbContextFactory,
            ICurrentUserService currentUserService,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
            EquiposView = CollectionViewSource.GetDefaultView(ListaEquiposInformaticos);
            if (EquiposView != null)
                EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
            
            // Suscribir cambios en la colección para recalcular las estadísticas automáticamente
            ListaEquiposInformaticos.CollectionChanged += (s, e) => RecalcularEstadisticas();
            
            // 📬 Suscribirse a notificaciones de cambios en mantenimientos correctivos
            WeakReferenceMessenger.Default.Register<MantenimientosCorrectivosActualizadosMessage>(this, (r, m) =>
            {
                // Refrescar la lista de equipos cuando hay cambios en mantenimientos
                _ = RefreshDataAsync();
            });
                
            // Inicialización asíncrona
            _ = InicializarAsync();
        }

        /// <summary>
        /// Implementación del método abstracto para auto-refresh automático
        /// </summary>
        protected override async Task RefreshDataAsync()
        {
            try
            {
                // Logs de refresco automáticos degradados a Debug para reducir ruido en producción
                _logger.LogDebug("[EquiposInformaticosViewModel] Refrescando datos automáticamente");
                await CargarEquiposAsync();
                _logger.LogDebug("[EquiposInformaticosViewModel] Datos refrescados exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al refrescar datos");
                throw;
            }
        }

        /// <summary>
        /// Override para manejar cuando se pierde la conexión específicamente para equipos
        /// </summary>
        protected override void OnConnectionLost()
        {
            StatusMessage = "Sin conexión - Gestión de equipos no disponible";
        }

        /// <summary>
        /// Método de inicialización asíncrona con timeout ultrarrápido
        /// </summary>
        public async Task InicializarAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Cargando equipos...";
                
                await CargarEquiposAsync();
                
                StatusMessage = $"Cargados {ListaEquiposInformaticos.Count} equipos";
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[EquiposInformaticosViewModel] Timeout - sin conexión BD");
                StatusMessage = "Sin conexión - Módulo no disponible";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al inicializar");
                StatusMessage = "Error al cargar equipos";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Comando público para recargar manualmente la lista de equipos desde la vista
        [RelayCommand]
        public async Task CargarEquipos()
        {
            try
            {
                IsLoading = true;
                await CargarEquiposAsync();
                StatusMessage = $"Cargados {ListaEquiposInformaticos.Count} equipos";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al cargar equipos manualmente");
                StatusMessage = "Error al actualizar equipos";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }

        private void RecalcularPermisos()
        {
            CanCrearEquipo = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanEditarEquipo = _currentUser.HasPermission("EquiposInformaticos.EditarEquipo");
            CanDarDeBajaEquipo = _currentUser.HasPermission("EquiposInformaticos.DarDeBajaEquipo");
            CanVerHistorial = _currentUser.HasPermission("EquiposInformaticos.VerHistorial");
            CanExportarDatos = _currentUser.HasPermission("EquiposInformaticos.ExportarDatos");
        }

        partial void OnFiltroEquipoChanged(string value)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => EquiposView?.Refresh());
        }

        partial void OnShowDadoDeBajaChanged(bool value)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => EquiposView?.Refresh());
        }

        private bool FiltrarEquipo(object obj)
        {
            if (obj is not EquipoInformaticoEntity eq) return false;
            // Filtrar por estado DadoDeBaja según toggle
            if (!ShowDadoDeBaja && (eq.Estado?.Trim().ToLowerInvariant() == "dadodebaja" || eq.Estado?.Trim().ToLowerInvariant() == "dado de baja"))
                return false;
            if (string.IsNullOrWhiteSpace(FiltroEquipo)) return true;
            
            var terminos = FiltroEquipo.Split(';')
                .Select(t => RemoverTildes(t.Trim()).ToLowerInvariant())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToArray();
                
            var campos = new[]
            {
                RemoverTildes(eq.Codigo ?? "").ToLowerInvariant(),
                RemoverTildes(eq.UsuarioAsignado ?? "").ToLowerInvariant(),
                RemoverTildes(eq.NombreEquipo ?? "").ToLowerInvariant(),
                RemoverTildes(eq.Marca ?? "").ToLowerInvariant(),
                RemoverTildes(eq.Sede ?? "").ToLowerInvariant(),
            };
            
            return terminos.All(term => campos.Any(campo => campo.Contains(term)));
        }

        private string RemoverTildes(string texto)
        {
            return texto
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ü", "u")
                .Replace("Á", "A").Replace("É", "E").Replace("Í", "I")
                .Replace("Ó", "O").Replace("Ú", "U").Replace("Ü", "U")
                .Replace("ñ", "n").Replace("Ñ", "N");
        }

        /// <summary>
        /// Carga todos los equipos desde la base de datos con timeout ultrarrápido
        /// </summary>
        private async Task CargarEquiposAsync()
        {
            try
            {                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var dbContext = _dbContextFactory.CreateDbContext();
                
                // ✅ TIMEOUT BALANCEADO: Suficiente tiempo para SSL handshake
                dbContext.Database.SetCommandTimeout(15);
                
                // Usar AsNoTracking para evitar devolver instancias ya rastreadas y con datos stale
                var equipos = await dbContext.EquiposInformaticos
                    .AsNoTracking()
                    .OrderBy(e => e.Codigo)
                    .ToListAsync(timeoutCts.Token);                // Se ha silenciado el logging de 'Estados únicos' para reducir ruido en los logs.
                var estadosUnicos = equipos.Select(e => e.Estado).Distinct().ToList();

                // Actualizar en hilo UI
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    ListaEquiposInformaticos.Clear();
                    foreach (var eq in equipos)
                        ListaEquiposInformaticos.Add(eq);

                    EquiposView?.Refresh();
                    // Recalcular estadísticas después de actualizar la lista
                    RecalcularEstadisticas();
                });
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == -1 || ex.Number == 26 || ex.Number == 10060)
            {
                _logger.LogInformation("[EquiposInformaticosViewModel] Sin conexión BD (Error {Number})", ex.Number);
                // No lanzar excepción - manejar silenciosamente
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[EquiposInformaticosViewModel] Timeout al cargar equipos");
                // No lanzar excepción - manejar silenciosamente  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al cargar equipos");
                // No romper UI si falla la carga
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Error al cargar equipos: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
        }

        [RelayCommand]
        private async Task VerDetalles(EquipoInformaticoEntity equipo)
        {
            if (equipo == null) return;

            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                dbContext.Database.SetCommandTimeout(15);

                // SIEMPRE recargar desde BD - no confiar en la instancia de la lista que puede estar desactualizada
                var detalle = await dbContext.EquiposInformaticos
                    .AsNoTracking() // Usar AsNoTracking para obtener datos frescos sin tracking
                    .Include(e => e.SlotsRam)
                    .Include(e => e.Discos)
                    .Include(e => e.Conexiones)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(e => e.Codigo == equipo.Codigo);

                if (detalle == null)
                {
                    System.Windows.MessageBox.Show("No se encontró el equipo en la base de datos.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }                // Crear ventana de detalles (necesitaría adaptar para usar factory en lugar de DbContext directo)
                var ventana = new GestLog.Modules.GestionEquiposInformaticos.Views.Equipos.DetallesEquipoInformaticoView(detalle, null);
                var owner = System.Windows.Application.Current?.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null;
                if (owner != null)
                {
                    ventana.Owner = owner;
                    ventana.ConfigurarParaVentanaPadre(owner);
                }
                
                // Mostrar ventana de detalles y esperar a que se cierre
                var result = ventana.ShowDialog();
                
                // CRÍTICO: Después de cerrar detalles, recargar la lista principal para reflejar cambios
                await CargarEquiposAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al obtener detalles del equipo");
                System.Windows.MessageBox.Show($"Error al obtener detalles del equipo: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanCrearEquipo))]
        private async Task AgregarEquipo()
        {
            var ventana = new AgregarEquipoInformaticoView();
            var resultado = ventana.ShowDialog();
            if (resultado == true)
            {
                // Recargar la lista para mostrar el nuevo equipo
                await CargarEquiposAsync();
            }
        }        [RelayCommand(CanExecute = nameof(CanExportarDatos))]
        private async Task ExportarEquipos()
        {
            // Detectar si hay filtros activos
            var hayFiltros = !string.IsNullOrWhiteSpace(FiltroEquipo) || ShowDadoDeBaja;
            
            // Obtener equipos a exportar
            var equiposAExportar = hayFiltros
                ? ListaEquiposInformaticos.Where(e => FiltrarEquipo(e)).ToList()
                : ListaEquiposInformaticos.ToList();

            if (equiposAExportar.Count == 0)
            {
                System.Windows.MessageBox.Show("No hay equipos para exportar.", "Sin Resultados", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var sufijo = hayFiltros ? "_Filtrados" : "";
            var dialog = new SaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Title = hayFiltros ? "Exportar equipos filtrados a Excel" : "Exportar equipos informáticos a Excel",
                FileName = $"EquiposInformaticos{sufijo}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Mostrar progreso
                    System.Windows.MessageBox.Show("Cargando detalles de equipos...", "Exportando", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    
                    // Cargar detalles de los equipos a exportar (RAM, Discos, Conexiones)
                    var equiposConDetalles = await CargarDetallesEquipos(equiposAExportar);
                    
                    ExportarEquiposAExcel(dialog.FileName, equiposConDetalles);
                    var mensaje = hayFiltros 
                        ? $"Se exportaron {equiposAExportar.Count} equipos (filtrados) a:\n{dialog.FileName}"
                        : $"Se exportaron {equiposAExportar.Count} equipos a:\n{dialog.FileName}";
                    System.Windows.MessageBox.Show(mensaje, "Exportación Completada", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al exportar equipos");
                    System.Windows.MessageBox.Show($"Error al exportar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Carga los detalles (RAM, Discos, Conexiones) para los equipos especificados
        /// </summary>
        private async Task<List<EquipoInformaticoEntity>> CargarDetallesEquipos(List<EquipoInformaticoEntity> equipos)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                dbContext.Database.SetCommandTimeout(15);

                var codigos = equipos.Select(e => e.Codigo).ToList();

                // Cargar equipos con todas sus relaciones
                var equiposConDetalles = await dbContext.EquiposInformaticos
                    .AsNoTracking()
                    .Include(e => e.SlotsRam)
                    .Include(e => e.Discos)
                    .Include(e => e.Conexiones)
                    // Evitar productorio cartesiano usando split queries
                    .AsSplitQuery()
                    .Where(e => codigos.Contains(e.Codigo))
                    .ToListAsync();

                return equiposConDetalles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al cargar detalles de equipos para exportación");
                throw;
            }
        }/// <summary>
        /// Exporta una lista de equipos a un archivo Excel con formato profesional
        /// Incluye hojas separadas para: Equipos, RAM, Discos, Conexiones
        /// </summary>
        private void ExportarEquiposAExcel(string rutaArchivo, IEnumerable<EquipoInformaticoEntity> equipos)
        {
            using var workbook = new XLWorkbook();
            var equiposList = equipos.ToList();

            // Hoja 1: Información principal de equipos
            ExportarHojaEquipos(workbook, equiposList);

            // Hoja 2: Detalles de RAM
            ExportarHojaRam(workbook, equiposList);

            // Hoja 3: Detalles de Discos
            ExportarHojaDiscos(workbook, equiposList);

            // Hoja 4: Conexiones de Red
            ExportarHojaConexiones(workbook, equiposList);

            // Guardar
            workbook.SaveAs(rutaArchivo);
        }

        /// <summary>
        /// Exporta información principal de equipos
        /// </summary>
        private void ExportarHojaEquipos(XLWorkbook workbook, List<EquipoInformaticoEntity> equipos)
        {
            var worksheet = workbook.Worksheets.Add("Equipos");

            var headers = new[]
            {
                "Código", "Nombre del Equipo", "Marca", "Modelo", "Usuario Asignado", "Sede", "Estado",
                "Procesador", "SO", "Serial", "AnyDesk", "Costo", "Fecha Compra", "Fecha Baja",
                "Observaciones", "Fecha Creación", "Fecha Modificación"
            };

            // Escribir headers
            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col);
                cell.Value = headers[col - 1];
                AplicarFormatoHeader(cell);
            }            // Escribir datos
            int row = 2;
            foreach (var equipo in equipos)
            {
                worksheet.Cell(row, 1).Value = equipo.Codigo ?? "";
                worksheet.Cell(row, 2).Value = equipo.NombreEquipo ?? "";
                worksheet.Cell(row, 3).Value = equipo.Marca ?? "";
                worksheet.Cell(row, 4).Value = equipo.Modelo ?? "";
                worksheet.Cell(row, 5).Value = equipo.UsuarioAsignado ?? "";
                worksheet.Cell(row, 6).Value = SepararPorMayusculas(equipo.Sede ?? "");
                worksheet.Cell(row, 7).Value = FormatearEstadoEquipo(equipo.Estado);
                worksheet.Cell(row, 8).Value = equipo.Procesador ?? "";
                worksheet.Cell(row, 9).Value = equipo.SO ?? "";
                worksheet.Cell(row, 10).Value = equipo.SerialNumber ?? "";
                worksheet.Cell(row, 11).Value = equipo.CodigoAnydesk ?? "";
                worksheet.Cell(row, 12).Value = equipo.Costo ?? (decimal?)null;
                worksheet.Cell(row, 13).Value = equipo.FechaCompra;
                worksheet.Cell(row, 14).Value = equipo.FechaBaja;

                // Limpiar observaciones: eliminar saltos de línea
                var observacionesLimpia = (equipo.Observaciones ?? "")
                    .Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
                worksheet.Cell(row, 15).Value = observacionesLimpia;

                worksheet.Cell(row, 16).Value = equipo.FechaCreacion;
                worksheet.Cell(row, 17).Value = equipo.FechaModificacion;

                // Color condicional para estado
                AplicarColorEstado(worksheet.Cell(row, 7), equipo.Estado);

                // Formato de fechas y números
                worksheet.Cell(row, 12).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                worksheet.Cell(row, 13).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Cell(row, 14).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Cell(row, 16).Style.DateFormat.Format = "dd/MM/yyyy hh:mm";
                worksheet.Cell(row, 17).Style.DateFormat.Format = "dd/MM/yyyy hh:mm";

                row++;
            }            worksheet.Columns().AdjustToContents();
            worksheet.Column(15).Width = 30;
            worksheet.SheetView.FreezeRows(1);

            // Agregar filtros automáticos en los encabezados
            if (equipos.Any())
            {
                worksheet.Range(1, 1, equipos.Count + 1, headers.Length).SetAutoFilter();
            }
        }

        /// <summary>
        /// Exporta detalles de RAM
        /// </summary>
        private void ExportarHojaRam(XLWorkbook workbook, List<EquipoInformaticoEntity> equipos)
        {
            var worksheet = workbook.Worksheets.Add("RAM");

            var headers = new[] { "Código Equipo", "Nombre Equipo", "Slot", "Capacidad (GB)", "Tipo Memoria", "Marca", "Frecuencia", "Ocupado", "Observaciones" };

            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col);
                cell.Value = headers[col - 1];
                AplicarFormatoHeader(cell);
            }

            int row = 2;
            foreach (var equipo in equipos)
            {
                foreach (var slot in equipo.SlotsRam)
                {
                    worksheet.Cell(row, 1).Value = equipo.Codigo ?? "";
                    worksheet.Cell(row, 2).Value = equipo.NombreEquipo ?? "";
                    worksheet.Cell(row, 3).Value = slot.NumeroSlot;
                    worksheet.Cell(row, 4).Value = slot.CapacidadGB;
                    worksheet.Cell(row, 5).Value = slot.TipoMemoria ?? "";
                    worksheet.Cell(row, 6).Value = slot.Marca ?? "";
                    worksheet.Cell(row, 7).Value = slot.Frecuencia ?? "";
                    worksheet.Cell(row, 8).Value = slot.Ocupado ? "Sí" : "No";
                    worksheet.Cell(row, 9).Value = (slot.Observaciones ?? "").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
                    row++;
                }
            }            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            // Agregar filtros automáticos en los encabezados
            if (row > 2)
            {
                worksheet.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
            }
        }        /// <summary>
        /// Exporta detalles de Discos
        /// </summary>
        private void ExportarHojaDiscos(XLWorkbook workbook, List<EquipoInformaticoEntity> equipos)
        {
            var worksheet = workbook.Worksheets.Add("Discos");

            var headers = new[] { "Código Equipo", "Nombre Equipo", "Número", "Tipo", "Capacidad (GB)", "Marca", "Modelo" };

            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col);
                cell.Value = headers[col - 1];
                AplicarFormatoHeader(cell);
            }

            int row = 2;
            foreach (var equipo in equipos)
            {
                foreach (var disco in equipo.Discos)
                {
                    worksheet.Cell(row, 1).Value = equipo.Codigo ?? "";
                    worksheet.Cell(row, 2).Value = equipo.NombreEquipo ?? "";
                    worksheet.Cell(row, 3).Value = disco.NumeroDisco;
                    worksheet.Cell(row, 4).Value = disco.Tipo ?? "";
                    worksheet.Cell(row, 5).Value = disco.CapacidadGB;
                    worksheet.Cell(row, 6).Value = disco.Marca ?? "";
                    worksheet.Cell(row, 7).Value = disco.Modelo ?? "";
                    row++;
                }
            }            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            // Agregar filtros automáticos en los encabezados
            if (row > 2)
            {
                worksheet.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
            }
        }

        /// <summary>
        /// Exporta detalles de Conexiones de Red
        /// </summary>
        private void ExportarHojaConexiones(XLWorkbook workbook, List<EquipoInformaticoEntity> equipos)
        {
            var worksheet = workbook.Worksheets.Add("Conexiones");

            var headers = new[] { "Código Equipo", "Nombre Equipo", "Adaptador", "IP IPv4", "MAC", "Máscara Subred", "Gateway" };

            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col);
                cell.Value = headers[col - 1];
                AplicarFormatoHeader(cell);
            }

            int row = 2;
            foreach (var equipo in equipos)
            {
                foreach (var conexion in equipo.Conexiones)
                {
                    worksheet.Cell(row, 1).Value = equipo.Codigo ?? "";
                    worksheet.Cell(row, 2).Value = equipo.NombreEquipo ?? "";
                    worksheet.Cell(row, 3).Value = conexion.Adaptador ?? "";
                    worksheet.Cell(row, 4).Value = conexion.DireccionIPv4 ?? "";
                    worksheet.Cell(row, 5).Value = conexion.DireccionMAC ?? "";
                    worksheet.Cell(row, 6).Value = conexion.MascaraSubred ?? "";
                    worksheet.Cell(row, 7).Value = conexion.PuertoEnlace ?? "";
                    row++;
                }
            }            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            // Agregar filtros automáticos en los encabezados
            if (row > 2)
            {
                worksheet.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
            }
        }        /// <summary>
        /// Aplica formato estándar a headers
        /// </summary>
        private void AplicarFormatoHeader(IXLCell cell)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(17, 137, 56);
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }        /// <summary>
        /// Aplica color de fondo a una celda según el estado del equipo
        /// </summary>
        private void AplicarColorEstado(IXLCell cell, string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado)) return;

            var estadoNormalizado = estado.Trim().Replace(" ", "").ToLowerInvariant();

            if (estadoNormalizado.Contains("inactivo"))
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(107, 114, 128); // Gris #6B7280
            else if (estadoNormalizado.Contains("dadodebaja"))
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(239, 68, 68); // Rojo #EF4444
            else if (estadoNormalizado.Contains("enmantenimiento"))
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 158, 11); // Naranja #F59E0B
            else if (estadoNormalizado.Contains("danado"))
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 38, 38); // Rojo oscuro #DC2626
            else if (estadoNormalizado.Contains("enreparacion"))
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(251, 191, 36); // Amarillo #FBD136
            else if (estadoNormalizado.Contains("enuso") || estadoNormalizado.Contains("activo"))
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(43, 142, 63); // Verde #2B8E3F
            else
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(39, 174, 96); // Verde por defecto

            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        /// <summary>
        /// Formatea el estado del equipo para mostrar correctamente en Excel
        /// </summary>
        private string FormatearEstadoEquipo(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                return "";

            var estadoLower = estado.Replace(" ", "").ToLowerInvariant();

            return estadoLower switch
            {
                "enuso" or "activo" => "En Uso",
                "enmantenimiento" => "En Mantenimiento",
                "enreparacion" => "En Reparación",
                "inactivo" => "Inactivo",
                "danado" => "Dañado",
                "dadodebaja" => "Dado de Baja",
                _ => estado // Devolver original si no coincide
            };
        }

        /// <summary>
        /// Separa un texto por mayúsculas e inserta " - " entre partes
        /// </summary>
        private string SepararPorMayusculas(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return texto;

            var resultado = new System.Text.StringBuilder();
            bool esFirstChar = true;

            foreach (char c in texto)
            {
                if (char.IsUpper(c) && !esFirstChar)
                {
                    resultado.Append(" - ");
                }
                resultado.Append(c);
                esFirstChar = false;
            }

            return resultado.ToString();
        }

        [RelayCommand(CanExecute = nameof(CanCrearEquipo))]
        private void ImportarEquipos()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos CSV (*.csv)|*.csv|Archivos Excel (*.xlsx)|*.xlsx|Todos los archivos (*.*)|*.*",
                Title = "Importar equipos informáticos"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Implementación simplificada de importación
                    // TODO: Parsear e insertar registros según formato (CSV/XLSX)
                    System.Windows.MessageBox.Show($"Archivo seleccionado: {dialog.FileName}\nFuncionalidad de importación no implementada.", "Importar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                    // Recargar lista después de una importación hipotética
                    _ = CargarEquiposAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EquiposInformaticosViewModel] Error al importar equipos");
                    System.Windows.MessageBox.Show($"Error al importar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        // Recalcula los contadores por estado a partir de la colección actual
        private void RecalcularEstadisticas()
        {
            try
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var list = ListaEquiposInformaticos ?? new System.Collections.ObjectModel.ObservableCollection<EquipoInformaticoEntity>();
                    EquiposActivos = list.Count(e => {
                        var s = (e.Estado ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
                        return s.Contains("enuso") || s.Contains("activo");
                    });
                    EquiposEnMantenimiento = list.Count(e => ((e.Estado ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant().Contains("enmantenimiento")));
                    EquiposEnReparacion = list.Count(e => ((e.Estado ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant().Contains("enreparacion")));
                    EquiposInactivos = list.Count(e => ((e.Estado ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant().Contains("inactivo")));
                    EquiposDadosBaja = list.Count(e => {
                        var s = (e.Estado ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
                        return s.Contains("dadodebaja");
                    });
                });
            }
            catch
            {
                // No interrumpir la UI por fallos al recalcular estadísticas
            }
        }

        /// <summary>
        /// Override Dispose si hay recursos adicionales que limpiar
        /// </summary>
        public override void Dispose()
        {
            try
            {
                // Desuscribirse de eventos específicos
                if (_currentUserService != null)
                {
                    _currentUserService.CurrentUserChanged -= OnCurrentUserChanged;
                }
                
                // Limpiar vista
                EquiposView = null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[EquiposInformaticosViewModel] Error durante dispose específico");
            }
            finally
            {
                // Llamar al dispose de la clase base
                base.Dispose();
            }
        }
    }
}

