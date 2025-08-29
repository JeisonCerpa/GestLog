using System.Windows.Controls;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    /// <summary>
    /// Vista principal de Gestión de Mantenimientos con tabs para Equipos, Cronograma y Seguimiento
    /// </summary>
    public partial class GestionMantenimientosView : WpfUserControl
    {
        private bool _equiposLoaded = false;
        private bool _cronogramaLoaded = false;
        private bool _seguimientoLoaded = false;

        public GestionMantenimientosView()
        {
            InitializeComponent();
            
            // Cargar solo la primera pestaña inicialmente
            LoadEquiposTab();
            _equiposLoaded = true;
        }

        private void OnTabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != tabMantenimientos) return;

            var selectedTab = tabMantenimientos.SelectedItem as TabItem;
            if (selectedTab == null) return;

            var header = selectedTab.Header.ToString();
            
            switch (header)
            {
                case "Equipos":
                    if (!_equiposLoaded)
                    {
                        LoadEquiposTab();
                        _equiposLoaded = true;
                    }
                    break;
                case "Cronograma":
                    if (!_cronogramaLoaded)
                    {
                        LoadCronogramaTab();
                        _cronogramaLoaded = true;
                    }
                    break;
                case "Seguimiento":
                    if (!_seguimientoLoaded)
                    {
                        LoadSeguimientoTab();
                        _seguimientoLoaded = true;
                    }
                    break;
            }
        }

        private void LoadEquiposTab()
        {
            var equiposTab = (TabItem)tabMantenimientos.Items[0];
            if (equiposTab.Content == null)
            {
                equiposTab.Content = new EquiposView();
            }
        }

        private void LoadCronogramaTab()
        {
            var cronogramaTab = (TabItem)tabMantenimientos.Items[1];
            if (cronogramaTab.Content == null)
            {
                cronogramaTab.Content = new CronogramaView();
            }
        }

        private void LoadSeguimientoTab()
        {
            var seguimientoTab = (TabItem)tabMantenimientos.Items[2];
            if (seguimientoTab.Content == null)
            {
                seguimientoTab.Content = new SeguimientoView();
            }
        }
    }
}
