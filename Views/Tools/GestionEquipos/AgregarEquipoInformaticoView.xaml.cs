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
        public AgregarEquipoInformaticoView()
        {
            InitializeComponent();
            
            // Configurar DataContext usando DI para resolver dependencias
            var app = (App)System.Windows.Application.Current;
            var serviceProvider = app.ServiceProvider;
            var currentUserService = serviceProvider.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
            var viewModel = new GestLog.ViewModels.Tools.GestionEquipos.AgregarEquipoInformaticoViewModel(currentUserService);
            DataContext = viewModel;
            
            // Cargar personas cuando se carga la ventana, pero sólo si no están ya cargadas (evita sobrescribir selección al abrir en modo editar)
            this.Loaded += async (sender, e) => {
                try
                {
                    if (viewModel.PersonasDisponibles == null || !viewModel.PersonasDisponibles.Any())
                    {
                        await viewModel.InicializarAsync();
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
                            // Sólo cerrar el diálogo si el guardado finalizó correctamente (el ViewModel establece DialogResult=true)
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
        }
    }
}
