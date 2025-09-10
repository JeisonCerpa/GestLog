using System;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionMantenimientos.Messages;
using System.Linq;

namespace GestLog.Services.Equipos
{
    public static class EquipoEstadoService
    {
        /// <summary>
        /// Establece el estado de un equipo. Si se provee DbContext intenta persistir los cambios; sino actúa solo en memoria.
        /// Devuelve true si la operación se completó (aunque sea en memoria), false si hubo error.
        /// </summary>
        public static bool SetEstado(EquipoInformaticoEntity equipo, string nuevoEstado, GestLogDbContext? db, out string mensaje, out bool persistedToDb)
        {
            mensaje = string.Empty;
            persistedToDb = false;
            try
            {
                bool esActivo = string.Equals(nuevoEstado, "Activo", StringComparison.OrdinalIgnoreCase);

                if (db != null)
                {
                    var equipoRef = db.EquiposInformaticos.FirstOrDefault(e => e.Codigo == equipo.Codigo);
                    if (equipoRef != null)
                    {
                        equipoRef.Estado = nuevoEstado;
                        equipoRef.FechaModificacion = DateTime.Now;
                        if (esActivo)
                            equipoRef.FechaBaja = null;

                        db.SaveChanges();

                        // Sincronizar entidad en memoria
                        equipo.Estado = equipoRef.Estado;
                        equipo.FechaModificacion = equipoRef.FechaModificacion;
                        equipo.FechaBaja = equipoRef.FechaBaja;

                        try { WeakReferenceMessenger.Default.Send(new EquiposActualizadosMessage()); } catch { }

                        mensaje = esActivo ? "Equipo marcado como activo y fecha de baja eliminada." : "Estado actualizado correctamente.";
                        persistedToDb = true;
                        return true;
                    }
                }

                // Fallback en memoria
                equipo.Estado = nuevoEstado;
                equipo.FechaModificacion = DateTime.Now;
                if (esActivo)
                    equipo.FechaBaja = null;

                mensaje = esActivo ? "Estado actualizado en memoria: equipo marcado como activo y fecha de baja eliminada." : "Estado actualizado en memoria.";
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
