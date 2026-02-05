using Microsoft.EntityFrameworkCore;
using System;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using GestLog.Modules.GestionVehiculos.Models.Entities;

namespace GestLog.Modules.DatabaseConnection
{
    public class GestLogDbContext : DbContext
    {
        public GestLogDbContext(DbContextOptions<GestLogDbContext> options) : base(options)
        {
        }

        // Ejemplo de DbSet inicial, puedes agregar más entidades aquí
        // public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<CronogramaMantenimiento> Cronogramas { get; set; }
        public DbSet<SeguimientoMantenimiento> Seguimientos { get; set; }
        public DbSet<GestLog.Modules.Personas.Models.Persona> Personas { get; set; }
        public DbSet<GestLog.Modules.Usuarios.Models.Cargo> Cargos { get; set; }
        public DbSet<GestLog.Modules.Usuarios.Models.Usuario> Usuarios { get; set; }
        public DbSet<GestLog.Modules.Usuarios.Models.TipoDocumento> TiposDocumento { get; set; }
        public DbSet<GestLog.Modules.Usuarios.Models.Auditoria> Auditorias { get; set; }
        public DbSet<GestLog.Modules.Usuarios.Models.Rol> Roles { get; set; }
        public DbSet<GestLog.Modules.Usuarios.Models.Permiso> Permisos { get; set; }
        public DbSet<GestLog.Modules.Usuarios.Models.UsuarioRol> UsuarioRoles { get; set; }
        public DbSet<GestLog.Modules.Usuarios.Models.RolPermiso> RolPermisos { get; set; }
        public DbSet<GestLog.Modules.Usuarios.Models.UsuarioPermiso> UsuarioPermisos { get; set; }        
        public DbSet<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.EquipoInformaticoEntity> EquiposInformaticos { get; set; }
        public DbSet<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity> SlotsRam { get; set; }
        public DbSet<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity> Discos { get; set; }
        public DbSet<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.ConexionEntity> Conexiones { get; set; }        
        public DbSet<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.PerifericoEquipoInformaticoEntity> PerifericosEquiposInformaticos { get; set; }        
        public DbSet<MantenimientoCorrectivoEntity> MantenimientosCorrectivos { get; set; }
        public DbSet<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.PlanCronogramaEquipo> PlanesCronogramaEquipos { get; set; }
        public DbSet<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.EjecucionSemanal> EjecucionesSemanales { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configuración adicional de entidades aquí
            var boolArrayToStringConverter = new ValueConverter<bool[], string>(
                v => v == null || v.Length == 0 ? string.Empty : string.Join(";", v.Select(b => b ? "1" : "0")),
                v => string.IsNullOrEmpty(v) ? Array.Empty<bool>() : v.Split(new[] {';'}, StringSplitOptions.None).Select(s => s == "1").ToArray()
            );
            var boolArrayComparer = new ValueComparer<bool[]>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                a => a == null ? 0 : a.Aggregate(0, (hash, b) => hash * 31 + (b ? 1 : 0)),
                a => a == null ? Array.Empty<bool>() : a.ToArray()
            );            modelBuilder.Entity<CronogramaMantenimiento>()
                .ToTable("GestionMantenimientos_Cronogramas")
                .Property(c => c.Semanas)
                .HasConversion(boolArrayToStringConverter)
                .Metadata.SetValueComparer(boolArrayComparer);            // Configuración explícita para enums y decimales en Equipo
            modelBuilder.Entity<Equipo>()
                .ToTable("GestionMantenimientos_Equipos")
                .Property(e => e.Estado)
                .HasConversion<int>();
            modelBuilder.Entity<Equipo>()
                .Property(e => e.Sede)
                .HasConversion<int?>();
            modelBuilder.Entity<Equipo>()
                .Property(e => e.FrecuenciaMtto)
                .HasConversion<int?>();
            modelBuilder.Entity<Equipo>()
                .Property(e => e.Precio)
                .HasPrecision(18, 2);            // Configuración explícita para decimales en SeguimientoMantenimiento
            modelBuilder.Entity<SeguimientoMantenimiento>()
                .ToTable("GestionMantenimientos_Seguimientos")
                .Property(s => s.Costo)
                .HasPrecision(18, 2);              // Configuración para precision en Costo de EquipoInformaticoEntity (evita warning EF Core)
            modelBuilder.Entity<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.EquipoInformaticoEntity>()
                .ToTable("GestionEquiposInformaticos_Equipos")
                .Property(e => e.Costo)
                .HasPrecision(18, 2);            // Configuración para PerifericoEquipoInformaticoEntity
            modelBuilder.Entity<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.PerifericoEquipoInformaticoEntity>()
                .ToTable("GestionEquiposInformaticos_Perifericos")
                .Property(p => p.Costo)
                .HasPrecision(18, 2);
            
