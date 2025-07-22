using System.Windows;
using System.Windows.Data;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    public partial class SeguimientoDialog : Window
    {
        public SeguimientoMantenimientoDto Seguimiento { get; private set; }
        public bool ModoRestringido { get; }
        public SeguimientoDialog(SeguimientoMantenimientoDto? seguimiento = null, bool modoRestringido = false)
        {
            InitializeComponent();
            Seguimiento = seguimiento != null ? new SeguimientoMantenimientoDto(seguimiento) : new SeguimientoMantenimientoDto();
            ModoRestringido = modoRestringido;
            // Si es registro nuevo y no tiene fecha, prellenar con la fecha actual
            if (seguimiento == null || Seguimiento.FechaRealizacion == null)
                Seguimiento.FechaRealizacion = System.DateTime.Now;
            DataContext = Seguimiento;
            Loaded += (s, e) =>
            {
                var cvs = (CollectionViewSource)this.Resources["TipoMantenimientoFiltrado"];
                cvs.Filter += (sender, args) =>
                {
                    if (args.Item is TipoMantenimiento tipo)
                    {
                        if (ModoRestringido)
                            args.Accepted = tipo == TipoMantenimiento.Correctivo || tipo == TipoMantenimiento.Predictivo;
                        else
                            args.Accepted = true;
                    }
                    else
                    {
                        args.Accepted = false;
                    }
                };
                cvs.View.Refresh();
            };
        }
        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            var errores = new List<string>();
            // Tipo de mantenimiento obligatorio
            if (Seguimiento.TipoMtno == null)
                errores.Add("Debe seleccionar el tipo de mantenimiento.");
            // Descripción obligatoria y máximo 200 caracteres
            if (string.IsNullOrWhiteSpace(Seguimiento.Descripcion))
                errores.Add("La descripción es obligatoria.");
            else if (Seguimiento.Descripcion.Length > 200)
                errores.Add("La descripción no puede superar los 200 caracteres.");
            // Responsable obligatorio
            if (string.IsNullOrWhiteSpace(Seguimiento.Responsable))
                errores.Add("El responsable es obligatorio.");
            // Costo no negativo
            if (Seguimiento.Costo != null && Seguimiento.Costo < 0)
                errores.Add("El costo no puede ser negativo.");
            // Frecuencia obligatoria si el campo es visible (no modo restringido)
            if (!ModoRestringido && Seguimiento.Frecuencia == null)
                errores.Add("Debe seleccionar la frecuencia de mantenimiento.");
            if (errores.Count > 0)
            {
                System.Windows.MessageBox.Show(string.Join("\n", errores), "Errores de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
