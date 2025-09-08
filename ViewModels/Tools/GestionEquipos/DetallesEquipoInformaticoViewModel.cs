using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public class DetallesEquipoInformaticoViewModel : ObservableObject
    {
        private readonly GestLogDbContext? _db;
        public EquipoInformaticoEntity Equipo { get; }
        public ObservableCollection<SlotRamEntity> SlotsRam { get; }
        public ObservableCollection<DiscoEntity> Discos { get; }

        public DetallesEquipoInformaticoViewModel(EquipoInformaticoEntity equipo, GestLogDbContext? db)
        {
            _db = db; // puede ser null
            Equipo = equipo ?? throw new ArgumentNullException(nameof(equipo));

            // Intentar cargar colecciones relacionadas desde el DbContext (si se proporcionó y la entidad está siendo rastreada)
            if (_db != null)
            {
                try
                {
                    if (!_db.Entry(Equipo).Collection(e => e.SlotsRam).IsLoaded)
                        _db.Entry(Equipo).Collection(e => e.SlotsRam).Load();
                    if (!_db.Entry(Equipo).Collection(e => e.Discos).IsLoaded)
                        _db.Entry(Equipo).Collection(e => e.Discos).Load();
                }
                catch
                {
                    // Si falla (p. ej. entidad no rastreada), continuamos con lo que venga en 'equipo'
                }
            }

            SlotsRam = new ObservableCollection<SlotRamEntity>(Equipo.SlotsRam ?? new List<SlotRamEntity>());
            Discos = new ObservableCollection<DiscoEntity>(Equipo.Discos ?? new List<DiscoEntity>());

            // Actualizar cabeceras cuando cambien las colecciones
            SlotsRam.CollectionChanged += (s, e) => OnPropertyChanged(nameof(RamHeader));
            Discos.CollectionChanged += (s, e) => OnPropertyChanged(nameof(DiscoHeader));
        }

        public string RamHeader => $"Memoria RAM ({SlotsRam?.Count ?? 0})";
        public string DiscoHeader => $"Discos ({Discos?.Count ?? 0})";

        // Propiedades de conveniencia usadas en la vista (passthrough)
        public string Codigo => Equipo.Codigo;
        public string? NombreEquipo => Equipo.NombreEquipo;
        public string? UsuarioAsignado => Equipo.UsuarioAsignado;
        public string? Marca => Equipo.Marca;
        public string? Modelo => Equipo.Modelo;
        public string? SO => Equipo.SO;
        public string? SerialNumber => Equipo.SerialNumber;
        public string? Procesador => Equipo.Procesador;
        public string? CodigoAnydesk => Equipo.CodigoAnydesk;
        public decimal? Costo => Equipo.Costo;
        public DateTime? FechaCompra => Equipo.FechaCompra;
        public DateTime? FechaBaja => Equipo.FechaBaja;
        public string? Observaciones => Equipo.Observaciones;
        public DateTime FechaCreacion => Equipo.FechaCreacion;
        public DateTime? FechaModificacion => Equipo.FechaModificacion;
        // Propiedades formateadas para enlazar desde XAML (uso de cultura española)
        private static readonly CultureInfo SpanishCulture = new CultureInfo("es-ES");
        public string FechaCreacionFormatted => $"Creado: {Equipo.FechaCreacion.ToString("f", SpanishCulture)}";
        public string FechaModificacionFormatted => Equipo.FechaModificacion.HasValue ? $"Modificado: {Equipo.FechaModificacion.Value.ToString("g", SpanishCulture)}" : "Modificado: -";
        public string? Estado => Equipo.Estado;
        public string? Sede => Equipo.Sede;
    }
}
