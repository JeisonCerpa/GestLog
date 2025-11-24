using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.DatabaseConnection;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionEquipos
{    public partial class AgregarEquipoInformaticoView : Window
    {
        private System.Windows.Forms.Screen? _lastScreenOwner;

        public AgregarEquipoInformaticoView()
        {
            InitializeComponent();
            
            // Configurar como overlay modal
            try
            {
                this.Owner = System.Windows.Application.Current?.MainWindow;
                this.ShowInTaskbar = false;
                // Maximizar ANTES de mostrar para que se abra ya en pantalla completa
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                // No crítico
            }
              // ✅ MIGRADO: Configurar DataContext usando DI para resolver dependencias DatabaseAwareViewModel
            var app = (App?)System.Windows.Application.Current;
            var serviceProvider = app?.ServiceProvider;
            if (serviceProvider == null)
            {
                // Fallback si no se puede resolver
                return;
            }
            
            var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
            var dbContextFactory = serviceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<GestLog.Modules.DatabaseConnection.GestLogDbContext>>();
            var databaseService = serviceProvider.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
            var logger = serviceProvider.GetRequiredService<GestLog.Services.Core.Logging.IGestLogLogger>();
            
            var viewModel = new GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel(
                currentUserService, dbContextFactory, databaseService, logger);
            DataContext = viewModel;
            
            // Cargar personas cuando se carga la ventana, pero sólo si no están ya cargadas (evita sobrescribir selección al abrir en modo editar)
            this.Loaded += async (sender, e) => {
                try
                {
                    if (viewModel.PersonasDisponibles == null || !viewModel.PersonasDisponibles.Any())
                    {
                        await viewModel.InicializarAsync();
                    }

                    // Si tiene Owner, sincronizar cambios de tamaño
                    if (this.Owner != null)
                    {
                        this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                        this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
                    }
                }
                catch { }
            };
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel vm)
                {
                    if (vm.CancelarCommand.CanExecute(null))
                    {
                        vm.CancelarCommand.Execute(null);
                        this.Close();
                        return;
                    }
                }
                // Fallback directo
                this.DialogResult = false;
                this.Close();
            }
            catch { this.DialogResult = false; this.Close(); }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel vm)
                {
                    if (vm.GuardarCommand.CanExecute(null))
                    {
                        // Await la tarea para que las excepciones sean observadas y la ventana no se cierre antes de completar
                        try
                        {
                            await vm.GuardarAsync().ConfigureAwait(true);

                            // Intentar establecer DialogResult sólo si la ventana fue mostrada como diálogo.
                            // Si no se puede (InvalidOperationException), cerrar como fallback.
                            bool dialogResultSet = false;
                            try
                            {
                                this.DialogResult = true; // esto cierra la ventana cuando fue opened con ShowDialog()
                                dialogResultSet = true;
                            }
                            catch (InvalidOperationException)
                            {
                                // No fue mostrada como dialog (Show), fallback: cerrar la ventana
                            }

                            if (!dialogResultSet)
                                this.Close();

                            return;
                        }
                        catch (Exception ex)
                        {
                            // Mostrar error al usuario y no cerrar la ventana
                            System.Windows.MessageBox.Show($"Error al guardar el equipo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
                // Intentar fallback: cerrar como cancel si no se pudo ejecutar
                this.DialogResult = false;
                this.Close();
            }
            catch
            {
                this.DialogResult = false;
                this.Close();
            }
        }        /// <summary>
        /// Manejador para sincronizar tamaño cuando la ventana padre cambia
        /// </summary>
        private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
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

        /// <summary>
        /// Manejador de clic en overlay - cierra la ventana
        /// </summary>
        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// Previene que el clic en la card cierre la ventana
        /// </summary>
        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Cierra la ventana desde el botón X del header
        /// </summary>
        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
