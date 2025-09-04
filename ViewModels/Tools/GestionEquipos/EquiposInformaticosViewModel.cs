using System.Collections.ObjectModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.DatabaseConnection;
using GestLog.Views.Tools.GestionEquipos;
using System.Windows;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public partial class EquiposInformaticosViewModel : ObservableObject
    {
        private readonly GestLogDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;

        public ObservableCollection<EquipoInformaticoEntity> ListaEquiposInformaticos { get; set; } = new();

        [ObservableProperty]
        private bool canCrearEquipo;
        [ObservableProperty]
        private bool canEditarEquipo;
        [ObservableProperty]
        private bool canDarDeBajaEquipo;
        [ObservableProperty]
        private bool canVerHistorial;
        [ObservableProperty]
        private bool canExportarDatos;

        public EquiposInformaticosViewModel(GestLogDbContext db, ICurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
            
            CargarEquipos();
        }

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }

        private void RecalcularPermisos()
        {
            CanCrearEquipo = _currentUser.HasPermission("EquiposInformaticos.CrearEquipo");
            CanEditarEquipo = _currentUser.HasPermission("EquiposInformaticos.EditarEquipo");
            CanDarDeBajaEquipo = _currentUser.HasPermission("EquiposInformaticos.DarDeBajaEquipo");
            CanVerHistorial = _currentUser.HasPermission("EquiposInformaticos.VerHistorial");
            CanExportarDatos = _currentUser.HasPermission("EquiposInformaticos.ExportarDatos");
        }

        private void CargarEquipos()
        {
            var equipos = _db.EquiposInformaticos.ToList();
            ListaEquiposInformaticos.Clear();
            foreach (var eq in equipos)
                ListaEquiposInformaticos.Add(eq);
        }

        [RelayCommand]
        private void VerDetalles(EquipoInformaticoEntity equipo)
        {
            // Aquí puedes abrir un diálogo o navegar a una vista de detalles
        }        
        [RelayCommand(CanExecute = nameof(CanCrearEquipo))]
        private void AgregarEquipo()
        {
            var ventana = new AgregarEquipoInformaticoView();
            var resultado = ventana.ShowDialog();
            if (resultado == true)
            {
                CargarEquipos();
            }
        }

        // Eliminar referencias a propiedades eliminadas de la entidad principal
        // public int? SlotsTotales { get; set; }
        // public int? SlotsUtilizados { get; set; }
        // public string? TipoRam { get; set; }
        // public int? CapacidadTotalRamGB { get; set; }
        // public int? CantidadDiscos { get; set; }
        // public int? CapacidadTotalDiscosGB { get; set; }
        // Si se requiere mostrar totales, calcularlos desde ListaRam y ListaDiscos
    }
}