            modelBuilder.Entity<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.PerifericoEquipoInformaticoEntity>()
                .Property(p => p.Estado)
                .HasConversion<int>();
              // Configuración para MantenimientoCorrectivoEntity - mapeo de tabla
            modelBuilder.Entity<MantenimientoCorrectivoEntity>()
                .ToTable("GestionEquiposInformaticos_MantenimientosCorrectivos");
            
            modelBuilder.Entity<MantenimientoCorrectivoEntity>()
                .Property(m => m.Estado)
                .HasConversion<int>();

            modelBuilder.Entity<MantenimientoCorrectivoEntity>()
                .Property(m => m.CostoReparacion)
                .HasPrecision(18, 2)
                .IsRequired(false);            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.TipoDocumento>(entity =>
            {
                entity.ToTable("GestionUsuarios_TiposDocumento");
                entity.HasKey(e => e.IdTipoDocumento);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Descripcion).HasMaxLength(100);
                entity.HasIndex(e => e.Codigo).IsUnique();
            });            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.Auditoria>(entity =>
            {
                entity.ToTable("GestionUsuarios_Auditorias");
                entity.HasKey(e => e.IdAuditoria);
                entity.Property(e => e.EntidadAfectada).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Accion).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UsuarioResponsable).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Detalle).IsRequired();
                entity.Property(e => e.FechaHora).IsRequired();
                entity.Property(e => e.IdEntidad).IsRequired();
            });            modelBuilder.Entity<GestLog.Modules.Personas.Models.Persona>(entity =>
            {
                entity.ToTable("GestionPersonas_Personas");
                entity.HasKey(e => e.IdPersona);
                entity.Property(e => e.Nombres).IsRequired();
                entity.Property(e => e.Apellidos).IsRequired();
                entity.Property(e => e.NumeroDocumento).IsRequired();
                entity.Property(e => e.Correo).IsRequired();
                entity.Property(e => e.Telefono).IsRequired();
                entity.Property(e => e.Activo).IsRequired();
                entity.Property(e => e.FechaCreacion).IsRequired();
                entity.Property(e => e.FechaModificacion).IsRequired();
                // Mapear Sede (enum nullable) como entero en la BD
                entity.Property(e => e.Sede).HasConversion<int?>();
                entity.HasOne(e => e.Cargo)
                    .WithMany()
                    .HasForeignKey(e => e.CargoId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.TipoDocumento)
                    .WithMany()
                    .HasForeignKey(e => e.TipoDocumentoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });            // --- RELACIONES USUARIOS, ROLES Y PERMISOS ---
            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.Rol>(entity =>
            {
                entity.ToTable("GestionUsuarios_Roles");
                entity.HasKey(e => e.IdRol);
                entity.Property(e => e.Nombre).IsRequired();
                entity.Property(e => e.Descripcion).IsRequired();
            });            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.Permiso>(entity =>
            {
                entity.ToTable("GestionUsuarios_Permisos");
                entity.HasKey(e => e.IdPermiso);
                entity.Property(e => e.Nombre).IsRequired();
                entity.Property(e => e.Descripcion).IsRequired();
                entity.HasOne<GestLog.Modules.Usuarios.Models.Permiso>()
                    .WithMany(p => p.SubPermisos)
                    .HasForeignKey(p => p.PermisoPadreId)
                    .OnDelete(DeleteBehavior.Restrict);
            });            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.UsuarioRol>(entity =>
            {
                entity.ToTable("GestionUsuarios_UsuarioRoles");
                entity.HasKey(e => new { e.IdUsuario, e.IdRol });
                entity.HasOne<GestLog.Modules.Usuarios.Models.Usuario>()
                    .WithMany()
                    .HasForeignKey(e => e.IdUsuario)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<GestLog.Modules.Usuarios.Models.Rol>()
                    .WithMany()
                    .HasForeignKey(e => e.IdRol)
                    .OnDelete(DeleteBehavior.Cascade);
            });            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.RolPermiso>(entity =>
            {
                entity.ToTable("GestionUsuarios_RolPermisos");
                entity.HasKey(e => new { e.IdRol, e.IdPermiso });
                entity.HasOne<GestLog.Modules.Usuarios.Models.Rol>()
                    .WithMany()
                    .HasForeignKey(e => e.IdRol)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<GestLog.Modules.Usuarios.Models.Permiso>()
                    .WithMany()
                    .HasForeignKey(e => e.IdPermiso)
                    .OnDelete(DeleteBehavior.Cascade);
            });            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.UsuarioPermiso>(entity =>
            {
                entity.ToTable("GestionUsuarios_UsuarioPermisos");
                entity.HasKey(e => new { e.IdUsuario, e.IdPermiso });
                entity.HasOne<GestLog.Modules.Usuarios.Models.Usuario>()
                    .WithMany()
                    .HasForeignKey(e => e.IdUsuario)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<GestLog.Modules.Usuarios.Models.Permiso>()
                    .WithMany()
                    .HasForeignKey(e => e.IdPermiso)                    .OnDelete(DeleteBehavior.Cascade);            });
            // Mapear Usuario y Cargo al nuevo esquema de nombres
            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.Usuario>(entity =>
            {
                entity.ToTable("GestionUsuarios_Usuarios");
                entity.HasKey(e => e.IdUsuario);
                // otras configuraciones por convención
            });

            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.Cargo>(entity =>
            {
                entity.ToTable("GestionUsuarios_Cargos");
                entity.HasKey(e => e.IdCargo);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.EjecucionSemanal>(entity =>
            {
                entity.ToTable("GestionEquiposInformaticos_EjecucionSemanal");
                
                // ✅ ÍNDICE PRINCIPAL: por equipo + año + semana (para consultas rápidas)
                entity.HasIndex(e => new { e.CodigoEquipo, e.AnioISO, e.SemanaISO });
                
                // Índice para búsquedas por plan (opcional, para migración)
                entity.HasIndex(e => e.PlanId);
                
                entity.Property(e => e.Estado).IsRequired();
                
                // ✅ FK a Equipo: REQUIRED, ON DELETE NO ACTION (evitar conflictos en migración)
                // NO ACTION permite que el historial persista aunque el equipo se elimine
                entity.HasOne(e => e.Equipo)
                      .WithMany()
                      .HasForeignKey(e => e.CodigoEquipo)
                      .OnDelete(DeleteBehavior.NoAction)
                      .IsRequired();
                
                // ✅ FK a Plan: OPTIONAL, ON DELETE SET NULL (permitir eliminar planes sin perder historial)
                entity.HasOne(e => e.Plan)
                      .WithMany(p => p.Ejecuciones)
                      .HasForeignKey(e => e.PlanId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });            modelBuilder.Entity<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.PlanCronogramaEquipo>(entity =>
            {
                entity.ToTable("GestionEquiposInformaticos_PlanCronogramaEquipo");
                entity.Property(p => p.DiaProgramado).IsRequired();
                // Ejecuciones ahora se configuran desde EjecucionSemanal
            });            // Configuración para ConexionEntity
            modelBuilder.Entity<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.ConexionEntity>(entity =>
            {
                entity.ToTable("GestionEquiposInformaticos_ConexionesEquipos");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CodigoEquipo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Adaptador).HasMaxLength(255);
                entity.Property(e => e.DireccionMAC).HasMaxLength(17);
                entity.Property(e => e.DireccionIPv4).HasMaxLength(15);
                entity.Property(e => e.MascaraSubred).HasMaxLength(15);
                entity.Property(e => e.PuertoEnlace).HasMaxLength(15);
                
                // Configurar relación con EquipoInformaticoEntity
                entity.HasOne(c => c.EquipoInformatico)
                      .WithMany(e => e.Conexiones)
                      .HasForeignKey(c => c.CodigoEquipo)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Mapear tablas restantes de GestionEquiposInformaticos
            modelBuilder.Entity<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.DiscoEntity>()
                .ToTable("GestionEquiposInformaticos_Discos")
                .HasKey(d => d.Id);

            modelBuilder.Entity<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.SlotRamEntity>()
                .ToTable("GestionEquiposInformaticos_SlotsRam")
                .HasKey(s => s.Id);

            // ✅ CONFIGURACIÓN PARA GESTIÓN DE VEHÍCULOS
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.ToTable("GestionVehiculos_Vehicles");
                entity.HasKey(e => e.Id);
                
                // Campos requeridos
                entity.Property(e => e.Plate)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("Plate");
                
                entity.Property(e => e.Vin)
                    .IsRequired()
                    .HasMaxLength(17)
                    .HasColumnName("VIN");
                
                entity.Property(e => e.Brand)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Model)
                    .IsRequired()
                    .HasMaxLength(100);
                
                // Campos opcionales
                entity.Property(e => e.Version)
                    .HasMaxLength(100)
                    .IsRequired(false);
                
                entity.Property(e => e.Color)
                    .HasMaxLength(50)
                    .IsRequired(false);
                
                entity.Property(e => e.PhotoPath)
                    .HasMaxLength(400)
                    .IsRequired(false);
                
                entity.Property(e => e.PhotoThumbPath)
                    .HasMaxLength(400)
                    .IsRequired(false);
                
                // Enum conversions
                entity.Property(e => e.Type)
                    .HasConversion<int>();
                
                entity.Property(e => e.State)
                    .HasConversion<int>();
                
                // Campos de auditoría
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetimeoffset")
                    .HasDefaultValueSql("GETUTCDATE()");
                
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetimeoffset");
                
                // Soft delete
                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
                
                // Índices
                entity.HasIndex(e => e.Plate)
                    .IsUnique()
                    .HasDatabaseName("IX_Vehicles_Plate");
                
                entity.HasIndex(e => e.Vin)
                    .IsUnique()
                    .HasDatabaseName("IX_Vehicles_VIN");
                
                entity.HasIndex(e => e.State)
                    .HasDatabaseName("IX_Vehicles_State");
                
                entity.HasIndex(e => e.Type)
                    .HasDatabaseName("IX_Vehicles_Type");
            });
        }

        public void SeedAdminRole()
        {
            // Crear permiso base si no existe
            var permisoAdmin = Permisos.FirstOrDefault(p => p.Nombre == "AdministrarSistema");
            if (permisoAdmin == null)
            {
                permisoAdmin = new GestLog.Modules.Usuarios.Models.Permiso
                {
                    IdPermiso = Guid.NewGuid(),
                    Nombre = "AdministrarSistema",
                    Descripcion = "Permite acceso total al sistema",
                    Modulo = "Sistema" // Inicialización obligatoria
                };
                Permisos.Add(permisoAdmin);
                SaveChanges();
            }
            // Crear rol admin si no existe
            var rolAdmin = Roles.FirstOrDefault(r => r.Nombre == "Administrador");
            if (rolAdmin == null)
            {
                rolAdmin = new GestLog.Modules.Usuarios.Models.Rol
                {
                    IdRol = Guid.NewGuid(),
                    Nombre = "Administrador",
                    Descripcion = "Rol con todos los permisos"
                };
                Roles.Add(rolAdmin);
                SaveChanges();
            }
            // Asignar permiso al rol si no está asignado
            var existeRolPermiso = RolPermisos.Any(rp => rp.IdRol == rolAdmin.IdRol && rp.IdPermiso == permisoAdmin.IdPermiso);
            if (!existeRolPermiso)
            {
                RolPermisos.Add(new GestLog.Modules.Usuarios.Models.RolPermiso
                {
                    IdRol = rolAdmin.IdRol,
                    IdPermiso = permisoAdmin.IdPermiso
                });
                SaveChanges();
            }
        }
    }

    
}

