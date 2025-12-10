using ClosedXML.Excel;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.Utilities
{
    /// <summary>
    /// Utilitarios para representar estados de seguimiento en texto y colores para Excel.
    /// Centraliza la lógica usada por múltiples ViewModels/Services.
    /// </summary>
    public static class EstadoSeguimientoUtils
    {        public static string EstadoToTexto(EstadoSeguimientoMantenimiento estado)
        {
            return estado switch
            {
                EstadoSeguimientoMantenimiento.RealizadoEnTiempo => "Realizado en Tiempo",
                EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => "Realizado fuera de tiempo",
                EstadoSeguimientoMantenimiento.Atrasado => "Atrasado",
                EstadoSeguimientoMantenimiento.NoRealizado => "No Realizado",
                EstadoSeguimientoMantenimiento.Pendiente => "Pendiente",
                EstadoSeguimientoMantenimiento.Correctivo => "Correctivo",
                _ => "-"
            };
        }

        public static XLColor XLColorFromEstado(EstadoSeguimientoMantenimiento estado)
        {
            return estado switch
            {                // #388E3C - Verde (Realizado en tiempo) - semántica: éxito/ok
                EstadoSeguimientoMantenimiento.RealizadoEnTiempo => XLColor.FromArgb(0x388E3C),

                // #FFB300 - Ámbar / Amarillo (Realizado fuera de tiempo) - semántica: atención/advertencia
                EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => XLColor.FromArgb(0xFFB300),

                // #A85B00 - Naranja / Ámbar oscuro (Atrasado) - semántica: intermedio entre advertencia y error
                EstadoSeguimientoMantenimiento.Atrasado => XLColor.FromArgb(0xA85B00),

                // #C80000 - Rojo (No realizado) - semántica: error/urgente
                EstadoSeguimientoMantenimiento.NoRealizado => XLColor.FromArgb(0xC80000),                // #B3E5FC - Celeste claro (Pendiente) - semántica: información / pendiente
                EstadoSeguimientoMantenimiento.Pendiente => XLColor.FromArgb(0xB3E5FC),

                // #7E57C2 - Morado (Correctivo) - semántica: mantenimiento no planificado, urgencia especial
                EstadoSeguimientoMantenimiento.Correctivo => XLColor.FromArgb(0x7E57C2),

                // Color por defecto para estados desconocidos
                _ => XLColor.Gray
            };
        }

        /// <summary>
        /// Obtiene el color XL para un tipo de mantenimiento (ej: Correctivo).
        /// </summary>
        public static XLColor XLColorFromTipo(TipoMantenimiento tipo)
        {
            return tipo switch
            {
                // #7E57C2 - Morado (Correctivo) - semántica: mantenimiento no planificado, urgencia especial
                TipoMantenimiento.Correctivo => XLColor.FromArgb(0x7E57C2),

                // #388E3C - Verde (Preventivo) - semántica: mantenimiento planificado
                TipoMantenimiento.Preventivo => XLColor.FromArgb(0x388E3C),

                // #2196F3 - Azul (Predictivo) - semántica: basado en predicciones/análisis
                TipoMantenimiento.Predictivo => XLColor.FromArgb(0x2196F3),

                // Color por defecto
                _ => XLColor.Gray
            };
        }

        /// <summary>
        /// Obtiene el texto descriptivo para un tipo de mantenimiento.
        /// </summary>
        public static string TipoToTexto(TipoMantenimiento tipo)
        {
            return tipo switch
            {
                TipoMantenimiento.Preventivo => "Preventivo",
                TipoMantenimiento.Correctivo => "Correctivo",
                TipoMantenimiento.Predictivo => "Predictivo",
                TipoMantenimiento.Otro => "Otro",
                _ => "-"
            };
        }
    }
}
