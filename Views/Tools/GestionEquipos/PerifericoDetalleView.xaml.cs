using System.Windows;
using System.Windows.Input;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;

namespace GestLog.Views.Tools.GestionEquipos
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
            _dbContextFactory = dbContextFactory;

            // Enlazar ViewModel con los datos
            DataContext = new GestLog.ViewModels.Tools.GestionEquipos.PerifericoDetalleViewModel(dto, canEdit);

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
        }

        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;

            try
            {
                // Si la ventana padre está maximizada, maximizar esta también
                if (parentWindow.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Maximized;
                }
                else
                {
                    // Para ventanas no maximizadas, obtener los bounds de la pantalla
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                    var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                    
                    // Guardar referencia a la pantalla actual
                    _lastScreenOwner = screen;
                    
                    // Usar los bounds completos de la pantalla
                    var bounds = screen.Bounds;
                    
                    // Configurar para cubrir toda la pantalla
                    this.Left = bounds.Left;
                    this.Top = bounds.Top;
                    this.Width = bounds.Width;
                    this.Height = bounds.Height;
                    this.WindowState = WindowState.Normal;
                }
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
        }

        private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
        {
            if (this.Owner == null) return;

            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Si la ventana padre está maximizada, maximizar esta también
                    if (this.Owner.WindowState == WindowState.Maximized)
                    {
                        this.WindowState = WindowState.Maximized;
                    }
                    else
                    {
                        // Detectar si el Owner cambió de pantalla
                        var interopHelper = new System.Windows.Interop.WindowInteropHelper(this.Owner);
                        var currentScreen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                        // Si cambió de pantalla, recalcular bounds
                        if (_lastScreenOwner == null || !_lastScreenOwner.DeviceName.Equals(currentScreen.DeviceName))
                        {
                            // Owner cambió de pantalla, recalcular
                            ConfigurarParaVentanaPadre(this.Owner);
                        }
                        else
                        {
                            // Mismo monitor, actualizar posición
                            var bounds = currentScreen.Bounds;
                            this.Left = bounds.Left;
                            this.Top = bounds.Top;
                            this.Width = bounds.Width;
                            this.Height = bounds.Height;
                        }
                    }
                }
                catch
                {
                    try
                    {
                        ConfigurarParaVentanaPadre(this.Owner);
                    }
                    catch { }
                }
            });
        }
    }
}
