using GestLog.Modules.GestionVehiculos.Models.Enums;
using System.Windows.Media;
using System.Windows;

namespace GestLog.Modules.GestionVehiculos.Utilities
{
    /// <summary>
    /// Utilidades para conversiones de estado de vehículos a colores y textos
    /// Sigue la paleta corporativa de GestLog
    /// </summary>
    public static class VehicleStateUtils
    {
        // Colores corporativos
        private const string GreenColor = "#2B8E3F";              // Activo
        private const string AmberColor = "#F9B233";              // En mantenimiento
        private const string OrangeColor = "#A85B00";             // En reparación
        private const string LightGrayColor = "#EDEDED";          // Dado de baja
        private const string MediumGrayColor = "#9E9E9E";         // Inactivo

        /// <summary>
        /// Obtiene el color correspondiente al estado del vehículo
        /// </summary>
        public static System.Windows.Media.Brush GetColorForState(VehicleState state)
        {
            return state switch
            {
                VehicleState.Activo => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(GreenColor)),
                VehicleState.EnMantenimiento => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(AmberColor)),
                VehicleState.DadoDeBaja => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(LightGrayColor)),
                VehicleState.Inactivo => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(MediumGrayColor)),
                _ => new SolidColorBrush(System.Windows.Media.Colors.Gray)
            };
        }

        /// <summary>
        /// Obtiene el color en formato string hex
        /// </summary>
        public static string GetColorHexForState(VehicleState state)
        {
            return state switch
            {
                VehicleState.Activo => GreenColor,
                VehicleState.EnMantenimiento => AmberColor,
                VehicleState.DadoDeBaja => LightGrayColor,
                VehicleState.Inactivo => MediumGrayColor,
                _ => "#999999"
            };
        }

        /// <summary>
        /// Obtiene la opacidad correspondiente al estado (para visual feedback)
        /// </summary>
        public static double GetOpacityForState(VehicleState state)
        {
            return state switch
            {
                VehicleState.Activo => 1.0,
                VehicleState.EnMantenimiento => 0.95,
                VehicleState.DadoDeBaja => 0.6,          // Muy transparente
                VehicleState.Inactivo => 0.85,
                _ => 0.8
            };
        }

        /// <summary>
        /// Obtiene el texto legible del estado
        /// </summary>
        public static string GetDisplayText(VehicleState state)
        {
            return state switch
            {
                VehicleState.Activo => "Activo",
                VehicleState.EnMantenimiento => "En mantenimiento",
                VehicleState.DadoDeBaja => "Dado de baja",
                VehicleState.Inactivo => "Inactivo",
                _ => "Desconocido"
            };
        }

        /// <summary>
        /// Indica si el texto debe mostrarse con tachado (para dado de baja)
        /// </summary>
        public static System.Windows.TextDecorationCollection? GetTextDecoration(VehicleState state)
        {
            return state == VehicleState.DadoDeBaja 
                ? System.Windows.TextDecorations.Strikethrough 
                : null;
        }

        /// <summary>
        /// Obtiene un icono/emoji representativo del estado
        /// </summary>
        public static string GetIconForState(VehicleState state)
        {
            return state switch
            {
                VehicleState.Activo => "✓",
                VehicleState.EnMantenimiento => "⚙",
                VehicleState.DadoDeBaja => "✗",
                VehicleState.Inactivo => "⏸",
                _ => "?"
            };
        }
    }
}
