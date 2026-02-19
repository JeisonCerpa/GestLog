using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Data;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using Serilog;

namespace GestLog.Modules.GestionVehiculos.Views.Vehicles
{
    /// <summary>
    /// Interaction logic for VehicleDocumentDialog.xaml
    /// </summary>
    public partial class VehicleDocumentDialog : Window
    {
        private readonly ILogger _logger = Log.ForContext<VehicleDocumentDialog>();

        public VehicleDocumentDialog()
        {
            InitializeComponent();

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

                // Cerrar la ventana cuando el ViewModel indique éxito (Save completado)
                viewModel.OnExito += (s, e) =>
                {
                    try
                    {
                        System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                this.DialogResult = true;
                            }
                            catch { }
                            try { this.Close(); } catch { }
                        }));
                    }
                    catch { }
                };

                try
                {
                    // Removed debug logs to reduce noise in production logs
                }
                catch (Exception)
                {
                    // Silently ignore verification errors
                }

                // Configurar modal para ocupar toda la pantalla
                try
                {
                    var ownerWindow = System.Windows.Application.Current?.MainWindow;
                    if (ownerWindow != null)
                    {
                        ConfigurarParaVentanaPadre(ownerWindow);
                    }
                }
                catch { }
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

                // Ejecutar forzado de UpdateTarget en el hilo UI tras una breve cola para asegurar que bindings estén listos
                try
                {                    System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            tryUpdateBinding("TxtSelectedFile", TextBlock.TextProperty);
                            tryUpdateBinding("TxtSelectedFileSize", TextBlock.TextProperty);
                            tryUpdateBinding("TxtSelectedFileMime", TextBlock.TextProperty);
                            tryUpdateBinding("ImgPreview", System.Windows.Controls.Image.SourceProperty);
                            // Force update binding for Visibility property to ensure IsImagePreview is evaluated
                            tryUpdateBinding("ImgPreview", System.Windows.UIElement.VisibilityProperty);

                            // Force update binding for PDF icon visibility
                            tryUpdateBinding("PdfIconContainer", System.Windows.UIElement.VisibilityProperty);
                        }
                        catch (Exception)
                        {
                            // Ignorar errores menores al forzar actualizaciones de binding
                        }
                    }));
                }
                catch (Exception)
                {
                    // Ignorar errores al encolar Dispatcher
                }
            }
        }

        private void tryUpdateBinding(string elementName, System.Windows.DependencyProperty dp)
        {
            try
            {
                var element = this.FindName(elementName) as System.Windows.FrameworkElement;
                if (element == null)
                {
                    // Control no encontrado, salir silenciosamente
                    return;
                }

                var be = BindingOperations.GetBindingExpression(element, dp);
                if (be != null)
                {
                    be.UpdateTarget();
                }
                else
                {
                    // No tiene binding, nothing to update
                }
            }
            catch (Exception)
            {
                // Ignorar excepciones al actualizar bindings
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is VehicleDocumentDialogModel vm && vm.SaveCommand != null && vm.SaveCommand.CanExecute(null))
            {
                vm.SaveCommand.Execute(null);
            }
        }

        private void BtnRemoveFile_Click(object sender, RoutedEventArgs e)
        {            if (this.DataContext is VehicleDocumentDialogModel vm && vm.RemoveSelectedFileCommand != null && vm.RemoveSelectedFileCommand.CanExecute(null))
            {
                vm.RemoveSelectedFileCommand.Execute(null);

                // Forzar actualización inmediata en el hilo UI y actualizar bindings
                try
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(new Action(() =>
                    {
                        try
                        {
                            tryUpdateBinding("TxtSelectedFile", TextBlock.TextProperty);
                            tryUpdateBinding("TxtSelectedFileSize", TextBlock.TextProperty);
                            tryUpdateBinding("TxtSelectedFileMime", TextBlock.TextProperty);
                            tryUpdateBinding("ImgPreview", System.Windows.Controls.Image.SourceProperty);
                            // Force update binding for Visibility property to ensure IsImagePreview is evaluated
                            tryUpdateBinding("ImgPreview", System.Windows.UIElement.VisibilityProperty);
                            // Force update binding for PDF icon visibility
                            tryUpdateBinding("PdfIconContainer", System.Windows.UIElement.VisibilityProperty);
                        }
                        catch (Exception)
                        {
                            // Ignorar errores al forzar update de bindings
                        }
                    }));
                }
                catch (Exception)
                {
                    // Ignorar errores del Dispatcher
                }
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
