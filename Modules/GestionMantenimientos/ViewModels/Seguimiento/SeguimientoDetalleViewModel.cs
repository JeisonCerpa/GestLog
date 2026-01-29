using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.ViewModels.Seguimiento
{
    /// <summary>
    /// ViewModel para visualizar detalles de un seguimiento (solo lectura).
    /// Presenta los datos de forma clara y estructurada.
    /// Gestiona la habilitación de edición/eliminación según estado y fecha.
    /// </summary>
    public partial class SeguimientoDetalleViewModel : ObservableObject
    {
        [ObservableProperty]
        private SeguimientoMantenimientoDto? seguimiento;

        [ObservableProperty]
        private string? sedeDisplayName;

        [ObservableProperty]
        private string? estadoDisplayName;

        [ObservableProperty]
        private string? tipoMantenimientoDisplayName;

        [ObservableProperty]
        private bool puedeEditarOEliminar;

        [ObservableProperty]
        private string? mensajeDeshabilitacion;

        // Propiedades para modo edición
        [ObservableProperty]
        private bool enModoEdicion;

        // Copias editables de los datos
        [ObservableProperty]
        private string? descripcionEditable;

        [ObservableProperty]
        private string? observacionesEditable;

        [ObservableProperty]
        private decimal? costoEditable;

        public SeguimientoDetalleViewModel(SeguimientoMantenimientoDto seguimientoDto)
        {
            if (seguimientoDto == null)
                throw new ArgumentNullException(nameof(seguimientoDto));

            Seguimiento = seguimientoDto;
            
            // Inicializar nombres de visualización
            ActualizarNombresVisuales();
            
            // Verificar si puede editar/eliminar
            ActualizarPermisoEdicionEliminacion();
        }

        private void ActualizarNombresVisuales()
        {
            if (Seguimiento == null)
                return;

            // Mapear sede a nombre legible
            SedeDisplayName = Seguimiento.Sede?.ToString() ?? "No especificada";

            // Mapear estado a nombre legible
            EstadoDisplayName = Seguimiento.Estado.ToString() switch
            {
                "RealizadoEnTiempo" => "✅ Realizado en Tiempo",
                "RealizadoFueraDeTiempo" => "⏱️ Realizado Fuera de Tiempo",
                "Atrasado" => "⚠️ Atrasado",
                "NoRealizado" => "❌ No Realizado",
                "Pendiente" => "⏸️ Pendiente",
                _ => Seguimiento.Estado.ToString()
            };

            // Mapear tipo de mantenimiento
            TipoMantenimientoDisplayName = Seguimiento.TipoMtno?.ToString() ?? "No especificado";
        }

        /// <summary>
        /// Valida si el seguimiento puede ser editado o eliminado.
        /// Usa la misma lógica que PuedeRegistrarMantenimiento:
        /// - Semana actual o anterior, hasta el viernes siguiente
        /// </summary>
        private void ActualizarPermisoEdicionEliminacion()
        {
            if (Seguimiento == null)
            {
                PuedeEditarOEliminar = false;
                MensajeDeshabilitacion = "Seguimiento no disponible";
                return;
            }

            // Usar la misma lógica que PuedeRegistrarMantenimiento
            int semanaActual = GetSemanaActual();
            int anioActual = GetAnioActual();
            var hoy = DateTime.Now;
            var primerDiaSemana = FirstDateOfWeekISO8601(Seguimiento.Anio, Seguimiento.Semana);
            var viernesSiguiente = primerDiaSemana.AddDays(11); // viernes siguiente

            // Permitir edición/eliminación en la semana actual y la anterior, hasta el viernes de la semana siguiente
            if (Seguimiento.Anio == anioActual && (Seguimiento.Semana == semanaActual || Seguimiento.Semana == semanaActual - 1))
            {
                if (hoy.Date <= viernesSiguiente.Date)
                {
                    PuedeEditarOEliminar = true;
                    MensajeDeshabilitacion = null;
                    return;
                }
            }

            // Si no cumple la condición
            PuedeEditarOEliminar = false;
            MensajeDeshabilitacion = "No se puede editar/eliminar seguimientos de semanas antiguas (máximo semana actual ± 1)";
        }

        /// <summary>
        /// Obtiene la semana actual según ISO 8601
        /// </summary>
        private int GetSemanaActual()
        {
            var hoy = DateTime.Now;
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            return cal.GetWeekOfYear(hoy, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Obtiene el año actual
        /// </summary>
        private int GetAnioActual()
        {
            return DateTime.Now.Year;
        }

        /// <summary>
        /// Obtiene el primer día de la semana según ISO 8601
        /// </summary>
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

        /// <summary>
        /// Entra en modo edición: copia los datos actuales a propiedades editables
        /// </summary>
        public void EntrarModoEdicion()
        {
            if (Seguimiento == null)
                return;

            // Copiar datos actuales a propiedades editables
            DescripcionEditable = Seguimiento.Descripcion;
            ObservacionesEditable = Seguimiento.Observaciones;
            CostoEditable = Seguimiento.Costo;

            EnModoEdicion = true;
        }

        /// <summary>
        /// Sale del modo edición sin guardar cambios
        /// </summary>
        public void SalirModoEdicion()
        {
            EnModoEdicion = false;
            LimpiarCopias();
        }

        /// <summary>
        /// Guarda los cambios editables en el seguimiento y sale del modo edición
        /// </summary>
        public void GuardarCambios()
        {
            if (Seguimiento == null)
                return;

            // Actualizar datos del seguimiento
            Seguimiento.Descripcion = DescripcionEditable;
            ObservacionesEditable = Seguimiento.Observaciones;
            CostoEditable = CostoEditable;

            EnModoEdicion = false;
            LimpiarCopias();
        }

        /// <summary>
        /// Limpia las copias editables
        /// </summary>
        private void LimpiarCopias()
        {
            DescripcionEditable = null;
            ObservacionesEditable = null;
            CostoEditable = null;
        }

        /// <summary>
        /// Retorna el color de fondo según el estado del seguimiento
        /// </summary>
        public string GetEstadoBackgroundColor()
        {
            return Seguimiento?.Estado.ToString() switch
            {
                "RealizadoEnTiempo" => "#E8F5E9",           // Verde claro
                "RealizadoFueraDeTiempo" => "#FFF3E0",      // Ámbar claro
                "Atrasado" => "#FFEBEE",                    // Rojo claro
                "NoRealizado" => "#FFEBEE",                 // Rojo claro
                "Pendiente" => "#F5F5F5",                   // Gris claro
                _ => "#FFFFFF"                              // Blanco
            };
        }

        /// <summary>
        /// Retorna el color de texto según el estado del seguimiento
        /// </summary>
        public string GetEstadoForegroundColor()
        {
            return Seguimiento?.Estado.ToString() switch
            {
                "RealizadoEnTiempo" => "#118938",           // Verde
                "RealizadoFueraDeTiempo" => "#F59E0B",      // Ámbar
                "Atrasado" => "#C0392B",                    // Rojo
                "NoRealizado" => "#C0392B",                 // Rojo
                "Pendiente" => "#706F6F",                   // Gris
                _ => "#504F4E"                              // Gris oscuro
            };
        }
    }
}
