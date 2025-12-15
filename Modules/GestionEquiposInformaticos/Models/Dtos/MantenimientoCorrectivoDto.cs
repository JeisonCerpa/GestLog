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
        /// ID del equipo informático (si aplica)
        /// </summary>
        [ObservableProperty]
        private int? _equipoInformaticoId;

        /// <summary>
        /// Código del equipo informático (si aplica)
        /// </summary>
        [ObservableProperty]
        private string? _equipoInformaticoCodigo;

        /// <summary>
        /// ID del periférico (si aplica)
        /// </summary>
        [ObservableProperty]
        private int? _perifericoEquipoInformaticoId;

        /// <summary>
        /// Código del periférico (si aplica)
        /// </summary>
        [ObservableProperty]
        private string? _perifericoEquipoInformaticoCodigo;

        /// <summary>
        /// Fecha en que ocurrió la falla
        /// </summary>
        [ObservableProperty]
        private DateTime _fechaFalla = DateTime.Now;

        /// <summary>
        /// Hora aproximada en que ocurrió la falla
        /// </summary>
        [ObservableProperty]
        private string? _horaFalla;

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
        private string? _observaciones;

        /// <summary>
        /// Indica si este registro ha sido dado de baja lógica
        /// </summary>
        [ObservableProperty]
        private bool _dadoDeBaja = false;

        /// <summary>
        /// ID del usuario que registró el mantenimiento correctivo
        /// </summary>
        [ObservableProperty]
        private int? _usuarioRegistroId;

        /// <summary>
        /// Código del usuario que registró el mantenimiento
        /// </summary>
        [ObservableProperty]
        private string? _usuarioRegistro;

        /// <summary>
        /// Fecha y hora en que se registró este mantenimiento
        /// </summary>
        [ObservableProperty]
        private DateTime _fechaRegistro = DateTime.Now;

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        [ObservableProperty]
        private DateTime _fechaCreacion = DateTime.Now;

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
