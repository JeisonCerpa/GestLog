using System;
using System.Collections.Generic;

namespace Modules.Usuarios.Services
{
    /// <summary>
    /// Cómo se audita cada entidad: qué la identifica, cómo se describe y cómo se llaman sus campos
    /// en lenguaje de negocio. Un equipo informático no se lee igual que una persona.
    /// </summary>
    public sealed class PerfilAuditoria
    {
        /// <summary>Nombre del tipo de registro tal como lo llama el negocio.</summary>
        public required string Nombre { get; init; }

        /// <summary>Propiedades que identifican el registro, en orden de preferencia.</summary>
        public string[] Clave { get; init; } = Array.Empty<string>();

        /// <summary>Propiedades que lo describen; se muestran juntas ("SOPORTE-IT2 — Juan Pérez").</summary>
        public string[] Descripcion { get; init; } = Array.Empty<string>();

        /// <summary>Nombre técnico del campo → cómo se llama para el usuario.</summary>
        public Dictionary<string, string> Etiquetas { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Campos que no aportan al historial de esta entidad.</summary>
        public HashSet<string> Ignorar { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// false cuando los registros los crea el sistema y no una persona (cronogramas generados
        /// automáticamente): su alta no dice nada: lo que interesa es quién los tocó después.
        /// </summary>
        public bool AuditarCreacion { get; init; } = true;
    }

    /// <summary>
    /// Registro central de entidades auditadas. Estar en este diccionario es lo que hace que una
    /// entidad se audite: agregar un módulo nuevo es agregar aquí su perfil.
    /// </summary>
    public static class AuditoriaPerfiles
    {
        /// <summary>
        /// Campos de mantenimiento interno, ignorados en todas las entidades. FechaRegistro NO está
        /// aquí: en los seguimientos es la fecha oficial de realización del mantenimiento.
        /// </summary>
        public static readonly HashSet<string> IgnoradasGlobales = new(StringComparer.OrdinalIgnoreCase)
        {
            "FechaModificacion", "FechaCreacion"
        };

        private static readonly Dictionary<string, PerfilAuditoria> Perfiles = new(StringComparer.Ordinal)
        {
            // ── Equipos informáticos ─────────────────────────────────────────────
            ["EquipoInformaticoEntity"] = new()
            {
                Nombre = "Equipo informático",
                Clave = new[] { "Codigo" },
                Descripcion = new[] { "NombreEquipo", "UsuarioAsignado" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["NombreEquipo"] = "Nombre del equipo",
                    ["UsuarioAsignado"] = "Usuario asignado",
                    ["SO"] = "Sistema operativo",
                    ["CodigoAnydesk"] = "Código AnyDesk",
                    ["SerialNumber"] = "Serial",
                    ["FechaCompra"] = "Fecha de compra",
                    ["FechaBaja"] = "Fecha de baja"
                },
                Ignorar = new(StringComparer.OrdinalIgnoreCase) { "UsuarioAsignadoAnterior" }
            },

            ["PerifericoEquipoInformaticoEntity"] = new()
            {
                Nombre = "Periférico",
                Clave = new[] { "Codigo" },
                Descripcion = new[] { "Dispositivo", "UsuarioAsignado" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["CodigoEquipoAsignado"] = "Equipo asignado",
                    ["UsuarioAsignado"] = "Usuario asignado",
                    ["SerialNumber"] = "Serial",
                    ["FechaCompra"] = "Fecha de compra"
                },
                Ignorar = new(StringComparer.OrdinalIgnoreCase)
                {
                    "UsuarioAsignadoAnterior", "CodigoEquipoAsignadoAnterior"
                }
            },

            ["MantenimientoCorrectivoEntity"] = new()
            {
                Nombre = "Mantenimiento correctivo",
                Clave = new[] { "Codigo" },
                Descripcion = new[] { "DescripcionFalla" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Codigo"] = "Equipo",
                    ["TipoEntidad"] = "Tipo de equipo",
                    ["DescripcionFalla"] = "Falla",
                    ["ProveedorAsignado"] = "Proveedor",
                    ["CostoReparacion"] = "Costo de reparación",
                    ["PeriodoGarantia"] = "Garantía",
                    ["FechaFalla"] = "Fecha de la falla",
                    ["FechaInicio"] = "Fecha de inicio",
                    ["FechaCompletado"] = "Fecha de finalización"
                },
                Ignorar = new(StringComparer.OrdinalIgnoreCase) { "FechaActualizacion" }
            },

            ["PlanCronogramaEquipo"] = new()
            {
                Nombre = "Plan de cronograma (informático)",
                Clave = new[] { "CodigoEquipo", "EquipoCodigo" },
                Descripcion = new[] { "Descripcion", "Responsable" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["CodigoEquipo"] = "Equipo",
                    ["DiaProgramado"] = "Día programado",
                    ["Responsable"] = "Responsable",
                    ["Activo"] = "Activo"
                },
                // El checklist es un JSON extenso: su diff no se puede leer en una tabla
                Ignorar = new(StringComparer.OrdinalIgnoreCase) { "ChecklistJson" }
            },

            ["EjecucionSemanal"] = new()
            {
                Nombre = "Mantenimiento ejecutado (informático)",
                Clave = new[] { "CodigoEquipo" },
                Descripcion = new[] { "DescripcionPlanSnapshot" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["CodigoEquipo"] = "Equipo",
                    ["UsuarioEjecuta"] = "Ejecutado por",
                    ["FechaObjetivo"] = "Fecha objetivo",
                    ["FechaEjecucion"] = "Fecha de ejecución",
                    ["SemanaISO"] = "Semana",
                    ["AnioISO"] = "Año",
                    ["DescripcionPlanSnapshot"] = "Plan",
                    ["ResponsablePlanSnapshot"] = "Responsable del plan"
                },
                Ignorar = new(StringComparer.OrdinalIgnoreCase) { "ResultadoJson" }
            },

            // ── Mantenimientos ───────────────────────────────────────────────────
            ["Equipo"] = new()
            {
                Nombre = "Equipo (mantenimientos)",
                Clave = new[] { "Codigo" },
                Descripcion = new[] { "Nombre" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["CompradoA"] = "Comprado a",
                    ["FrecuenciaMtto"] = "Frecuencia de mantenimiento",
                    ["FechaCompra"] = "Fecha de compra",
                    ["FechaBaja"] = "Fecha de baja",
                    ["Precio"] = "Precio"
                },
                // Aquí FechaRegistro es la fecha de alta del equipo, no un dato de negocio que cambie
                Ignorar = new(StringComparer.OrdinalIgnoreCase) { "FechaRegistro" }
            },

            ["SeguimientoMantenimiento"] = new()
            {
                Nombre = "Mantenimiento ejecutado",
                Clave = new[] { "Codigo" },
                Descripcion = new[] { "Nombre" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Codigo"] = "Equipo",
                    ["TipoMtno"] = "Tipo de mantenimiento",
                    ["FechaRealizacion"] = "Fecha de realización",
                    ["FechaRegistro"] = "Fecha de registro",
                    ["Semana"] = "Semana",
                    ["Anio"] = "Año",
                    ["Costo"] = "Costo",
                    ["Responsable"] = "Responsable",
                    ["Frecuencia"] = "Frecuencia"
                }
            },

            ["CronogramaMantenimiento"] = new()
            {
                Nombre = "Cronograma de mantenimiento",
                Clave = new[] { "Codigo" },
                Descripcion = new[] { "Nombre" },
                // Los cronogramas los genera el sistema; solo interesa quién los modificó después
                AuditarCreacion = false,
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Codigo"] = "Equipo",
                    ["FrecuenciaMtto"] = "Frecuencia",
                    ["Semanas"] = "Semanas programadas",
                    ["Anio"] = "Año"
                }
            },

            // ── Vehículos ────────────────────────────────────────────────────────
            ["Vehicle"] = new()
            {
                Nombre = "Vehículo",
                Clave = new[] { "Plate" },
                Descripcion = new[] { "Brand", "Model" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Plate"] = "Placa",
                    ["Brand"] = "Marca",
                    ["Model"] = "Modelo",
                    ["Version"] = "Versión",
                    ["Year"] = "Año",
                    ["Mileage"] = "Kilometraje",
                    ["FuelType"] = "Tipo de combustible"
                }
            },

            ["VehicleDocument"] = new()
            {
                Nombre = "Documento de vehículo",
                Clave = new[] { "DocumentNumber" },
                Descripcion = new[] { "DocumentType", "FileName" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["VehicleId"] = "Vehículo",
                    ["DocumentType"] = "Tipo de documento",
                    ["DocumentNumber"] = "Número",
                    ["IssuedDate"] = "Fecha de expedición",
                    ["ExpirationDate"] = "Fecha de vencimiento",
                    ["FileName"] = "Archivo"
                }
            },

            ["PlanMantenimientoVehiculo"] = new()
            {
                Nombre = "Plan de mantenimiento de vehículo",
                Clave = new[] { "PlacaVehiculo" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["PlacaVehiculo"] = "Placa",
                    ["PlantillaId"] = "Plantilla",
                    ["IntervaloKMPersonalizado"] = "Intervalo (km)",
                    ["IntervaloDiasPersonalizado"] = "Intervalo (días)"
                }
            },

            ["EjecucionMantenimiento"] = new()
            {
                Nombre = "Ejecución de mantenimiento",
                Clave = new[] { "PlacaVehiculo" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["PlacaVehiculo"] = "Placa",
                    ["FechaEjecucion"] = "Fecha de ejecución",
                    ["KMAlMomento"] = "Kilometraje",
                    ["ObservacionesTecnico"] = "Observaciones del técnico",
                    ["RutaFactura"] = "Factura"
                }
            },

            // ── Identidad y catálogos ────────────────────────────────────────────
            ["Usuario"] = new()
            {
                Nombre = "Usuario",
                Clave = new[] { "NombreUsuario", "Username" },
                Descripcion = new[] { "NombreUsuario" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["NombreUsuario"] = "Nombre de usuario",
                    ["Activo"] = "Activo",
                    ["PersonaId"] = "Persona vinculada"
                },
                // El cambio de credenciales se registra como acción propia ("RestablecerContrasena"),
                // no como un diff de campos cifrados que no dice nada.
                Ignorar = new(StringComparer.OrdinalIgnoreCase)
                {
                    "HashContrasena", "Salt", "PasswordChangedAt"
                }
            },

            ["Persona"] = new()
            {
                Nombre = "Persona",
                Clave = new[] { "NumeroDocumento", "Documento" },
                Descripcion = new[] { "Nombres", "Apellidos" },
                Etiquetas = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["NumeroDocumento"] = "Documento",
                    ["TipoDocumentoId"] = "Tipo de documento",
                    ["CargoId"] = "Cargo",
                    ["CorreoElectronico"] = "Correo electrónico"
                }
            },

            ["Rol"] = new()
            {
                Nombre = "Rol",
                Clave = new[] { "Nombre" },
                Descripcion = new[] { "Descripcion" }
            },

            ["Permiso"] = new()
            {
                Nombre = "Permiso",
                Clave = new[] { "Nombre" },
                Descripcion = new[] { "Descripcion" }
            },

            ["Cargo"] = new()
            {
                Nombre = "Cargo",
                Clave = new[] { "Nombre" },
                Descripcion = new[] { "Descripcion" }
            },

            ["TipoDocumento"] = new()
            {
                Nombre = "Tipo de documento",
                Clave = new[] { "Codigo", "Nombre" },
                Descripcion = new[] { "Nombre" }
            },

            ["UsuarioRol"] = new() { Nombre = "Rol asignado a usuario" },
            ["RolPermiso"] = new() { Nombre = "Permiso asignado a rol" },
            ["UsuarioPermiso"] = new() { Nombre = "Permiso asignado a usuario" }
        };

        /// <summary>Perfil de la entidad, o null si no se audita.</summary>
        public static PerfilAuditoria? Obtener(string nombreEntidad) =>
            Perfiles.TryGetValue(nombreEntidad, out var perfil) ? perfil : null;

        /// <summary>Nombre legible del tipo de registro; devuelve el técnico si no hay perfil.</summary>
        public static string NombreLegible(string nombreEntidad) =>
            Perfiles.TryGetValue(nombreEntidad, out var perfil) ? perfil.Nombre : nombreEntidad;
    }
}
