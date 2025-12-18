using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using System;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{
    /// <summary>
    /// ViewModel para la ventana de detalles de un mantenimiento correctivo
    /// Muestra información read-only del mantenimiento completado o cancelado
    /// </summary>
    public partial class DetallesMantenimientoViewModel : ObservableObject
    {
        /// <summary>
        /// Mantenimiento a mostrar en detalles
        /// </summary>
        [ObservableProperty]
        private MantenimientoCorrectivoDto? mantenimiento;

        /// <summary>
        /// Inicializa el ViewModel con los datos del mantenimiento
        /// </summary>
        public void InitializarMantenimiento(MantenimientoCorrectivoDto mantenimiento)
        {
            Mantenimiento = mantenimiento;
        }
    }
}
