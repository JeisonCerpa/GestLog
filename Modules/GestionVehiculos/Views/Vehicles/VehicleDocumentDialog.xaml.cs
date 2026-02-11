using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionVehiculos.Views.Vehicles
{
    /// <summary>
    /// Interaction logic for VehicleDocumentDialog.xaml
    /// </summary>
    public partial class VehicleDocumentDialog : Window
    {
        public VehicleDocumentDialog()
        {
            InitializeComponent();

            // Obtener ViewModel desde DI
            var app = (App)System.Windows.Application.Current;
            var vm = app.ServiceProvider?.GetService<VehicleDocumentDialogModel>();
            if (vm == null)
                return; // caller puede usar constructor con parámetro

            this.DataContext = vm;
            vm.Owner = this;

            // Asegurar que el modal ocupe toda la pantalla del owner por defecto
            try
            {
                var ownerWindow = System.Windows.Application.Current?.MainWindow;
                if (ownerWindow != null)
                {
                    ConfigurarParaVentanaPadre(ownerWindow);
                }
            }
            catch { }

            // Suscribirse al evento OnExito si está disponible
            if (vm.GetType().GetEvent("OnExito") != null)
            {
                // Usamos reflexión para ser flexibles
                vm.GetType().GetEvent("OnExito")?.AddEventHandler(vm, new EventHandler((s, e) =>
                {
                    this.DialogResult = true;
                    this.Close();
                }));
            }

            // Manejar Escape para cerrar
            this.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    this.DialogResult = false;
                    this.Close();
                }
            };
        }

        public VehicleDocumentDialog(VehicleDocumentDialogModel viewModel) : this()
        {
            // Si se pasó explícitamente el ViewModel, usarlo
            if (viewModel != null)
            {
                this.DataContext = viewModel;
                viewModel.Owner = this;
            }
        }

        public VehicleDocumentDialogModel? ViewModel => this.DataContext as VehicleDocumentDialogModel;

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is VehicleDocumentDialogModel vm && vm.SelectFileCommand != null && vm.SelectFileCommand.CanExecute(null))
            {
                vm.SelectFileCommand.Execute(null);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is VehicleDocumentDialogModel vm && vm.SaveCommand != null && vm.SaveCommand.CanExecute(null))
            {
                vm.SaveCommand.Execute(null);
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid && grid.Name == "RootGrid")
            {
                e.Handled = true;
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Configura la ventana como modal maximizado sobre una ventana padre
        /// </summary>
        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;

            this.Owner = parentWindow;
            this.ShowInTaskbar = false;
            this.WindowState = WindowState.Maximized;

            this.Loaded += (s, e) =>
            {
                if (this.Owner != null)
                {
                    this.Owner.LocationChanged += (s2, e2) =>
                    {
                        if (this.WindowState != WindowState.Maximized)
                            this.WindowState = WindowState.Maximized;
                    };
                    this.Owner.SizeChanged += (s2, e2) =>
                    {
                        if (this.WindowState != WindowState.Maximized)
                            this.WindowState = WindowState.Maximized;
                    };
                }
            };
        }
    }
}
