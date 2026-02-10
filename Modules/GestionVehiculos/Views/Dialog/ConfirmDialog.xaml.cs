using System.Windows;

namespace GestLog.Modules.GestionVehiculos.Views.Dialog
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog()
        {
            InitializeComponent();
            BtnYes.Click += (s, e) => { DialogResult = true; Close(); };
            BtnNo.Click += (s, e) => { DialogResult = false; Close(); };
        }

        public string Message
        {
            get => TxtMessage.Text;
            set => TxtMessage.Text = value;
        }
    }
}
