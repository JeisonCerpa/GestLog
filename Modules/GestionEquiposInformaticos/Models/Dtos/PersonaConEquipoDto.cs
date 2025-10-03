using System;
using System.Text;
using GestLog.Modules.Personas.Models;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Dtos
{
    /// <summary>
    /// DTO que representa una persona con información de su equipo asignado
    /// </summary>
    public class PersonaConEquipoDto
    {
        /// <summary>
        /// ID de la persona
        /// </summary>
        public Guid PersonaId { get; set; }

        /// <summary>
        /// Nombre completo de la persona
        /// </summary>
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>
        /// Código del equipo asignado
        /// </summary>
        public string CodigoEquipo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del equipo asignado
        /// </summary>
        public string NombreEquipo { get; set; } = string.Empty;

        /// <summary>
        /// Texto de display que combina el nombre de la persona con información del equipo
        /// Formato: "Nombre Completo (Código y Nombre de equipo)"
        /// </summary>
        public string DisplayText => $"{NombreCompleto} ({CodigoEquipo} {NombreEquipo})";        /// <summary>
        /// Texto normalizado para filtrado (sin acentos, minúsculas)
        /// </summary>
        public string TextoNormalizado { get; set; } = string.Empty;

        /// <summary>
        /// Override para que ComboBox.Text (cuando usa SelectedItem.ToString()) muestre el DisplayText (nombre + equipo)
        /// </summary>
        public override string ToString() => DisplayText;
    }
}
