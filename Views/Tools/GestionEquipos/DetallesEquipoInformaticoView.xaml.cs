using System.Windows;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.ViewModels.Tools.GestionEquipos;
using System.Linq;
using System.Globalization;
using GestLog.Modules.DatabaseConnection; // agregado para poder recibir GestLogDbContext
using Microsoft.Extensions.DependencyInjection; // ✅ MIGRADO: Para resolver dependencias

namespace GestLog.Views.Tools.GestionEquipos
{    public partial class DetallesEquipoInformaticoView : Window
    {
        // Guardar referencia al DbContext pasado por el llamador para reutilizarlo en refrescos
        private readonly GestLogDbContext? _db;
        
        // Referencia a la pantalla actual para detectar cambios de monitor en multi-monitor
        private System.Windows.Forms.Screen? _lastScreenOwner;

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
            
            // ✅ MIGRADO: Resolver dependencias para DatabaseAwareViewModel de forma segura
            try
            {
                var app = System.Windows.Application.Current as App;
                var serviceProvider = app?.ServiceProvider;
                
                if (serviceProvider == null)
                {
                    System.Windows.MessageBox.Show("Error: No se pudo acceder a las dependencias de la aplicación.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dbContextFactory = serviceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<GestLogDbContext>>();
                var databaseService = serviceProvider.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                var logger = serviceProvider.GetRequiredService<GestLog.Services.Core.Logging.IGestLogLogger>();
                var seguimientoService = serviceProvider.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IGestionEquiposInformaticosSeguimientoCronogramaService>();
                
                DataContext = new DetallesEquipoInformaticoViewModel(equipo, dbContextFactory, databaseService, logger, seguimientoService);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cargar el ViewModel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        /// <summary>
        /// Configura el overlay de la ventana modal para cubrir exactamente la pantalla del Owner.
        /// Usa WindowState.Maximized para soporte multi-monitor robusto sin problemas de DPI.
        /// Método basado en estándar ModalWindowsStandard.md
        /// </summary>
        public void ConfigurarParaVentanaPadre(Window owner)
        {
            if (owner == null) return;
            
            this.Owner = owner;
            this.ShowInTaskbar = false;

            try
            {
                // Guardar referencia a la pantalla actual del owner
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(owner);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                _lastScreenOwner = screen;

                // Para un overlay modal, siempre maximizar para cubrir toda la pantalla
                // Esto evita problemas de DPI, pantallas múltiples y posicionamiento
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                // Fallback: maximizar en pantalla principal
                this.WindowState = WindowState.Maximized;
            }
        }// Nuevo manejador para registrar los event handlers cuando se carga la ventana
        private void DetallesEquipoInformaticoView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                // Si el Owner se mueve/redimensiona, mantener el overlay sincronizado
                this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }
        }        private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
        {
            if (this.Owner == null) return;

            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Siempre maximizar para mantener el overlay cubriendo toda la pantalla
                    this.WindowState = WindowState.Maximized;
                    
                    // Detectar si el Owner cambió de pantalla
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(this.Owner);
                    var currentScreen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                    // Si cambió de pantalla, actualizar la referencia
                    if (_lastScreenOwner == null || !_lastScreenOwner.DeviceName.Equals(currentScreen.DeviceName))
                    {
                        _lastScreenOwner = currentScreen;
                    }
                }
                catch
                {
                    // En caso de error, asegurar que la ventana está maximizada
                    this.WindowState = WindowState.Maximized;
                }
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
        }        private async void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(this.DataContext is DetallesEquipoInformaticoViewModel detallesVm))
                {
                    System.Windows.MessageBox.Show("No se pudo obtener la información del equipo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Abrir la ventana de edición
                var editarWindow = new AgregarEquipoInformaticoView();
                
                // Precargar datos en el ViewModel de edición
                if (editarWindow.DataContext is GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel editVm)
                {
                    // Copiar datos principales
                    editVm.Codigo = detallesVm.Codigo;
                    editVm.NombreEquipo = detallesVm.NombreEquipo ?? string.Empty;
                    editVm.Marca = detallesVm.Marca ?? string.Empty;
                    editVm.Modelo = detallesVm.Modelo ?? string.Empty;
                    editVm.Procesador = detallesVm.Procesador ?? string.Empty;
                    editVm.So = detallesVm.SO ?? string.Empty;
                    editVm.SerialNumber = detallesVm.SerialNumber ?? string.Empty;
                    editVm.CodigoAnydesk = detallesVm.CodigoAnydesk ?? string.Empty;
                    editVm.Observaciones = detallesVm.Observaciones ?? string.Empty;                    editVm.Costo = detallesVm.Costo;
                    editVm.FechaCompra = detallesVm.FechaCompra;
                    editVm.Estado = detallesVm.Estado ?? string.Empty;
                    editVm.Sede = detallesVm.Sede ?? string.Empty;
                    
                    // Copiar listas
                    editVm.ListaRam = new System.Collections.ObjectModel.ObservableCollection<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity>(
                        detallesVm.SlotsRam?.ToList() ?? new System.Collections.Generic.List<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity>());
                    editVm.ListaDiscos = new System.Collections.ObjectModel.ObservableCollection<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity>(
                        detallesVm.Discos?.ToList() ?? new System.Collections.Generic.List<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity>());
                    editVm.ListaConexiones = new System.Collections.ObjectModel.ObservableCollection<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.ConexionEntity>(
                        detallesVm.Conexiones?.ToList() ?? new System.Collections.Generic.List<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.ConexionEntity>());

                    // Marcar modo edición
                    editVm.IsEditing = true;
                    editVm.OriginalCodigo = detallesVm.Codigo;

                    // Guardar usuario asignado original
                    string usuarioAsignado = detallesVm.UsuarioAsignado ?? string.Empty;

                    // Inicializar ViewModel (carga de personas)
                    try 
                    { 
                        await editVm.InicializarAsync(); 
                    } 
                    catch (Exception ex)
                    { 
                        System.Windows.MessageBox.Show($"Error al inicializar: {ex.Message}", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    // Intentar seleccionar la persona asignada
                    if (!string.IsNullOrWhiteSpace(usuarioAsignado) && editVm.PersonasDisponibles != null)
                    {
                        var persona = editVm.PersonasDisponibles.FirstOrDefault(p => 
                            p.NombreCompleto.Equals(usuarioAsignado, StringComparison.OrdinalIgnoreCase));
                        
                        if (persona != null)
                        {
                            editVm.PersonaAsignada = persona;
                        }
                    }
                }

                // Mostrar ventana de edición
                var result = editarWindow.ShowDialog();

                // Si se guardó, recargar datos
                if (result == true && editarWindow.DataContext is GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel editVm2)
                {
                    try
                    {
                        // Reconstruir entidad con los datos editados
                        var equipoEditado = new EquipoInformaticoEntity
                        {
                            Codigo = editVm2.Codigo,
                            NombreEquipo = editVm2.NombreEquipo,
                            Marca = editVm2.Marca,
                            Modelo = editVm2.Modelo,
                            Procesador = editVm2.Procesador,
                            SO = editVm2.So,
                            SerialNumber = editVm2.SerialNumber,
                            CodigoAnydesk = editVm2.CodigoAnydesk,
                            Observaciones = editVm2.Observaciones,
                            Costo = editVm2.Costo,
                            FechaCompra = editVm2.FechaCompra,
                            Estado = editVm2.Estado,
                            Sede = editVm2.Sede,
                            UsuarioAsignado = editVm2.PersonaAsignada?.NombreCompleto ?? string.Empty,
                            SlotsRam = editVm2.ListaRam?.ToList() ?? new System.Collections.Generic.List<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity>(),
                            Discos = editVm2.ListaDiscos?.ToList() ?? new System.Collections.Generic.List<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity>(),
                            Conexiones = editVm2.ListaConexiones?.ToList() ?? new System.Collections.Generic.List<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.ConexionEntity>()
                        };

                        // Crear nuevo ViewModel con datos actualizados
                        try
                        {
                            var app = System.Windows.Application.Current as App;
                            var serviceProvider = app?.ServiceProvider;
                            if (serviceProvider != null)
                            {
                                var dbContextFactory = serviceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<GestLogDbContext>>();
                                var databaseService = serviceProvider.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                                var logger = serviceProvider.GetRequiredService<GestLog.Services.Core.Logging.IGestLogLogger>();
                                var seguimientoService = serviceProvider.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IGestionEquiposInformaticosSeguimientoCronogramaService>();
                                
                                this.DataContext = new DetallesEquipoInformaticoViewModel(equipoEditado, dbContextFactory, databaseService, logger, seguimientoService);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Error al recargar detalles: {ex.Message}", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error al procesar cambios: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
