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
                cvs.Filter -= TipoMantenimientoFilterHandler;
                cvs.Filter += TipoMantenimientoFilterHandler;
                cvs.View.Refresh();
            };
        }

        private void TipoMantenimientoFilterHandler(object sender, FilterEventArgs args)
        {
            if (args.Item is TipoMantenimiento tipo)
            {
                if (ModoRestringido)
                {
                    // En modo restringido normalmente solo permitimos Preventivo.
                    // Sin embargo, si el DTO ya tiene un TipoMtno preseleccionado (por ejemplo Correctivo desde el flujo de Equipos), permitir también ese valor para que el combo muestre la selección.
                    var preseleccionado = Seguimiento?.TipoMtno;
                    args.Accepted = tipo == TipoMantenimiento.Preventivo || (preseleccionado != null && tipo == preseleccionado);
                }
                else
                    args.Accepted = tipo == TipoMantenimiento.Preventivo || tipo == TipoMantenimiento.Correctivo || tipo == TipoMantenimiento.Predictivo;
            }
            else
            {
                args.Accepted = false;
            }
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

            // Agregar fecha programada en observaciones si corresponde
            if (Seguimiento.Estado == GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo ||
                Seguimiento.Estado == GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.NoRealizado)
            {
                DateTime fechaProgramada = FirstDateOfWeekISO8601(Seguimiento.Anio, Seguimiento.Semana);
                string observacionFechaProgramada = $"[Fecha programada: {fechaProgramada:dd/MM/yyyy}]";
                if (string.IsNullOrWhiteSpace(Seguimiento.Observaciones) || !Seguimiento.Observaciones.Contains(observacionFechaProgramada))
                {
                    Seguimiento.Observaciones = ((Seguimiento.Observaciones ?? "") + " " + observacionFechaProgramada).Trim();
                }
            }

            DialogResult = true;
            Close();
        }

        // Utilidad para obtener el primer día de la semana ISO 8601
        private static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var weekNum = weekOfYear;
            if (firstWeek <= 1)
                weekNum -= 1;
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
