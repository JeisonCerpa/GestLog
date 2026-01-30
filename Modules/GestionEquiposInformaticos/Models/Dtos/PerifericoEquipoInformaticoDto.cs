using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Modules.GestionEquiposInformaticos.Models.Dtos
{
    /// <summary>
    /// DTO para periféricos de equipos informáticos con binding bidireccional
    /// </summary>
    public partial class PerifericoEquipoInformaticoDto : ObservableObject
    {
        [ObservableProperty]
        private int? _id;

        [ObservableProperty]
        private string _codigo = string.Empty;

        [ObservableProperty]
        private string _dispositivo = string.Empty;

        [ObservableProperty]
        private DateTime? _fechaCompra;

        [ObservableProperty]
        private decimal? _costo;

        [ObservableProperty]
        private string? _marca;

        [ObservableProperty]
        private string? _modelo;

        [ObservableProperty]
        private string? _serial;

        [ObservableProperty]
        private string? _codigoEquipoAsignado;

        [ObservableProperty]
        private string? _usuarioAsignado;

        [ObservableProperty]
        private string? _usuarioAsignadoAnterior;

        [ObservableProperty]
        private string? _codigoEquipoAsignadoAnterior;

        [ObservableProperty]
        private SedePeriferico _sede = SedePeriferico.AdministrativaBarranquilla;

        [ObservableProperty]
        private EstadoPeriferico _estado = EstadoPeriferico.EnUso;

        [ObservableProperty]
        private string? _observaciones;

        [ObservableProperty]
        private DateTime _fechaCreacion = DateTime.Now;

        [ObservableProperty]
        private DateTime _fechaModificacion = DateTime.Now;

        /// <summary>
        /// Nombre del equipo asignado (para mostrar en la UI)
        /// </summary>
        [ObservableProperty]
        private string? _nombreEquipoAsignado;

        /// <summary>
        /// Indica si el periférico está asignado a algo (equipo o usuario)
        /// </summary>
        // Considerar también NombreEquipoAsignado para permitir mostrar asignación temporal antes de persistir el equipo
        public bool EstaAsignado => !string.IsNullOrEmpty(CodigoEquipoAsignado) || !string.IsNullOrEmpty(UsuarioAsignado) || !string.IsNullOrEmpty(NombreEquipoAsignado);

        /// <summary>
        /// Texto descriptivo de la asignación
        /// </summary>
        public string TextoAsignacion
        {
            get
            {
                // Mostrar como en el ComboBox: "Usuario (CodigoEquipo NombreEquipo)" cuando haya usuario y equipo
                string teamInfo = string.Empty;
                if (!string.IsNullOrEmpty(CodigoEquipoAsignado) && !string.IsNullOrEmpty(NombreEquipoAsignado))
                    teamInfo = $"{CodigoEquipoAsignado} {NombreEquipoAsignado}";
                else if (!string.IsNullOrEmpty(CodigoEquipoAsignado))
                    teamInfo = CodigoEquipoAsignado;
                else if (!string.IsNullOrEmpty(NombreEquipoAsignado))
                    teamInfo = NombreEquipoAsignado;

                if (!string.IsNullOrEmpty(UsuarioAsignado))
                {
                    return string.IsNullOrEmpty(teamInfo) ? UsuarioAsignado : $"{UsuarioAsignado} ({teamInfo})";
                }

                if (!string.IsNullOrEmpty(teamInfo))
                    return teamInfo;

                return "Sin asignar";
            }
        }

        /// <summary>
        /// Fecha de compra formateada
        /// </summary>
        public string FechaCompraFormatted => FechaCompra?.ToString("dd/MM/yyyy") ?? "No especificada";

        /// <summary>
        /// Costo formateado como moneda
        /// </summary>
        public string CostoFormatted => Costo?.ToString("C0") ?? "No especificado";

        /// <summary>
        /// Descripción del estado para mostrar en UI
        /// </summary>
        public string EstadoDescripcion
        {
            get
            {
                return Estado switch
                {
                    EstadoPeriferico.EnUso => "En Uso",
                    EstadoPeriferico.AlmacenadoFuncionando => "Almacenado (Funcionando)",
                    EstadoPeriferico.DadoDeBaja => "Dado de Baja",
                    _ => Estado.ToString()
                };
            }
        }

        /// <summary>
        /// Orden numérico del estado para permitir ordenación correcta en DataGrid
        /// </summary>
        public int EstadoOrden
        {
            get
            {
                return Estado switch
                {
                    EstadoPeriferico.EnUso => 1,
                    EstadoPeriferico.AlmacenadoFuncionando => 2,
                    EstadoPeriferico.DadoDeBaja => 3,
                    _ => 99
                };
            }
        }

        // Partial methods generados por [ObservableProperty] se pueden usar para propagar cambios dependientes
        partial void OnCodigoEquipoAsignadoChanged(string? value)
        {
            OnPropertyChanged(nameof(EstaAsignado));
            OnPropertyChanged(nameof(TextoAsignacion));
        }

        partial void OnUsuarioAsignadoChanged(string? value)
        {
            OnPropertyChanged(nameof(EstaAsignado));
            OnPropertyChanged(nameof(TextoAsignacion));
        }

        partial void OnNombreEquipoAsignadoChanged(string? value)
        {
            OnPropertyChanged(nameof(EstaAsignado));
            OnPropertyChanged(nameof(TextoAsignacion));
        }

        partial void OnEstadoChanged(EstadoPeriferico value)
        {
            OnPropertyChanged(nameof(EstadoDescripcion));
        }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public PerifericoEquipoInformaticoDto() { }        /// <summary>
        /// Constructor de copia
        /// </summary>
        public PerifericoEquipoInformaticoDto(PerifericoEquipoInformaticoDto other)
        {
            Id = other.Id;
            Codigo = other.Codigo;
            Dispositivo = other.Dispositivo;
            FechaCompra = other.FechaCompra;
            Costo = other.Costo;
            Marca = other.Marca;
            Modelo = other.Modelo;
            Serial = other.Serial;
            CodigoEquipoAsignado = other.CodigoEquipoAsignado;
            UsuarioAsignado = other.UsuarioAsignado;
            UsuarioAsignadoAnterior = other.UsuarioAsignadoAnterior;
            CodigoEquipoAsignadoAnterior = other.CodigoEquipoAsignadoAnterior;
            Sede = other.Sede;
            Estado = other.Estado;
            Observaciones = other.Observaciones;
            FechaCreacion = other.FechaCreacion;
            FechaModificacion = other.FechaModificacion;
            NombreEquipoAsignado = other.NombreEquipoAsignado;
        }        /// <summary>
        /// Constructor desde Entity
        /// </summary>
        public PerifericoEquipoInformaticoDto(Entities.PerifericoEquipoInformaticoEntity entity)
        {
            Id = null; // Entity usa Codigo como PK, no Id
            Codigo = entity.Codigo;
            Dispositivo = entity.Dispositivo;
            FechaCompra = entity.FechaCompra;
            Costo = entity.Costo;
            Marca = entity.Marca;
            Modelo = entity.Modelo;
            Serial = entity.SerialNumber; // Entity usa SerialNumber
            CodigoEquipoAsignado = entity.CodigoEquipoAsignado;
            UsuarioAsignado = entity.UsuarioAsignado;
            UsuarioAsignadoAnterior = entity.UsuarioAsignadoAnterior;
            CodigoEquipoAsignadoAnterior = entity.CodigoEquipoAsignadoAnterior;
            Sede = entity.Sede;
            Estado = entity.Estado;
            Observaciones = entity.Observaciones;
            FechaCreacion = entity.FechaCreacion;
            FechaModificacion = entity.FechaModificacion;
            NombreEquipoAsignado = entity.EquipoAsignado?.NombreEquipo;
        }
    }
}
