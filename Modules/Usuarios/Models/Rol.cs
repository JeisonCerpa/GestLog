using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace GestLog.Modules.Usuarios.Models
{
    public class Rol : INotifyPropertyChanged
    {
        public Guid IdRol { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        // Permisos asignados al rol, por módulo
        public List<Permiso> Permisos { get; set; } = new();

        private bool _isSelected;
        [NotMapped]
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
