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

                // [DEBUG] Verify ViewModel and properties are accessible
                try
                {
                    _logger.Information("[DEBUG] VehicleDocumentDialog constructor: DataContext type = {Type}", this.DataContext?.GetType().Name);
                    _logger.Information("[DEBUG] VehicleDocumentDialog constructor: SelectedFileName = '{FileName}'", viewModel.SelectedFileName);
                    _logger.Information("[DEBUG] VehicleDocumentDialog constructor: SelectedFilePath = '{FilePath}'", viewModel.SelectedFilePath);
                }
                catch (Exception ex)
                {
                    _logger.Information("[DEBUG] Error verificando ViewModel: {Message}", ex.Message);
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
                _logger.Information("[DEBUG] BtnSelectFile_Click ANTES - SelectedFileName = '{FileName}'", vm.SelectedFileName);

                vm.SelectFileCommand.Execute(null);

                _logger.Information("[DEBUG] BtnSelectFile_Click DESPUES - SelectedFileName = '{FileName}', SelectedFilePath = '{FilePath}'", vm.SelectedFileName, vm.SelectedFilePath);

                // Ejecutar forzado de UpdateTarget en el hilo UI tras una breve cola para asegurar que bindings estén listos
                try
                {
                    System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            tryUpdateBinding("TxtSelectedFile", TextBlock.TextProperty);
                            tryUpdateBinding("TxtSelectedFileBold", TextBlock.TextProperty);
                            tryUpdateBinding("TxtSelectedFileSize", TextBlock.TextProperty);
                            tryUpdateBinding("TxtSelectedFileMime", TextBlock.TextProperty);
                            tryUpdateBinding("ImgPreview", System.Windows.Controls.Image.SourceProperty);
                        }
                        catch (Exception ex)
                        {
                            _logger.Information("[DEBUG] BtnSelectFile_Click: error forzando UpdateTarget bindings (Dispatcher): {Message}", ex.Message);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    _logger.Information("[DEBUG] BtnSelectFile_Click: error al encolar Dispatcher para UpdateTarget: {Message}", ex.Message);
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
                    _logger.Information("[DEBUG] tryUpdateBinding: control '{Name}' no encontrado.", elementName);
                    return;
                }

                var be = BindingOperations.GetBindingExpression(element, dp);
                if (be != null)
                {
                    _logger.Information("[DEBUG] tryUpdateBinding: control '{Name}' binding path = '{Path}' - actualizando target.", elementName, be.ParentBinding.Path?.Path);
                    be.UpdateTarget();
                }
                else
                {
                    _logger.Information("[DEBUG] tryUpdateBinding: control '{Name}' no tiene BindingExpression.", elementName);
                }
            }
            catch (Exception ex)
            {
                _logger.Information("[DEBUG] tryUpdateBinding: excepción para '{Name}': {Message}", elementName, ex.Message);
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
        {
            if (this.DataContext is VehicleDocumentDialogModel vm && vm.RemoveSelectedFileCommand != null && vm.RemoveSelectedFileCommand.CanExecute(null))
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
                        }
                        catch (Exception ex)
                        {
                            _logger.Information("[DEBUG] BtnRemoveFile_Click: error forzando UpdateTarget bindings: {Message}", ex.Message);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    _logger.Information("[DEBUG] BtnRemoveFile_Click: error al invocar Dispatcher para UpdateTarget: {Message}", ex.Message);
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
