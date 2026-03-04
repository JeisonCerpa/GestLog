using GestLog.Modules.GestionVehiculos.Models.DTOs;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace GestLog.Modules.GestionVehiculos.Views.Combustible
{
    public partial class RegistroTanqueadaDialog : Window
    {
        public ConsumoCombustibleVehiculoDto? Resultado { get; private set; }

        public RegistroTanqueadaDialog(ConsumoCombustibleVehiculoDto? existente)
        {
            InitializeComponent();

            if (existente != null)
            {
                Title = "Editar tanqueada";
                DpFecha.SelectedDate = existente.FechaTanqueada.Date;
                TxtKM.Text = existente.KMAlMomento.ToString(CultureInfo.CurrentCulture);
                TxtGalones.Text = existente.Galones.ToString(CultureInfo.CurrentCulture);
                TxtValor.Text = existente.ValorTotal.ToString(CultureInfo.CurrentCulture);
                TxtProveedor.Text = existente.Proveedor ?? string.Empty;
                TxtObs.Text = existente.Observaciones ?? string.Empty;
                Resultado = new ConsumoCombustibleVehiculoDto
                {
                    Id = existente.Id,
                    PlacaVehiculo = existente.PlacaVehiculo
                };
            }
            else
            {
                DpFecha.SelectedDate = DateTime.Today;
            }

            // Configurar como modal maximizado sobre la ventana padre
            var ownerWindow = System.Windows.Application.Current?.MainWindow;
            if (ownerWindow != null)
                ConfigurarParaVentanaPadre(ownerWindow);

            // Manejar Escape para cerrar
            this.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
        }

        /// <summary>
        /// Configura la ventana como modal maximizado, asegurando que el overlay cubra toda la pantalla
        /// </summary>
        private void ConfigurarParaVentanaPadre(Window parentWindow)
        {
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;
            this.WindowState = WindowState.Maximized;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Cerrar al hacer clic en el overlay oscuro (solo RootGrid)
            if (sender is Grid grid && grid.Name == "RootGrid")
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Evitar que el clic en el panel principal dispare el cierre del overlay
            e.Handled = true;
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (DpFecha.SelectedDate == null)
            {
                System.Windows.MessageBox.Show("Selecciona una fecha válida.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!long.TryParse(TxtKM.Text?.Trim(), out var km) || km < 0)
            {
                System.Windows.MessageBox.Show("Ingresa un kilometraje válido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtGalones.Text?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var galones) &&
                !decimal.TryParse(TxtGalones.Text?.Trim(), out galones))
            {
                System.Windows.MessageBox.Show("Ingresa galones válidos.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtValor.Text?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) &&
                !decimal.TryParse(TxtValor.Text?.Trim(), out valor))
            {
                System.Windows.MessageBox.Show("Ingresa un valor válido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (galones <= 0 || valor < 0)
            {
                System.Windows.MessageBox.Show("Los galones deben ser mayores a cero y el valor no puede ser negativo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Resultado ??= new ConsumoCombustibleVehiculoDto();
            Resultado.FechaTanqueada = new DateTimeOffset(DpFecha.SelectedDate.Value.Date);
            Resultado.KMAlMomento = km;
            Resultado.Galones = decimal.Round(galones, 2);
            Resultado.ValorTotal = decimal.Round(valor, 2);
            Resultado.Proveedor = TxtProveedor.Text?.Trim();
            Resultado.Observaciones = TxtObs.Text?.Trim();

            DialogResult = true;
            Close();
        }
    }
}
