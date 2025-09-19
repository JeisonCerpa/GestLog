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
    {        private readonly GestLogDbContext? _db;
        public EquipoInformaticoEntity Equipo { get; }
        public ObservableCollection<SlotRamEntity> SlotsRam { get; }
        public ObservableCollection<DiscoEntity> Discos { get; }
        public ObservableCollection<ConexionEntity> Conexiones { get; }
        public ObservableCollection<PerifericoEquipoInformaticoEntity> Perifericos { get; }

        public DetallesEquipoInformaticoViewModel(EquipoInformaticoEntity equipo, GestLogDbContext? db)
        {
            _db = db; // puede ser null
            Equipo = equipo ?? throw new ArgumentNullException(nameof(equipo));

            // Intentar cargar colecciones relacionadas desde el DbContext (si se proporcionó y la entidad está siendo rastreada)
            if (_db != null)
            {
                try
                {
                    var entry = _db.Entry(Equipo);

                    // Solo cargar automáticamente si la entidad está siendo rastreada para evitar duplicados
                    // cuando la entidad ya trae colecciones (p. ej. proviene de AsNoTracking) o cuando
                    // reconstruimos la entidad desde el VM de edición (ya contiene listas).
                    if (entry.State != EntityState.Detached)
                    {                        if (!entry.Collection(e => e.SlotsRam).IsLoaded && (Equipo.SlotsRam == null || !Equipo.SlotsRam.Any()))
                            entry.Collection(e => e.SlotsRam).Load();

                        if (!entry.Collection(e => e.Discos).IsLoaded && (Equipo.Discos == null || !Equipo.Discos.Any()))
                            entry.Collection(e => e.Discos).Load();

                        if (!entry.Collection(e => e.Conexiones).IsLoaded && (Equipo.Conexiones == null || !Equipo.Conexiones.Any()))
                            entry.Collection(e => e.Conexiones).Load();
                    }
                }
                catch
                {
                    // Si falla (p. ej. entidad no rastreada), continuamos con lo que venga en 'equipo'
                }
            }            SlotsRam = new ObservableCollection<SlotRamEntity>(Equipo.SlotsRam ?? new List<SlotRamEntity>());
            Discos = new ObservableCollection<DiscoEntity>(Equipo.Discos ?? new List<DiscoEntity>());
            Conexiones = new ObservableCollection<ConexionEntity>(Equipo.Conexiones ?? new List<ConexionEntity>());

            // Cargar periféricos asignados a este equipo
            var perifericosAsignados = new List<PerifericoEquipoInformaticoEntity>();
            if (_db != null)
            {
                try
                {
                    perifericosAsignados = _db.PerifericosEquiposInformaticos
                        .Where(p => p.CodigoEquipoAsignado == Equipo.Codigo)
                        .ToList();
                }
                catch
                {
                    // Si falla la consulta, continuar con lista vacía
                }
            }
            Perifericos = new ObservableCollection<PerifericoEquipoInformaticoEntity>(perifericosAsignados);

            // Actualizar cabeceras cuando cambien las colecciones
            SlotsRam.CollectionChanged += (s, e) => OnPropertyChanged(nameof(RamHeader));
            Discos.CollectionChanged += (s, e) => OnPropertyChanged(nameof(DiscoHeader));
            Conexiones.CollectionChanged += (s, e) => OnPropertyChanged(nameof(ConexionesHeader));
            Perifericos.CollectionChanged += (s, e) => OnPropertyChanged(nameof(PerifericosHeader));
        }        public string RamHeader => $"Memoria RAM ({SlotsRam?.Count ?? 0})";
        public string DiscoHeader => $"Discos ({Discos?.Count ?? 0})";
        public string ConexionesHeader => $"Conexiones de Red ({Conexiones?.Count ?? 0})";
        public string PerifericosHeader => $"Periféricos Asignados ({Perifericos?.Count ?? 0})";

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
            // Delegar a SetEstado para centralizar reglas de negocio y persistencia.
            try
            {
                var result = SetEstado("Dado de baja", out mensaje, out persistedToDb);
                return result;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al dar de baja: {ex.Message}";
                persistedToDb = false;
                return false;
            }
        }

        /// <summary>
        /// Establece un nuevo estado al equipo. Si el estado es 'Activo', limpia FechaBaja; si es 'Dado de baja', establece FechaBaja a ahora.
        /// Persiste en DB si hay DbContext.
        /// </summary>
        public bool SetEstado(string nuevoEstado, out string mensaje, out bool persistedToDb)
        {
            mensaje = string.Empty;
            persistedToDb = false;
            try
            {
                bool esActivo = string.Equals(nuevoEstado, "Activo", StringComparison.OrdinalIgnoreCase);
                bool esDadoBaja = string.Equals(nuevoEstado, "Dado de baja", StringComparison.OrdinalIgnoreCase) || string.Equals(nuevoEstado, "DadoDeBaja", StringComparison.OrdinalIgnoreCase);

                if (_db != null)
                {
                    var equipoRef = _db.EquiposInformaticos.FirstOrDefault(e => e.Codigo == Equipo.Codigo);
                    if (equipoRef != null)
                    {
                        equipoRef.Estado = nuevoEstado;
                        equipoRef.FechaModificacion = DateTime.Now;

                        if (esActivo)
                            equipoRef.FechaBaja = null;

                        if (esDadoBaja)
                            equipoRef.FechaBaja = DateTime.Now;

                        _db.SaveChanges();

                        // Sincronizar entidad en memoria
                        Equipo.Estado = equipoRef.Estado;
                        Equipo.FechaModificacion = equipoRef.FechaModificacion;
                        Equipo.FechaBaja = equipoRef.FechaBaja;

                        try { WeakReferenceMessenger.Default.Send(new EquiposActualizadosMessage()); } catch { }

                        mensaje = esActivo ? "Equipo marcado como activo y fecha de baja eliminada." : (esDadoBaja ? "Equipo dado de baja correctamente." : "Estado actualizado correctamente.");
                        persistedToDb = true;
                        return true;
                    }
                }

                // Fallback en memoria
                Equipo.Estado = nuevoEstado;
                Equipo.FechaModificacion = DateTime.Now;
                if (esActivo)
                    Equipo.FechaBaja = null;
                if (esDadoBaja)
                    Equipo.FechaBaja = DateTime.Now;

                mensaje = esActivo ? "Estado actualizado en memoria: equipo marcado como activo y fecha de baja eliminada." : (esDadoBaja ? "Estado actualizado en memoria: equipo marcado como dado de baja." : "Estado actualizado en memoria.");
                persistedToDb = false;
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error al actualizar estado: {ex.Message}";
                persistedToDb = false;
                return false;
            }
        }
    }
}
