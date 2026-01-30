using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using System;
using System.Collections.Generic;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Perifericos
{
    public partial class PerifericoDetalleViewModel : ObservableObject
    {
        [ObservableProperty]
        private string codigo = string.Empty;

        [ObservableProperty]
        private string nombre = string.Empty;

        [ObservableProperty]
        private string tipo = string.Empty;

        [ObservableProperty]
        private string sede = string.Empty;

        [ObservableProperty]
        private string asignacion = string.Empty;

        [ObservableProperty]
        private string estado = string.Empty;

        [ObservableProperty]
        private string marca = string.Empty;

        [ObservableProperty]
        private string modelo = string.Empty;

        [ObservableProperty]
        private string serialNumber = string.Empty;

        [ObservableProperty]
        private DateTime? fechaAdquisicion;

        [ObservableProperty]
        private string observaciones = string.Empty;

        [ObservableProperty]
        private string? usuarioAsignadoAnterior;

        [ObservableProperty]
        private string? codigoEquipoAsignadoAnterior;

        [ObservableProperty]
        private IEnumerable<string> detallesAdicionales = new List<string>();

        [ObservableProperty]
        private bool canEditar;

        public PerifericoDetalleViewModel(PerifericoEquipoInformaticoDto dto, bool canEdit = false)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            Codigo = dto.Codigo ?? string.Empty;
            Nombre = dto.Dispositivo ?? string.Empty;
            Tipo = string.Empty;
            Sede = ToFriendlyName(dto.Sede.ToString());
            Asignacion = dto.TextoAsignacion ?? string.Empty;
            Estado = dto.EstadoDescripcion ?? dto.Estado.ToString();
            Marca = dto.Marca ?? string.Empty;
            Modelo = dto.Modelo ?? string.Empty;
            SerialNumber = dto.Serial ?? string.Empty;
            FechaAdquisicion = dto.FechaCompra;
            Observaciones = dto.Observaciones ?? string.Empty;
            UsuarioAsignadoAnterior = dto.UsuarioAsignadoAnterior;
            CodigoEquipoAsignadoAnterior = dto.CodigoEquipoAsignadoAnterior;
            CanEditar = canEdit;

            // Detalles adicionales útiles para mostrar en la sección de lista
            var detalles = new List<string>();
            detalles.Add($"Código: {Codigo}");
            if (!string.IsNullOrEmpty(dto.CostoFormatted)) detalles.Add($"Costo: {dto.CostoFormatted}");
            if (!string.IsNullOrEmpty(dto.FechaCompraFormatted)) detalles.Add($"Fecha compra: {dto.FechaCompraFormatted}");
            if (!string.IsNullOrEmpty(Asignacion)) detalles.Add($"Asignación: {Asignacion}");
            if (!string.IsNullOrEmpty(Observaciones)) detalles.Add($"Observaciones: {Observaciones}");

            DetallesAdicionales = detalles;
        }

        private static string ToFriendlyName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            // Insertar espacios antes de mayúsculas (PascalCase -> 'Administrativa Barranquilla')
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(raw[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
