using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GestLog.Services.Core.Logging;

namespace GestLog.Views.Configuration.General
{
    public partial class GeneralConfigView : System.Windows.Controls.UserControl
    {
        private readonly IGestLogLogger? _logger;

        public GeneralConfigView()
        {
            InitializeComponent();
            _logger = LoggingService.GetLogger();
            DataContextChanged += GeneralConfigView_DataContextChanged;
        }

        private void GeneralConfigView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Sin log ni controles de depuración
        }

        private void ChkStartMaximized_Checked(object sender, RoutedEventArgs e)
        {
            // Sin log de depuración
        }

        private void ChkStartMaximized_Unchecked(object sender, RoutedEventArgs e)
        {
            // Sin log de depuración
        }

        private void BrowseWorkingDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Seleccionar Directorio de Trabajo"
            };

            if (dialog.ShowDialog() == true)
            {
                if (DataContext is GestLog.Models.Configuration.GeneralSettings settings)
                {
                    settings.WorkingDirectory = dialog.FolderName;
                }
            }
        }
    }
}
