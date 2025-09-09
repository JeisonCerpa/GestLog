using System.Collections.ObjectModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.DatabaseConnection;
using GestLog.Views.Tools.GestionEquipos;
using System.Windows;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Win32;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public partial class EquiposInformaticosViewModel : ObservableObject
    {
        private readonly GestLogDbContext _db;
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
        private ICollectionView? equiposView;

        public EquiposInformaticosViewModel(GestLogDbContext db, ICurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
            EquiposView = CollectionViewSource.GetDefaultView(ListaEquiposInformaticos);
            if (EquiposView != null)
                EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
            CargarEquipos();
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

        private bool FiltrarEquipo(object obj)
        {
            if (obj is not EquipoInformaticoEntity eq) return false;
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

        private void CargarEquipos()
        {
            var equipos = _db.EquiposInformaticos.ToList();
            ListaEquiposInformaticos.Clear();
            foreach (var eq in equipos)
                ListaEquiposInformaticos.Add(eq);
            EquiposView?.Refresh();
        }

        [RelayCommand]
        private void VerDetalles(EquipoInformaticoEntity equipo)
        {
            if (equipo == null) return;

            // Obtener la entidad y cargar explícitamente las colecciones relacionadas
            var detalle = _db.EquiposInformaticos.FirstOrDefault(e => e.Codigo == equipo.Codigo);
            if (detalle != null)
            {
                _db.Entry(detalle).Collection(e => e.SlotsRam).Load();
                _db.Entry(detalle).Collection(e => e.Discos).Load();
            }

            // Use detalle if available, otherwise fallback to the original equipo parameter
            var equipoParaDetalles = detalle ?? equipo;
            var ventana = new GestLog.Views.Tools.GestionEquipos.DetallesEquipoInformaticoView(equipoParaDetalles);
            var owner = System.Windows.Application.Current?.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null;
            if (owner != null) ventana.Owner = owner;
            ventana.ShowDialog();
        }        

        [RelayCommand(CanExecute = nameof(CanCrearEquipo))]
        private void AgregarEquipo()
        {
            var ventana = new AgregarEquipoInformaticoView();
            var resultado = ventana.ShowDialog();
            if (resultado == true)
            {
                CargarEquipos();
            }
        }

        [RelayCommand(CanExecute = nameof(CanCrearEquipo))]
        private void ImportarEquipos()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Title = "Importar equipos informáticos desde Excel"
            };
            if (dialog.ShowDialog() != true)
                return;

            var importErrors = new List<string>();
            int imported = 0;

            try
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook(dialog.FileName);
                var ws = workbook.Worksheet(1);
                var headerRow = ws.Row(1);
                var headers = headerRow.CellsUsed().Select(c => c.GetString().Trim()).ToList();

                // Función para buscar columna por una lista de posibles encabezados (insensible a mayúsculas)
                int Col(params string[] matches)
                {
                    for (int i = 0; i < headers.Count; i++)
                    {
                        var h = headers[i];
                        foreach (var m in matches)
                        {
                            if (h.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0)
                                return i + 1; // ClosedXML usa 1-based
                        }
                    }
                    return 0;
                }

                int idxCodigo = Col("código", "codigo", "code");
                int idxUsuarioAsignado = Col("usuario asignado", "usuario", "asignado");
                int idxNombreEquipo = Col("nombre equipo", "nombre", "equipo");
                int idxSede = Col("sede", "sucursal", "oficina");
                int idxCosto = Col("costo", "valor", "price", "precio");
                int idxFechaCompra = Col("fecha compra", "fecha de compra", "fecha compra", "fecha_compra", "purchase");
                int idxEstado = Col("estado", "state", "estatus");
                int idxCodigoAnydesk = Col("anydesk", "código anydesk", "codigo anydesk");
                int idxMarca = Col("marca");
                int idxModelo = Col("modelo");
                int idxSO = Col("so", "sistema operativo", "sistema");
                int idxSerialNumber = Col("serial", "serial number", "nro serial");
                int idxProcesador = Col("procesador", "cpu", "intel", "amd");
                int idxObservaciones = Col("observaciones", "observacion", "obs", "nota");
                int idxFechaBaja = Col("fecha baja", "fechabaja", "fecha_de_baja");
                int idxFechaCreacion = Col("fecha creación", "fecha creacion", "fecha_creacion", "fecha de creación");
                int idxFechaModificacion = Col("fecha modificación", "fecha modificacion", "fecha_modificacion", "fecha de modificacion");

                // Helpers de parseo defensivo
                decimal? ParseDecimalSafe(string raw)
                {
                    if (string.IsNullOrWhiteSpace(raw)) return null;
                    raw = raw.Trim();
                    // Eliminar caracteres invisibles y espacios
                    raw = raw.Replace("\uFEFF", "").Replace(" ", "");
                    var styles = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
                    if (decimal.TryParse(raw, styles, CultureInfo.CurrentCulture, out var val)) return val;
                    if (decimal.TryParse(raw, styles, CultureInfo.InvariantCulture, out val)) return val;
                    // Heurística: si contiene "," y "." asumir que "." es separador de miles
                    if (raw.Count(c => c == ',') > 0 && raw.Count(c => c == '.') > 0)
                    {
                        var alt = raw.Replace(".", "").Replace(",", ".");
                        if (decimal.TryParse(alt, NumberStyles.Number, CultureInfo.InvariantCulture, out val)) return val;
                    }
                    // Si sólo tiene comas como separador decimal
                    if (raw.Contains(',') && !raw.Contains('.'))
                    {
                        var alt = raw.Replace(',', '.');
                        if (decimal.TryParse(alt, NumberStyles.Number, CultureInfo.InvariantCulture, out val)) return val;
                    }
                    // Si falla, devolver null y registrar advertencia fuera
                    return null;
                }

                DateTime? ParseDateSafe(string raw)
                {
                    if (string.IsNullOrWhiteSpace(raw)) return null;
                    raw = raw.Trim().Replace("\uFEFF", "");
                    var formatos = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "dd-MM-yyyy" };
                    if (DateTime.TryParseExact(raw, formatos, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt)) return dt;
                    if (DateTime.TryParseExact(raw, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) return dt;
                    if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt)) return dt;
                    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt)) return dt;
                    return null;
                }

                foreach (var row in ws.RowsUsed().Skip(1)) // Saltar encabezado
                {
                    try
                    {
                        var rowNum = row.RowNumber();
                        string codigo = idxCodigo > 0 && !row.Cell(idxCodigo).IsEmpty() ? row.Cell(idxCodigo).GetString().Trim() : string.Empty;
                        if (string.IsNullOrWhiteSpace(codigo))
                        {
                            importErrors.Add($"Fila {rowNum}: Código vacío - fila ignorada");
                            continue;
                        }

                        string usuario = idxUsuarioAsignado > 0 && !row.Cell(idxUsuarioAsignado).IsEmpty() ? row.Cell(idxUsuarioAsignado).GetString().Trim() : string.Empty;
                        string nombre = idxNombreEquipo > 0 && !row.Cell(idxNombreEquipo).IsEmpty() ? row.Cell(idxNombreEquipo).GetString().Trim() : string.Empty;
                        string sede = idxSede > 0 && !row.Cell(idxSede).IsEmpty() ? row.Cell(idxSede).GetString().Trim() : string.Empty;

                        string rawCosto = idxCosto > 0 && !row.Cell(idxCosto).IsEmpty() ? row.Cell(idxCosto).GetString().Trim() : string.Empty;
                        // Si la celda es numérica ClosedXML puede tener .Value as double; capturamos también
                        if (idxCosto > 0 && row.Cell(idxCosto).DataType == ClosedXML.Excel.XLDataType.Number)
                        {
                            try
                            {
                                var v = row.Cell(idxCosto).GetDouble();
                                rawCosto = v.ToString(CultureInfo.InvariantCulture);
                            }
                            catch { /* ignorar y dejar rawCosto si falla */ }
                        }

                        decimal? costo = ParseDecimalSafe(rawCosto);
                        if (idxCosto > 0 && rawCosto.Length > 0 && costo == null)
                            importErrors.Add($"Fila {row.RowNumber()}: no se pudo parsear Costo ('{rawCosto}').");

                        string rawFechaCompra = idxFechaCompra > 0 && !row.Cell(idxFechaCompra).IsEmpty() ? row.Cell(idxFechaCompra).GetString().Trim() : string.Empty;
                        if (idxFechaCompra > 0 && row.Cell(idxFechaCompra).DataType == ClosedXML.Excel.XLDataType.DateTime)
                        {
                            try { rawFechaCompra = row.Cell(idxFechaCompra).GetDateTime().ToString("dd/MM/yyyy"); } catch { }
                        }
                        DateTime? fechaCompra = ParseDateSafe(rawFechaCompra);
                        if (idxFechaCompra > 0 && !string.IsNullOrWhiteSpace(rawFechaCompra) && fechaCompra == null)
                            importErrors.Add($"Fila {row.RowNumber()}: no se pudo parsear Fecha Compra ('{rawFechaCompra}').");

                        string estado = idxEstado > 0 && !row.Cell(idxEstado).IsEmpty() ? row.Cell(idxEstado).GetString().Trim() : string.Empty;
                        string anydesk = idxCodigoAnydesk > 0 && !row.Cell(idxCodigoAnydesk).IsEmpty() ? row.Cell(idxCodigoAnydesk).GetString().Trim() : string.Empty;
                        string marca = idxMarca > 0 && !row.Cell(idxMarca).IsEmpty() ? row.Cell(idxMarca).GetString().Trim() : string.Empty;
                        string modelo = idxModelo > 0 && !row.Cell(idxModelo).IsEmpty() ? row.Cell(idxModelo).GetString().Trim() : string.Empty;
                        string so = idxSO > 0 && !row.Cell(idxSO).IsEmpty() ? row.Cell(idxSO).GetString().Trim() : string.Empty;
                        string serial = idxSerialNumber > 0 && !row.Cell(idxSerialNumber).IsEmpty() ? row.Cell(idxSerialNumber).GetString().Trim() : string.Empty;
                        string procesador = idxProcesador > 0 && !row.Cell(idxProcesador).IsEmpty() ? row.Cell(idxProcesador).GetString().Trim() : string.Empty;
                        string observ = idxObservaciones > 0 && !row.Cell(idxObservaciones).IsEmpty() ? row.Cell(idxObservaciones).GetString().Trim() : string.Empty;

                        string rawFechaBaja = idxFechaBaja > 0 && !row.Cell(idxFechaBaja).IsEmpty() ? row.Cell(idxFechaBaja).GetString().Trim() : string.Empty;
                        if (idxFechaBaja > 0 && row.Cell(idxFechaBaja).DataType == ClosedXML.Excel.XLDataType.DateTime)
                        {
                            try { rawFechaBaja = row.Cell(idxFechaBaja).GetDateTime().ToString("dd/MM/yyyy"); } catch { }
                        }
                        DateTime? fechaBaja = ParseDateSafe(rawFechaBaja);

                        string rawFechaCreacion = idxFechaCreacion > 0 && !row.Cell(idxFechaCreacion).IsEmpty() ? row.Cell(idxFechaCreacion).GetString().Trim() : string.Empty;
                        if (idxFechaCreacion > 0 && row.Cell(idxFechaCreacion).DataType == ClosedXML.Excel.XLDataType.DateTime)
                        {
                            try { rawFechaCreacion = row.Cell(idxFechaCreacion).GetDateTime().ToString("dd/MM/yyyy"); } catch { }
                        }
                        DateTime fechaCreacion = ParseDateSafe(rawFechaCreacion) ?? DateTime.Now;

                        string rawFechaMod = idxFechaModificacion > 0 && !row.Cell(idxFechaModificacion).IsEmpty() ? row.Cell(idxFechaModificacion).GetString().Trim() : string.Empty;
                        if (idxFechaModificacion > 0 && row.Cell(idxFechaModificacion).DataType == ClosedXML.Excel.XLDataType.DateTime)
                        {
                            try { rawFechaMod = row.Cell(idxFechaModificacion).GetDateTime().ToString("dd/MM/yyyy"); } catch { }
                        }
                        DateTime? fechaMod = ParseDateSafe(rawFechaMod);

                        // Crear entidad
                        var eq = new EquipoInformaticoEntity
                        {
                            // Evitar asignar null a la propiedad non-nullable Codigo
                            Codigo = string.IsNullOrWhiteSpace(codigo) ? string.Empty : codigo,
                            UsuarioAsignado = usuario,
                            NombreEquipo = nombre,
                            Sede = sede,
                            Costo = costo,
                            FechaCompra = fechaCompra,
                            Estado = estado,
                            CodigoAnydesk = anydesk,
                            Marca = marca,
                            Modelo = modelo,
                            SO = so,
                            SerialNumber = serial,
                            Procesador = procesador,
                            Observaciones = observ,
                            FechaBaja = fechaBaja,
                            FechaCreacion = fechaCreacion,
                            FechaModificacion = fechaMod
                        };

                        // Guardar: si ya existe, omitimos (podrías cambiar a actualización si se desea)
                        if (!_db.EquiposInformaticos.Any(e => e.Codigo == eq.Codigo))
                        {
                            _db.EquiposInformaticos.Add(eq);
                            imported++;
                        }
                        else
                        {
                            importErrors.Add($"Fila {rowNum}: Código '{codigo}' ya existe - fila ignorada");
                        }
                    }
                    catch (Exception exRow)
                    {
                        importErrors.Add($"Fila {row.RowNumber()}: excepción: {exRow.Message}");
                    }
                }

                _db.SaveChanges();

                var summary = $"Se importaron {imported} equipos.";
                if (importErrors.Any())
                {
                    summary += $" Se encontraron {importErrors.Count} advertencias/errores.\n" + string.Join("\n", importErrors.Take(50));
                    System.Windows.MessageBox.Show(summary, "Importación completada con advertencias", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    System.Windows.MessageBox.Show(summary, "Importación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CargarEquipos();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al importar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExportarDatos))]
        private void ExportarEquipos()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Title = "Exportar equipos informáticos a Excel",
                FileName = $"EquiposInformaticos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using var workbook = new ClosedXML.Excel.XLWorkbook();
                    // Hoja principal: Equipos
                    var ws = workbook.Worksheets.Add("EquiposInformaticos");
                    ws.Cell(1, 1).Value = "Código";
                    ws.Cell(1, 2).Value = "Usuario Asignado";
                    ws.Cell(1, 3).Value = "Nombre Equipo";
                    ws.Cell(1, 4).Value = "Costo";
                    ws.Cell(1, 5).Value = "Fecha Compra";
                    ws.Cell(1, 6).Value = "Estado";
                    ws.Cell(1, 7).Value = "Sede";
                    ws.Cell(1, 8).Value = "Código Anydesk";
                    ws.Cell(1, 9).Value = "Modelo";
                    ws.Cell(1, 10).Value = "SO";
                    ws.Cell(1, 11).Value = "Marca";
                    ws.Cell(1, 12).Value = "Serial Number";
                    ws.Cell(1, 13).Value = "Procesador";
                    ws.Cell(1, 14).Value = "Observaciones";
                    ws.Cell(1, 15).Value = "Fecha Baja";
                    ws.Cell(1, 16).Value = "Fecha Creación";
                    ws.Cell(1, 17).Value = "Fecha Modificación";
                    int row = 2;
                    foreach (var eq in ListaEquiposInformaticos)
                    {
                        ws.Cell(row, 1).Value = eq.Codigo ?? "";
                        ws.Cell(row, 2).Value = eq.UsuarioAsignado ?? "";
                        ws.Cell(row, 3).Value = eq.NombreEquipo ?? "";
                        ws.Cell(row, 4).Value = eq.Costo ?? 0;
                        ws.Cell(row, 5).Value = eq.FechaCompra?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(row, 6).Value = eq.Estado ?? "";
                        ws.Cell(row, 7).Value = eq.Sede ?? "";
                        ws.Cell(row, 8).Value = eq.CodigoAnydesk ?? "";
                        ws.Cell(row, 9).Value = eq.Modelo ?? "";
                        ws.Cell(row, 10).Value = eq.SO ?? "";
                        ws.Cell(row, 11).Value = eq.Marca ?? "";
                        ws.Cell(row, 12).Value = eq.SerialNumber ?? "";
                        ws.Cell(row, 13).Value = eq.Procesador ?? "";
                        ws.Cell(row, 14).Value = eq.Observaciones ?? "";
                        ws.Cell(row, 15).Value = eq.FechaBaja?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(row, 16).Value = eq.FechaCreacion.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(row, 17).Value = eq.FechaModificacion?.ToString("dd/MM/yyyy") ?? "";
                        row++;
                    }
                    ws.Columns().AdjustToContents();

                    // Hoja RAM
                    var wsRam = workbook.Worksheets.Add("RAM");
                    wsRam.Cell(1, 1).Value = "Código Equipo";
                    wsRam.Cell(1, 2).Value = "Slot";
                    wsRam.Cell(1, 3).Value = "Capacidad (GB)";
                    wsRam.Cell(1, 4).Value = "Tipo Memoria";
                    wsRam.Cell(1, 5).Value = "Marca";
                    wsRam.Cell(1, 6).Value = "Frecuencia";
                    wsRam.Cell(1, 7).Value = "Ocupado";
                    wsRam.Cell(1, 8).Value = "Observaciones";
                    int rowRam = 2;
                    foreach (var eq in ListaEquiposInformaticos)
                    {
                        if (eq.SlotsRam != null)
                        {
                            foreach (var slot in eq.SlotsRam)
                            {
                                wsRam.Cell(rowRam, 1).Value = eq.Codigo ?? "";
                                wsRam.Cell(rowRam, 2).Value = slot.NumeroSlot;
                                wsRam.Cell(rowRam, 3).Value = slot.CapacidadGB;
                                wsRam.Cell(rowRam, 4).Value = slot.TipoMemoria ?? "";
                                wsRam.Cell(rowRam, 5).Value = slot.Marca ?? "";
                                wsRam.Cell(rowRam, 6).Value = slot.Frecuencia ?? "";
                                wsRam.Cell(rowRam, 7).Value = slot.Ocupado ? "Sí" : "No";
                                wsRam.Cell(rowRam, 8).Value = slot.Observaciones ?? "";
                                rowRam++;
                            }
                        }
                    }
                    wsRam.Columns().AdjustToContents();

                    // Hoja Discos
                    var wsDiscos = workbook.Worksheets.Add("Discos");
                    wsDiscos.Cell(1, 1).Value = "Código Equipo";
                    wsDiscos.Cell(1, 2).Value = "N° Disco";
                    wsDiscos.Cell(1, 3).Value = "Tipo";
                    wsDiscos.Cell(1, 4).Value = "Capacidad (GB)";
                    wsDiscos.Cell(1, 5).Value = "Marca";
                    wsDiscos.Cell(1, 6).Value = "Modelo";
                    int rowDisco = 2;
                    foreach (var eq in ListaEquiposInformaticos)
                    {
                        if (eq.Discos != null)
                        {
                            foreach (var disco in eq.Discos)
                            {
                                wsDiscos.Cell(rowDisco, 1).Value = eq.Codigo ?? "";
                                wsDiscos.Cell(rowDisco, 2).Value = disco.NumeroDisco;
                                wsDiscos.Cell(rowDisco, 3).Value = disco.Tipo ?? "";
                                wsDiscos.Cell(rowDisco, 4).Value = disco.CapacidadGB;
                                wsDiscos.Cell(rowDisco, 5).Value = disco.Marca ?? "";
                                wsDiscos.Cell(rowDisco, 6).Value = disco.Modelo ?? "";
                                rowDisco++;
                            }
                        }
                    }
                    wsDiscos.Columns().AdjustToContents();

                    workbook.SaveAs(dialog.FileName);
                    System.Windows.MessageBox.Show($"Equipos y detalles exportados correctamente a:\n{dialog.FileName}", "Exportación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
