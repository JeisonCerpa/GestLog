using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Services
{
    /// <summary>
    /// Registra automáticamente en GestionUsuarios_Auditorias las altas, bajas y modificaciones
    /// de las entidades auditables, sin que cada módulo tenga que llamar a la auditoría.
    /// Las filas se insertan dentro del mismo SaveChanges, por lo que comparten transacción
    /// con el cambio auditado: si el guardado falla, no queda auditoría huérfana.
    /// </summary>
    public class AuditoriaSaveChangesInterceptor : SaveChangesInterceptor
    {
        /// <summary>Fragmentos que delatan un dato sensible; su valor nunca se escribe en la auditoría.</summary>
        private static readonly string[] FragmentosSensibles =
        {
            "contrasena", "contraseña", "password", "hash", "salt", "token"
        };

        private const int MaxLongitudDetalle = 2000;

        /// <summary>A partir de aquí, un guardado se resume en una sola fila en vez de una por registro.</summary>
        private const int UmbralOperacionMasiva = 25;

        private readonly ICurrentUserService _currentUserService;

        public AuditoriaSaveChangesInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context != null)
                RegistrarCambios(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
                RegistrarCambios(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void RegistrarCambios(DbContext context)
        {
            // ToList() antes de agregar: si no, se modifica el ChangeTracker mientras se recorre.
            var entradas = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .Select(e => (Entrada: e, Perfil: AuditoriaPerfiles.Obtener(e.Metadata.ClrType.Name)))
                .Where(x => x.Perfil != null)
                // Altas generadas por el sistema (cronogramas automáticos): no las hizo nadie
                .Where(x => x.Entrada.State != EntityState.Added || x.Perfil!.AuditarCreacion)
                .ToList();

            if (entradas.Count == 0) return;

            var actual = _currentUserService.Current;
            var usuario = !string.IsNullOrWhiteSpace(actual?.FullName)
                ? actual!.FullName
                : (actual?.Username ?? "Sistema");
            var ahora = DateTime.Now;

            foreach (var lote in entradas.GroupBy(x => (Entidad: x.Entrada.Metadata.ClrType.Name, x.Entrada.State)))
            {
                var perfil = lote.First().Perfil!;
                var accion = lote.Key.State switch
                {
                    EntityState.Added => "Creacion",
                    EntityState.Deleted => "Eliminacion",
                    _ => "Modificacion"
                };

                // Una importación de Excel puede tocar cientos de filas de golpe: ahí interesa
                // "se importaron 320 seguimientos", no 320 filas que nadie va a leer.
                if (lote.Count() > UmbralOperacionMasiva)
                {
                    context.Add(new Auditoria
                    {
                        IdAuditoria = Guid.NewGuid(),
                        EntidadAfectada = lote.Key.Entidad,
                        IdEntidad = Guid.Empty,
                        Accion = accion + "Masiva",
                        UsuarioResponsable = usuario,
                        FechaHora = ahora,
                        Detalle = $"Operación masiva sobre {lote.Count()} registros de {perfil.Nombre} " +
                                  "(importación o proceso automático)"
                    });
                    continue;
                }

                foreach (var (entrada, _) in lote)
                {
                    var detalle = ConstruirDetalle(entrada, perfil);
                    if (string.IsNullOrEmpty(detalle)) continue; // nada cambió realmente

                    var (clave, idEntidad) = ObtenerClave(entrada, perfil);

                    context.Add(new Auditoria
                    {
                        IdAuditoria = Guid.NewGuid(),
                        EntidadAfectada = lote.Key.Entidad,
                        IdEntidad = idEntidad,
                        ClaveEntidad = clave,
                        DescripcionEntidad = ObtenerDescripcion(entrada, perfil),
                        Accion = accion,
                        UsuarioResponsable = usuario,
                        FechaHora = ahora,
                        Detalle = detalle
                    });
                }
            }
        }

        private static string ConstruirDetalle(EntityEntry entrada, PerfilAuditoria perfil)
        {
            var sb = new StringBuilder();

            if (entrada.State == EntityState.Deleted)
            {
                sb.Append("Registro eliminado");
            }
            else if (entrada.State == EntityState.Added)
            {
                sb.Append("Registro creado");
                var valores = entrada.Properties
                    .Where(p => !EsIgnorada(perfil, p.Metadata.Name))
                    .Where(p => !string.IsNullOrWhiteSpace(p.CurrentValue?.ToString()))
                    .Select(p => $"{Etiqueta(perfil, p.Metadata.Name)}: {Formatear(p.Metadata.Name, p.CurrentValue)}");
                var texto = string.Join("; ", valores);
                if (texto.Length > 0) sb.Append(" — ").Append(texto);
            }
            else
            {
                var cambios = entrada.Properties
                    .Where(p => p.IsModified && !EsIgnorada(perfil, p.Metadata.Name))
                    .Where(p => CambioReal(p.OriginalValue, p.CurrentValue))
                    .Select(p => $"{Etiqueta(perfil, p.Metadata.Name)}: '{Formatear(p.Metadata.Name, p.OriginalValue)}' → '{Formatear(p.Metadata.Name, p.CurrentValue)}'")
                    .ToList();

                if (cambios.Count == 0) return string.Empty;
                sb.Append(string.Join("; ", cambios));
            }

            return sb.Length > MaxLongitudDetalle
                ? sb.ToString(0, MaxLongitudDetalle) + "…"
                : sb.ToString();
        }

        private static bool EsIgnorada(PerfilAuditoria perfil, string nombrePropiedad) =>
            AuditoriaPerfiles.IgnoradasGlobales.Contains(nombrePropiedad) || perfil.Ignorar.Contains(nombrePropiedad);

        /// <summary>Nombre del campo como lo llama el negocio; si no hay etiqueta, el nombre técnico.</summary>
        private static string Etiqueta(PerfilAuditoria perfil, string nombrePropiedad) =>
            perfil.Etiquetas.TryGetValue(nombrePropiedad, out var etiqueta) ? etiqueta : nombrePropiedad;

        /// <summary>
        /// EF marca como modificada una propiedad que pasó de null a "" (y viceversa); para el
        /// historial eso no es un cambio y solo genera ruido. Los textos se comparan normalizados.
        /// </summary>
        private static bool CambioReal(object? original, object? actual)
        {
            // Texto (o ausencia de valor): null, "" y "  " son el mismo estado para el historial.
            if (original is null or string && actual is null or string)
                return !string.Equals(Normalizar(original), Normalizar(actual), StringComparison.Ordinal);

            return !Equals(original, actual);

            static string Normalizar(object? valor) => (valor as string)?.Trim() ?? string.Empty;
        }

        private static bool EsSensible(string nombrePropiedad) =>
            FragmentosSensibles.Any(f => nombrePropiedad.Contains(f, StringComparison.OrdinalIgnoreCase));

        private static string Formatear(string nombrePropiedad, object? valor)
        {
            if (EsSensible(nombrePropiedad)) return "***";
            return valor?.ToString() ?? "(vacío)";
        }

        /// <summary>
        /// Clave natural de la entidad a partir de su PK. Para entidades con PK generada por la base
        /// (identity), en una inserción el valor aún es provisional; el detalle conserva los datos.
        /// </summary>
        private static (string? clave, Guid idEntidad) ObtenerClave(EntityEntry entrada, PerfilAuditoria perfil)
        {
            var pk = entrada.Metadata.FindPrimaryKey();
            var valoresPk = pk?.Properties
                .Select(p => entrada.Property(p.Name).CurrentValue)
                .ToList() ?? new List<object?>();

            var idEntidad = valoresPk.Count == 1 && valoresPk[0] is Guid guid ? guid : Guid.Empty;

            // La clave de negocio identifica mejor el registro que la PK ("CLASE-001" vs "1117")
            var clave = ValoresDe(entrada, perfil.Clave).FirstOrDefault()
                        ?? string.Join("|", valoresPk.Select(v => v?.ToString() ?? string.Empty));

            if (clave.Length > 100) clave = clave.Substring(0, 100);

            return (string.IsNullOrEmpty(clave) ? null : clave, idEntidad);
        }

        /// <summary>
        /// Cómo se describe el registro para una persona: los campos que el perfil marque, juntos
        /// ("SOPORTE-IT2 — Juan Pérez"). Sin perfil de descripción, la fila queda solo con su código.
        /// </summary>
        private static string? ObtenerDescripcion(EntityEntry entrada, PerfilAuditoria perfil)
        {
            var partes = ValoresDe(entrada, perfil.Descripcion).Distinct().ToList();
            if (partes.Count == 0) return null;

            var descripcion = string.Join(" — ", partes);
            return descripcion.Length > 200 ? descripcion.Substring(0, 200) : descripcion;
        }

        /// <summary>Valores no vacíos de las propiedades indicadas, en el orden pedido.</summary>
        private static IEnumerable<string> ValoresDe(EntityEntry entrada, string[] nombres)
        {
            foreach (var nombre in nombres)
            {
                var propiedad = entrada.Properties.FirstOrDefault(p =>
                    string.Equals(p.Metadata.Name, nombre, StringComparison.OrdinalIgnoreCase));

                var valor = propiedad?.CurrentValue?.ToString();
                if (!string.IsNullOrWhiteSpace(valor)) yield return valor.Trim();
            }
        }
    }
}
