using System.Windows;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Services.Core.Logging;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    /// <summary>
    /// Lógica de interacción para EquipoDialog.xaml
    /// </summary>
    public partial class EquipoDialog : Window
    {
        public EquipoDto Equipo { get; private set; }
        public bool IsEditMode { get; }

        public EquipoDialog(EquipoDto? equipo = null)
        {
            InitializeComponent();
            if (equipo != null)
            {
                // Modo edición: clonar para no modificar el original hasta guardar
                Equipo = new EquipoDto(equipo);
                Equipo.IsCodigoReadOnly = true;
                Equipo.IsCodigoEnabled = false;
                IsEditMode = true;
            }
            else
            {
                Equipo = new EquipoDto();
                Equipo.IsCodigoReadOnly = false;
                Equipo.IsCodigoEnabled = true;
                IsEditMode = false;
            }
            DataContext = new EquipoDialogViewModel(Equipo)
            {
                EstadosEquipo = System.Enum.GetValues(typeof(EstadoEquipo)) as EstadoEquipo[] ?? new EstadoEquipo[0],
                Sedes = System.Enum.GetValues(typeof(Sede)) as Sede[] ?? new Sede[0],
                FrecuenciasMantenimiento = System.Enum.GetValues(typeof(FrecuenciaMantenimiento)) as FrecuenciaMantenimiento[] ?? new FrecuenciaMantenimiento[0]
            };
        }

        private async void OnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var errores = new List<string>();
            // Validaciones obligatorias
            if (string.IsNullOrWhiteSpace(Equipo.Codigo))
                errores.Add("El código del equipo es obligatorio.");
            if (string.IsNullOrWhiteSpace(Equipo.Nombre))
                errores.Add("El nombre del equipo es obligatorio.");
            if (string.IsNullOrWhiteSpace(Equipo.Marca))
                errores.Add("La marca es obligatoria.");
            if (Equipo.Sede == null)
                errores.Add("La sede es obligatoria.");
            if (Equipo.Precio != null && Equipo.Precio < 0)
                errores.Add("El precio no puede ser negativo.");
            if (Equipo.FrecuenciaMtto == null)
                errores.Add("La frecuencia de mantenimiento es obligatoria.");
            if (Equipo.Estado == null)
                errores.Add("El estado es obligatorio.");
            if (Equipo.Estado == GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.DadoDeBaja && !Equipo.FechaBaja.HasValue)
                errores.Add("Debe indicar la fecha de baja si el equipo está dado de baja.");
            // Validación de unicidad de código solo en alta
            if (!IsEditMode && !string.IsNullOrWhiteSpace(Equipo.Codigo))
            {
                var service = LoggingService.GetService<GestLog.Modules.GestionMantenimientos.Interfaces.IEquipoService>();
                var existente = await service.GetByCodigoAsync(Equipo.Codigo);
                if (existente != null)
                    errores.Add($"Ya existe un equipo con el código '{Equipo.Codigo}'.");
            }
            if (errores.Count > 0)
            {
                System.Windows.MessageBox.Show(string.Join("\n", errores), "Errores de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void OnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public class EquipoDialogViewModel
        {
            public EquipoDto Equipo { get; set; }
            public IEnumerable<EstadoEquipo> EstadosEquipo { get; set; } = new EstadoEquipo[0];
            public IEnumerable<Sede> Sedes { get; set; } = new Sede[0];
            public IEnumerable<FrecuenciaMantenimiento> FrecuenciasMantenimiento { get; set; } = new FrecuenciaMantenimiento[0];
            // Proxy directo a las propiedades del EquipoDto para binding simple
            public string? Codigo { get => Equipo.Codigo; set => Equipo.Codigo = value; }
            public string? Nombre { get => Equipo.Nombre; set => Equipo.Nombre = value; }
            public string? Marca { get => Equipo.Marca; set => Equipo.Marca = value; }
            public EstadoEquipo? Estado { get => Equipo.Estado; set => Equipo.Estado = value; }
            public Sede? Sede { get => Equipo.Sede; set => Equipo.Sede = value; }
            public FrecuenciaMantenimiento? FrecuenciaMtto { get => Equipo.FrecuenciaMtto; set => Equipo.FrecuenciaMtto = value; }
            public decimal? Precio { get => Equipo.Precio; set => Equipo.Precio = value; }
            public string? Observaciones { get => Equipo.Observaciones; set => Equipo.Observaciones = value; }
            public DateTime? FechaRegistro { get => Equipo.FechaRegistro; set => Equipo.FechaRegistro = value; }
            public DateTime? FechaBaja { get => Equipo.FechaBaja; set => Equipo.FechaBaja = value; }
            public bool IsCodigoReadOnly { get => Equipo.IsCodigoReadOnly; set => Equipo.IsCodigoReadOnly = value; }
            public bool IsCodigoEnabled { get => Equipo.IsCodigoEnabled; set => Equipo.IsCodigoEnabled = value; }
            public string? Clasificacion { get => Equipo.Clasificacion; set => Equipo.Clasificacion = value; }
            public string? CompradoA { get => Equipo.CompradoA; set => Equipo.CompradoA = value; }
            public EquipoDialogViewModel(EquipoDto equipo)
            {
                Equipo = equipo;
            }
        }
    }
}
