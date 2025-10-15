using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using System;
using System.Collections.Generic;

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public class PerifericoDetalleViewModel : ObservableObject
    {
        public PerifericoDetalleViewModel(PerifericoEquipoInformaticoDto dto, bool canEdit = false)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            Codigo = dto.Codigo ?? string.Empty;
            Nombre = dto.Dispositivo ?? string.Empty;
            // Mantener Tipo vacío (no hay campo explícito en el DTO)
            Tipo = string.Empty;
            // Mostrar la sede en una propiedad dedicada
            Sede = ToFriendlyName(dto.Sede.ToString());
            // Mostrar la asignación (usuario/equipo) en el campo de detalle
            Asignacion = dto.TextoAsignacion ?? string.Empty;
            // Usar la descripción legible del estado
            Estado = dto.EstadoDescripcion ?? dto.Estado.ToString();
            Marca = dto.Marca ?? string.Empty;
            Modelo = dto.Modelo ?? string.Empty;
            SerialNumber = dto.Serial ?? string.Empty;
            FechaAdquisicion = dto.FechaCompra;
            Observaciones = dto.Observaciones ?? string.Empty;
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

        public string Codigo { get; }
        public string Nombre { get; }
        public string Tipo { get; }
        public string Sede { get; }
        public string Asignacion { get; }
        public string Estado { get; }
        public string Marca { get; }
        public string Modelo { get; }
        public string SerialNumber { get; }
        public DateTime? FechaAdquisicion { get; }
        public string Observaciones { get; }
        public IEnumerable<string> DetallesAdicionales { get; }
        public bool CanEditar { get; }
    }
}