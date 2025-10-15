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

        // Indica al caller que el usuario solicitó editar desde la vista de detalle
        public bool RequestEdit { get; private set; } = false;

        public PerifericoDetalleView(PerifericoEquipoInformaticoDto dto, IDbContextFactory<GestLogDbContext>? dbContextFactory = null, bool canEdit = false)
        {
            InitializeComponent();

            _dto = dto ?? throw new System.ArgumentNullException(nameof(dto));
            _dbContextFactory = dbContextFactory;

            // Enlazar ViewModel con los datos
            DataContext = new GestLog.ViewModels.Tools.GestionEquipos.PerifericoDetalleViewModel(dto, canEdit);

            this.Owner = System.Windows.Application.Current?.MainWindow;
            this.ShowInTaskbar = false;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            this.KeyDown += PerifericoDetalleView_KeyDown;
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
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            // Señalar al caller que se solicitó edición y cerrar. El caller deberá abrir el editor y persistir cambios.
            RequestEdit = true;
            this.Close();
        }

        // Helper para ajustar bounds y Owner para cubrir la pantalla del owner (multi-monitor / DPI)
        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow != null)
            {
                Owner = parentWindow;

                if (parentWindow.WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                    var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                    var bounds = screen.Bounds;

                    Left = bounds.Left;
                    Top = bounds.Top;
                    Width = bounds.Width;
                    Height = bounds.Height;
                    WindowState = WindowState.Normal;
                }
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }
    }
}
