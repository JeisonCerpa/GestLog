using System.Windows;
using System.Windows.Data;
using System.Collections.Generic;
// se usan tipos WPF fully-qualified para evitar ambigüedad con WinForms
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Views.Tools.GestionMantenimientos
{    
    public partial class SeguimientoDialog : Window
    {
        public SeguimientoMantenimientoDto Seguimiento { get; private set; }
        public bool ModoRestringido { get; }        
        
        // Prefijo generado por el checklist para no duplicarlo al componer la descripción
        private string lastGeneratedChecklist = string.Empty;

        public SeguimientoDialog(SeguimientoMantenimientoDto? seguimiento = null, bool modoRestringido = false)
        {
            InitializeComponent();
            Seguimiento = seguimiento != null ? new SeguimientoMantenimientoDto(seguimiento) : new SeguimientoMantenimientoDto();
            ModoRestringido = modoRestringido;
            // Si es registro nuevo y no tiene fecha, prellenar con la fecha actual
            if (seguimiento == null || Seguimiento.FechaRealizacion == null)
                Seguimiento.FechaRealizacion = System.DateTime.Now;
            DataContext = Seguimiento;
            this.ShowInTaskbar = false;
            this.KeyDown += SeguimientoDialog_KeyDown;
            this.Loaded += SeguimientoDialog_OnLoaded;
            Loaded += (s, e) =>
            {
                var cvs = (CollectionViewSource)this.Resources["TipoMantenimientoFiltrado"];
                cvs.Filter -= TipoMantenimientoFilterHandler;
                cvs.Filter += TipoMantenimientoFilterHandler;
                cvs.View.Refresh();
            };
        }        
        
        private void SeguimientoDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Si no tiene Owner, usar la ventana principal como Owner
            if (this.Owner == null)
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    this.Owner = mainWindow;
                }
            }

            // Maximizar ventana para que el overlay cubra toda la pantalla
            this.WindowState = WindowState.Maximized;

            // Sincronizar con cambios de tamaño/posición del owner
            if (this.Owner != null)
            {
                this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }

            // Inicializar los CheckBoxes del checklist si la descripción ya contiene esos textos
            try
            {
                var desc = (Seguimiento?.Descripcion ?? string.Empty);
                if (!string.IsNullOrEmpty(desc))
                {
                    // Marcar checkboxes si aparecen las frases esperadas
                    if (this.FindName("cbRevision") is System.Windows.Controls.CheckBox cbRev)
                        cbRev.IsChecked = desc.Contains("Revisión General");
                    if (this.FindName("cbLimpieza") is System.Windows.Controls.CheckBox cbLimp)
                        cbLimp.IsChecked = desc.Contains("Limpieza");
                    if (this.FindName("cbAjustes") is System.Windows.Controls.CheckBox cbAj)
                        cbAj.IsChecked = desc.Contains("Ajustes");

                    // Guardar el prefijo generado actualmente para evitar duplicados posteriores
                    lastGeneratedChecklist = BuildChecklistPrefixFromControls();
                }
            }
            catch
            {
                // No crítico si falla la inicialización de controles
            }
        }

        // Construye el prefijo de checklist según los CheckBoxes actuales
        private string BuildChecklistPrefixFromControls()
        {
            var items = new List<string>();
            if (this.FindName("cbRevision") is System.Windows.Controls.CheckBox cbRev && cbRev.IsChecked == true)
                items.Add("Revisión General");
            if (this.FindName("cbLimpieza") is System.Windows.Controls.CheckBox cbLimp && cbLimp.IsChecked == true)
                items.Add("Limpieza");
            if (this.FindName("cbAjustes") is System.Windows.Controls.CheckBox cbAj && cbAj.IsChecked == true)
                items.Add("Ajustes");
            return items.Count > 0 ? string.Join("; ", items) : string.Empty;
        }

        // Evento común para Checked/Unchecked de los items del checklist
        private void ChecklistItem_Checked(object? sender, RoutedEventArgs e)
        {
            try
            {
                var newPrefix = BuildChecklistPrefixFromControls();

                // Tomar el texto actual (puede venir del binding)
                var current = Seguimiento.Descripcion ?? string.Empty;

                // Si el texto actual empieza con el último prefijo generado, removerlo para reemplazar por el nuevo
                var remaining = current;
                if (!string.IsNullOrEmpty(lastGeneratedChecklist) && remaining.StartsWith(lastGeneratedChecklist))
                {
                    remaining = remaining.Substring(lastGeneratedChecklist.Length).TrimStart(' ', '–', '-', ':');
                    if (remaining.StartsWith("—")) // caracteres de separación
                        remaining = remaining.TrimStart('—', ' ', '-', ':');
                }

                // Si queda un separador al inicio, limpiarlo
                remaining = remaining.TrimStart(' ', '–', '-', ':');

                // Formar nueva descripción combinando el prefijo generado y el texto manual restante
                string nueva;
                if (string.IsNullOrEmpty(newPrefix))
                    nueva = remaining; // no hay items seleccionados
                else if (string.IsNullOrEmpty(remaining))
                    nueva = newPrefix;
                else
                    nueva = newPrefix + " — " + remaining;

                // Actualizar primero el TextBox visual (el binding no notificará el cambio desde el DTO)
                try
                {
                    if (this.FindName("DescripcionTextBox") is System.Windows.Controls.TextBox tb)
                    {
                        tb.Text = nueva ?? string.Empty;
                        tb.CaretIndex = tb.Text.Length;
                    }
                }
                catch { }
                // Luego sincronizar el DTO
                Seguimiento.Descripcion = nueva;
                lastGeneratedChecklist = newPrefix;
            }
            catch
            {
                // Silenciar errores no críticos
            }
        }

        private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
        {
            if (this.Owner == null) return;

            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Mantener maximizada para que el overlay cubra toda la pantalla
                    this.WindowState = WindowState.Maximized;
                }
                catch
                {
                    this.WindowState = WindowState.Maximized;
                }
            });
        }

        private void SeguimientoDialog_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
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
