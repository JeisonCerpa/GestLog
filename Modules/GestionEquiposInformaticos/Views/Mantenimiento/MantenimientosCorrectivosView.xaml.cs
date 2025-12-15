using System;
using System.Linq;
using System.Windows;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento
{
    /// <summary>
    /// Lógica de interacción para MantenimientosCorrectivosView.xaml
    /// </summary>
    public partial class MantenimientosCorrectivosView
    {
        public MantenimientosCorrectivosView()
        {
            InitializeComponent();
        }        private void BtnNuevoMantenimiento_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Crear diálogo modal - obtiene el ViewModel del contenedor DI automáticamente
                var dialog = new RegistroMantenimientoCorrectivoDialog();
                
                if (dialog.ShowDialog() == true && this.DataContext is GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.MantenimientosCorrectivosViewModel vm)
                {
                    _ = vm.CargarMantenimientosAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnVerMantenimiento_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button btn && btn.Tag is int id && this.DataContext is GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.MantenimientosCorrectivosViewModel vm)
                {
                    var mtto = vm.MantenimientosCorrectivos.FirstOrDefault(m => m.Id == id);
                    if (mtto != null)
                    {
                        vm.SelectedMantenimiento = mtto;
                        System.Windows.MessageBox.Show($"ID: {mtto.Id}\nEquipo: {mtto.EquipoInformaticoCodigo ?? mtto.PerifericoEquipoInformaticoCodigo}\nFalla: {mtto.DescripcionFalla}", "Detalles", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }        private void BtnCompletarMantenimiento_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button btn && btn.Tag is int id && this.DataContext is GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.MantenimientosCorrectivosViewModel vm)
                {
                    var mtto = vm.MantenimientosCorrectivos.FirstOrDefault(m => m.Id == id);
                    if (mtto != null)
                    {
                        // Obtener el ViewModel del contenedor DI
                        var serviceProvider = ((App)System.Windows.Application.Current).ServiceProvider;
                        var dialogViewModel = serviceProvider?.GetService(typeof(GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.CompletarCancelarMantenimientoViewModel)) 
                            as GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.CompletarCancelarMantenimientoViewModel;
                        
                        if (dialogViewModel != null)
                        {
                            vm.SelectedMantenimiento = mtto;
                            var dialog = new CompletarCancelarMantenimientoDialog();
                            dialogViewModel.SetMantenimiento(mtto);
                            
                            if (dialog.ShowDialog() == true)
                            {
                                _ = vm.CargarMantenimientosAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
