using System;

namespace GestLog.Modules.GestionVehiculos.Interfaces.Dialog
{
    public interface IAppDialogService
    {
        /// <summary>
        /// Muestra un diálogo de confirmación (Sí/No). Retorna true si el usuario confirma.
        /// </summary>
        bool Confirm(string message, string title = "Confirmar");

        /// <summary>
        /// Muestra un diálogo de confirmación con icono de advertencia. Retorna true si el usuario confirma.
        /// </summary>
        bool ConfirmWarning(string message, string title = "Atención");

        /// <summary>
        /// Muestra un diálogo de error informativo.
        /// </summary>
        void ShowError(string message, string title = "Error");
    }
}
