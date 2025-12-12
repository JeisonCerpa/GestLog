using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Perifericos;
using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Perifericos
{
    public partial class PerifericoDetalleView : Window
    {
        private readonly PerifericoEquipoInformaticoDto _dto;
        private readonly IDbContextFactory<GestLogDbContext>? _dbContextFactory;
        private System.Windows.Forms.Screen? _lastScreenOwner;

        // Indica al caller que el usuario solicitó editar desde la vista de detalle
        public bool RequestEdit { get; private set; } = false;

        public PerifericoDetalleView(PerifericoEquipoInformaticoDto dto, IDbContextFactory<GestLogDbContext>? dbContextFactory = null, bool canEdit = false)
        {
            InitializeComponent();

            _dto = dto ?? throw new System.ArgumentNullException(nameof(dto));
            _dbContextFactory = dbContextFactory;            // Enlazar ViewModel con los datos
            DataContext = new GestLog.Modules.GestionEquiposInformaticos.ViewModels.Perifericos.PerifericoDetalleViewModel(dto, canEdit);

            // Manejar tecla Escape
            this.KeyDown += PerifericoDetalleView_KeyDown;
            this.Loaded += PerifericoDetalleView_Loaded;
        }

        private void PerifericoDetalleView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            // Señalar al caller que se solicitó edición y cerrar. El caller deberá abrir el editor y persistir cambios.
            RequestEdit = true;
            this.Close();
        }        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;

            try
            {
                // Guardar referencia a la pantalla actual del owner
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                _lastScreenOwner = screen;

                // Para un overlay modal, siempre maximizar para cubrir toda la pantalla
                // Esto evita problemas de DPI, pantallas múltiples y posicionamiento
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                // Fallback: maximizar en pantalla principal
                this.WindowState = WindowState.Maximized;
            }
        }

        private void PerifericoDetalleView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                // Si el Owner se mueve/redimensiona, mantener sincronizado
                this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }
        }        private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
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
    }
}

