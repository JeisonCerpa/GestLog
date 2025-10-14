using System.Windows;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.ViewModels.Tools.GestionEquipos;
using System.Linq;
using System.Globalization;
using GestLog.Modules.DatabaseConnection; // agregado para poder recibir GestLogDbContext
using Microsoft.Extensions.DependencyInjection; // ✅ MIGRADO: Para resolver dependencias

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class DetallesEquipoInformaticoView : Window
    {
        // Guardar referencia al DbContext pasado por el llamador para reutilizarlo en refrescos
        private readonly GestLogDbContext? _db;

        // Constructor antiguo preservado y redirigido a la nueva sobrecarga
        public DetallesEquipoInformaticoView(EquipoInformaticoEntity equipo) : this(equipo, null)
        {
        }        // Nueva sobrecarga que recibe el DbContext (nullable) y lo pasa al ViewModel
        public DetallesEquipoInformaticoView(EquipoInformaticoEntity equipo, GestLogDbContext? db)
        {
            InitializeComponent();

            // Asegurar que esta ventana se abra como modal sobre la ventana principal y no aparezca en la barra de tareas
            try
            {
                // Asignar el Owner para centrar respecto al padre y bloquear la interacción
                this.Owner = System.Windows.Application.Current?.MainWindow;
                this.ShowInTaskbar = false;
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            catch
            {
                // No crítico si no existe Application.Current o MainWindow en algunos escenarios de test
            }

            // Registrar Loaded para ajustar el tamaño/posición del overlay para que cubra totalmente el Owner
            this.Loaded += DetallesEquipoInformaticoView_Loaded;

            // Pasar el DbContext al ViewModel para que realice persistencia cuando corresponda
            _db = db;
            
            // ✅ MIGRADO: Resolver dependencias para DatabaseAwareViewModel
            // Resolver el Application actual de forma segura
            var app = System.Windows.Application.Current as App;
            var serviceProvider = app?.ServiceProvider;
            if (serviceProvider == null)
            {
                // Intentar fallback simple: no inicializar dependencias si no hay ServiceProvider
                return;
            }
            var dbContextFactory = serviceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<GestLogDbContext>>();
            var databaseService = serviceProvider.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
            var logger = serviceProvider.GetRequiredService<GestLog.Services.Core.Logging.IGestLogLogger>();
            var seguimientoService = serviceProvider.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IGestionEquiposInformaticosSeguimientoCronogramaService>();
            
            DataContext = new DetallesEquipoInformaticoViewModel(equipo, dbContextFactory, databaseService, logger, seguimientoService);
        }

        // Nuevo manejador para ajustar el overlay al Owner y seguirlo si se mueve/redimensiona
        private void DetallesEquipoInformaticoView_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (this.Owner != null)
                {
                    // Ajustar la ventana al tamaño y posición del Owner para que el overlay cubra toda el área del padre
                    this.Left = this.Owner.Left;
                    this.Top = this.Owner.Top;
                    this.Width = this.Owner.ActualWidth;
                    this.Height = this.Owner.ActualHeight;

                    // Si el Owner se mueve/redimensiona, mantener el overlay sincronizado
                    this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                    this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
                }
            }
            catch
            {
                // No crítico
            }
        }

        private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
        {
            if (this.Owner == null) return;
            // Dispatcher por si el evento viene de otro hilo de UI
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    this.Left = this.Owner.Left;
                    this.Top = this.Owner.Top;
                    this.Width = this.Owner.ActualWidth;
                    this.Height = this.Owner.ActualHeight;
                }
                catch { }
            });
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Evitar que el click dentro del panel propague y cierre el overlay
            e.Handled = true;
        }

        private async void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Abrir la ventana de AgregarEquipoInformaticoView y precargar datos para edición
                var editarWindow = new AgregarEquipoInformaticoView();
                if (editarWindow.DataContext is GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel vm)
                {
                    string? usuarioAsignadoOriginal = null;
                    // Mapear los campos principales desde el ViewModel de Detalles (usar una única variable `det`)
                    if (this.DataContext is GestLog.ViewModels.Tools.GestionEquipos.DetallesEquipoInformaticoViewModel det)
                    {
                        vm.Codigo = det.Codigo;
                        vm.NombreEquipo = det.NombreEquipo ?? string.Empty;
                        vm.Marca = det.Marca ?? string.Empty;
                        vm.Modelo = det.Modelo ?? string.Empty;
                        vm.Procesador = det.Procesador ?? string.Empty;
                        vm.So = det.SO ?? string.Empty;
                        vm.SerialNumber = det.SerialNumber ?? string.Empty;
                        vm.CodigoAnydesk = det.CodigoAnydesk ?? string.Empty;
                        vm.Observaciones = det.Observaciones ?? string.Empty;
                        vm.Costo = det.Costo;
                        vm.FechaCompra = det.FechaCompra;

                        // Marcar modo edición y guardar código original para identificación en BD
                        vm.IsEditing = true;
                        vm.OriginalCodigo = det.Codigo;                        // Cargar listas de RAM y discos
                        vm.ListaRam = new System.Collections.ObjectModel.ObservableCollection<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity>(det.SlotsRam);
                        vm.ListaDiscos = new System.Collections.ObjectModel.ObservableCollection<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity>(det.Discos);
                        vm.ListaConexiones = new System.Collections.ObjectModel.ObservableCollection<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.ConexionEntity>(det.Conexiones);

                        // Transferir Estado y Sede al VM de edición antes de InicializarAsync para preservar la selección en el ComboBox Estado
                        try
                        {
                            vm.Estado = string.IsNullOrWhiteSpace(det.Estado) ? string.Empty : det.Estado;
                        }
                        catch { /* no crítico */ }

                        try
                        {
                            vm.Sede = string.IsNullOrWhiteSpace(det.Sede) ? vm.Sede : det.Sede;
                        }
                        catch { /* no crítico */ }

                        // Guardar el nombre del usuario asignado para usarlo una vez inicializado el VM de edición
                        usuarioAsignadoOriginal = det.UsuarioAsignado;
                    }

                    // Inicializar VM (carga de personas u otros recursos) antes de mostrar la ventana
                    try { await vm.InicializarAsync(); } catch { /* no crítico si falla aquí */ }

                    // Seleccionar la persona asignada usando el nombre capturado (si aplica)
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(usuarioAsignadoOriginal))
                        {
                            // Si no hay lista de personas cargada, dejar el texto en el filtro para que el usuario lo vea
                            if (vm.PersonasDisponibles == null || !vm.PersonasDisponibles.Any())
                            {
                                vm.FiltroPersonaAsignada = usuarioAsignadoOriginal.Trim();
                            }
                            else
                            {
                                // Normalizar función para comparar sin acentos ni mayúsculas
                                static string NormalizeString(string s)
                                {
                                    if (string.IsNullOrWhiteSpace(s))
                                        return string.Empty;
                                    var normalized = s.Normalize(System.Text.NormalizationForm.FormD);
                                    var chars = normalized.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
                                    return new string(chars).Normalize(System.Text.NormalizationForm.FormC).Trim().ToLowerInvariant();
                                }

                                var objetivo = NormalizeString(usuarioAsignadoOriginal);

                                // Intentos de emparejamiento: exacto normalizado, contains, y como último recurso dejar el filtro con el texto
                                var persona = vm.PersonasDisponibles.FirstOrDefault(p => NormalizeString(p.NombreCompleto) == objetivo);
                                if (persona == null)
                                    persona = vm.PersonasDisponibles.FirstOrDefault(p => NormalizeString(p.NombreCompleto).Contains(objetivo) || objetivo.Contains(NormalizeString(p.NombreCompleto)));

                                if (persona != null)
                                    vm.PersonaAsignada = persona;
                                else
                                    vm.FiltroPersonaAsignada = usuarioAsignadoOriginal.Trim();
                            }
                        }
                    }
                    catch { /* No crítico */ }
                }

                var result = editarWindow.ShowDialog();
                // Si se guardó (DialogResult == true), reconstruir la vista de detalles desde el VM de edición (no usamos reflexión para obtener DbContext)
                if (result == true)
                {
                    try
                    {
                        // Determinar código del equipo (puede haber cambiado durante la edición)
                        string? codigoParaCargar = null;
                        if (editarWindow.DataContext is GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel vm2)
                            codigoParaCargar = vm2.Codigo;

                        // Fallback: reconstruir entidad desde el VM de edición (si existe vm2)
                        if (editarWindow.DataContext is GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel vmFallback)
                        {                            var equipoReconstruido = new EquipoInformaticoEntity
                            {
                                Codigo = vmFallback.Codigo,
                                UsuarioAsignado = vmFallback.PersonaAsignada?.NombreCompleto ?? string.Empty,
                                NombreEquipo = vmFallback.NombreEquipo,
                                Sede = vmFallback.Sede,
                                Marca = vmFallback.Marca,
                                Modelo = vmFallback.Modelo,
                                Procesador = vmFallback.Procesador,
                                SO = vmFallback.So,
                                SerialNumber = vmFallback.SerialNumber,
                                Observaciones = vmFallback.Observaciones,
                                FechaCreacion = DateTime.Now,
                                FechaCompra = vmFallback.FechaCompra,
                                Costo = vmFallback.Costo,
                                CodigoAnydesk = vmFallback.CodigoAnydesk,
                                Estado = vmFallback.Estado,
                                SlotsRam = vmFallback.ListaRam?.ToList() ?? new System.Collections.Generic.List<Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity>(),
                                Discos = vmFallback.ListaDiscos?.ToList() ?? new System.Collections.Generic.List<Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity>(),
                                Conexiones = vmFallback.ListaConexiones?.ToList() ?? new System.Collections.Generic.List<Modules.GestionEquiposInformaticos.Models.Entities.ConexionEntity>()
                            };                            // Asignar nuevo DataContext usando el equipo reconstruido (reutilizando el DbContext si existe)
                            // ✅ MIGRADO: Resolver dependencias para DatabaseAwareViewModel
                            // Resolver el Application actual de forma segura
                            var app = System.Windows.Application.Current as App;
                            var serviceProvider = app?.ServiceProvider;
                            if (serviceProvider == null)
                            {
                                // Intentar fallback simple: no inicializar dependencias si no hay ServiceProvider
                                return;
                            }
                            var dbContextFactory = serviceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<GestLogDbContext>>();
                            var databaseService = serviceProvider.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                            var logger = serviceProvider.GetRequiredService<GestLog.Services.Core.Logging.IGestLogLogger>();
                            var seguimientoService = serviceProvider.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IGestionEquiposInformaticosSeguimientoCronogramaService>();
                            
                            this.DataContext = new DetallesEquipoInformaticoViewModel(equipoReconstruido, dbContextFactory, databaseService, logger, seguimientoService);
                        }
                    }
                    catch
                    {
                        // No crítico: si falla el refresco, mantenemos la vista actual
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al abrir el editor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDarDeBaja_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(this.DataContext is GestLog.ViewModels.Tools.GestionEquipos.DetallesEquipoInformaticoViewModel vm))
                {
                    System.Windows.MessageBox.Show("No se pudo obtener la información del equipo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(vm.Codigo))
                {
                    System.Windows.MessageBox.Show("Código de equipo inválido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = System.Windows.MessageBox.Show($"¿Está seguro que desea dar de baja el equipo '{vm.NombreEquipo}' (código: {vm.Codigo})? Esta acción marcará la fecha de baja.", "Confirmar baja de equipo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                    return;

                // Delegar la operación al ViewModel
                if (vm.DarDeBaja(out string mensaje, out bool persistedToDb))
                {
                    if (persistedToDb)
                    {
                        System.Windows.MessageBox.Show(mensaje, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(mensaje, "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    this.Close();
                    return;
                }

                // Si vm.DarDeBaja regresó false hubo un error
                System.Windows.MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al dar de baja el equipo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
