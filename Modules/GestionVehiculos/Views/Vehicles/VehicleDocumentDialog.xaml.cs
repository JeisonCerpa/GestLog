using System;
using System.Windows;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;

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
        }

        public VehicleDocumentDialog(VehicleDocumentDialogModel viewModel) : this()
        {
            // Constructor que recibe el ViewModel ya resuelto desde el caller
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
    }
}
