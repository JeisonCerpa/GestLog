using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using System.Linq;

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

        /// <summary>
        /// Intenta dar de baja el equipo: si hay DbContext persiste los cambios; si no, actualiza la entidad en memoria.
        /// Devuelve true si la operación se completó (aunque sea solo en memoria), false si hubo error fatal.
        /// </summary>
        public bool DarDeBaja(out string mensaje, out bool persistedToDb)
        {
            mensaje = string.Empty;
            persistedToDb = false;
            try
            {
                // Si hay DbContext, intentar localizar la entidad rastreada en la BD
                if (_db != null)
                {
                    var equipoRef = _db.EquiposInformaticos.FirstOrDefault(e => e.Codigo == Equipo.Codigo);
                    if (equipoRef != null)
                    {
                        equipoRef.FechaBaja = DateTime.Now;
                        equipoRef.Estado = "Dado de baja";
                        equipoRef.FechaModificacion = DateTime.Now;
                        _db.SaveChanges();

                        // Actualizar la entidad en memoria para mantener consistencia
                        Equipo.FechaBaja = equipoRef.FechaBaja;
                        Equipo.Estado = equipoRef.Estado;
                        Equipo.FechaModificacion = equipoRef.FechaModificacion;

                        // Notificar al resto de la app
                        try { WeakReferenceMessenger.Default.Send(new EquiposActualizadosMessage()); } catch { }

                        mensaje = "Equipo dado de baja correctamente.";
                        persistedToDb = true;
                        return true;
                    }
                }

                // Fallback: actualizar solo en memoria
                Equipo.FechaBaja = DateTime.Now;
                Equipo.Estado = "Dado de baja";
                Equipo.FechaModificacion = DateTime.Now;

                mensaje = "No se pudo acceder a la base de datos, se actualizó el estado en memoria.";
                persistedToDb = false;
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al dar de baja: {ex.Message}";
                persistedToDb = false;
                return false;
            }
        }
    }
}
