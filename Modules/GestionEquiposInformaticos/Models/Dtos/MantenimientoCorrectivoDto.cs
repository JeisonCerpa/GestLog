using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Dtos
{
    /// <summary>
    /// DTO para mantenimientos correctivos (reactivos) de equipos e periféricos
    /// Soporta tanto equipos informáticos como periféricos de forma polimórfica
    /// </summary>
    public partial class MantenimientoCorrectivoDto : ObservableObject
    {
        /// <summary>
        /// Identificador único del mantenimiento correctivo
        /// </summary>
        [ObservableProperty]
        private int? _id;

        /// <summary>
        /// Tipo de entidad: "Equipo" o "Periferico"
        /// </summary>
        [ObservableProperty]
        private string _tipoEntidad = "Equipo";

        /// <summary>
        /// Código del equipo o periférico (unificado)
        /// </summary>
        [ObservableProperty]
        private string? _codigo;

        /// <summary>
        /// Fecha en que ocurrió la falla
        /// </summary>
        [ObservableProperty]
        private DateTime _fechaFalla = DateTime.Now;

        /// <summary>
        /// Descripción detallada de la falla reportada
        /// </summary>
        [ObservableProperty]
        private string _descripcionFalla = string.Empty;

        /// <summary>
        /// Nombre o razón social del proveedor asignado para reparación
        /// </summary>
        [ObservableProperty]
        private string? _proveedorAsignado;

        /// <summary>
        /// Estado actual del mantenimiento correctivo
        /// </summary>
        [ObservableProperty]
        private EstadoMantenimientoCorrectivo _estado = EstadoMantenimientoCorrectivo.Pendiente;

        /// <summary>
        /// Fecha en que inició la reparación
        /// </summary>
        [ObservableProperty]
        private DateTime? _fechaInicio;

        /// <summary>
        /// Fecha en que se completó la reparación
        /// </summary>
        [ObservableProperty]
        private DateTime? _fechaCompletado;

        /// <summary>
        /// Observaciones adicionales sobre el mantenimiento
        /// </summary>
        [ObservableProperty]
        private string? _observaciones;        /// <summary>
        /// Costo total de la reparación (si aplica)
        /// </summary>
        [ObservableProperty]
        private decimal? _costoReparacion;

        /// <summary>
        /// Período de garantía en días (ej: 90, 180, 365)
        /// </summary>
        [ObservableProperty]
        private int? _periodoGarantia;

        /// <summary>
        /// Fecha y hora en que se registró este mantenimiento
        /// </summary>
        [ObservableProperty]
        private DateTime _fechaRegistro = DateTime.Now;

        /// <summary>
        /// Fecha de última actualización del registro
        /// </summary>
        [ObservableProperty]
        private DateTime _fechaActualizacion = DateTime.Now;

        /// <summary>
        /// Información adicional de visualización: nombre del equipo o periférico
        /// </summary>
        [ObservableProperty]
        private string? _nombreEntidad;

        /// <summary>
        /// Información adicional de visualización: usuario asignado
        /// </summary>
        [ObservableProperty]
        private string? _usuarioAsignado;
    }
}
