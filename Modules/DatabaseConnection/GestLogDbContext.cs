using Microsoft.EntityFrameworkCore;
using System;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configuración adicional de entidades aquí
            var boolArrayToStringConverter = new ValueConverter<bool[], string>(
                v => string.Join(";", v.Select(b => b ? "1" : "0")),
                v => v.Length == 0 ? new bool[52] : v.Split(new[] {';'}, 52, System.StringSplitOptions.None).Select(s => s == "1").ToArray()
            );
            var boolArrayComparer = new ValueComparer<bool[]>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                a => a == null ? 0 : a.Aggregate(0, (hash, b) => hash * 31 + (b ? 1 : 0)),
                a => a == null ? new bool[52] : a.ToArray()
            );
            modelBuilder.Entity<CronogramaMantenimiento>()
                .Property(c => c.Semanas)
                .HasConversion(boolArrayToStringConverter)
                .Metadata.SetValueComparer(boolArrayComparer);

            // Configuración explícita para enums y decimales en Equipo
            modelBuilder.Entity<Equipo>()
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
                .HasPrecision(18, 2);

            // Configuración explícita para decimales en SeguimientoMantenimiento
            modelBuilder.Entity<SeguimientoMantenimiento>()
                .Property(s => s.Costo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.TipoDocumento>(entity =>
            {
                entity.ToTable("TipoDocumento");
                entity.HasKey(e => e.IdTipoDocumento);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Descripcion).HasMaxLength(100);
                entity.HasIndex(e => e.Codigo).IsUnique();
            });
            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.Auditoria>(entity =>
            {
                entity.ToTable("Auditoria");
                entity.HasKey(e => e.IdAuditoria);
                entity.Property(e => e.EntidadAfectada).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Accion).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UsuarioResponsable).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Detalle).IsRequired();
                entity.Property(e => e.FechaHora).IsRequired();
                entity.Property(e => e.IdEntidad).IsRequired();
            });
            modelBuilder.Entity<GestLog.Modules.Personas.Models.Persona>(entity =>
            {
                entity.ToTable("Personas");
                entity.HasKey(e => e.IdPersona);
                entity.Property(e => e.Nombres).IsRequired();
                entity.Property(e => e.Apellidos).IsRequired();
                entity.Property(e => e.NumeroDocumento).IsRequired();
                entity.Property(e => e.Correo).IsRequired();
                entity.Property(e => e.Telefono).IsRequired();
                entity.Property(e => e.Activo).IsRequired();
                entity.Property(e => e.FechaCreacion).IsRequired();
                entity.Property(e => e.FechaModificacion).IsRequired();
                entity.HasOne(e => e.Cargo)
                    .WithMany()
                    .HasForeignKey(e => e.CargoId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.TipoDocumento)
                    .WithMany()
                    .HasForeignKey(e => e.TipoDocumentoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // --- RELACIONES USUARIOS, ROLES Y PERMISOS ---
            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.Rol>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.IdRol);
                entity.Property(e => e.Nombre).IsRequired();
                entity.Property(e => e.Descripcion).IsRequired();
            });
            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.Permiso>(entity =>
            {
                entity.ToTable("Permisos");
                entity.HasKey(e => e.IdPermiso);
                entity.Property(e => e.Nombre).IsRequired();
                entity.Property(e => e.Descripcion).IsRequired();
                entity.HasOne<GestLog.Modules.Usuarios.Models.Permiso>()
                    .WithMany(p => p.SubPermisos)
                    .HasForeignKey(p => p.PermisoPadreId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.UsuarioRol>(entity =>
            {
                entity.ToTable("UsuarioRoles");
                entity.HasKey(e => new { e.IdUsuario, e.IdRol });
                entity.HasOne<GestLog.Modules.Usuarios.Models.Usuario>()
                    .WithMany()
                    .HasForeignKey(e => e.IdUsuario)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<GestLog.Modules.Usuarios.Models.Rol>()
                    .WithMany()
                    .HasForeignKey(e => e.IdRol)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.RolPermiso>(entity =>
            {
                entity.ToTable("RolPermisos");
                entity.HasKey(e => new { e.IdRol, e.IdPermiso });
                entity.HasOne<GestLog.Modules.Usuarios.Models.Rol>()
                    .WithMany()
                    .HasForeignKey(e => e.IdRol)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<GestLog.Modules.Usuarios.Models.Permiso>()
                    .WithMany()
                    .HasForeignKey(e => e.IdPermiso)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<GestLog.Modules.Usuarios.Models.UsuarioPermiso>(entity =>
            {
                entity.ToTable("UsuarioPermisos");
                entity.HasKey(e => new { e.IdUsuario, e.IdPermiso });
                entity.HasOne<GestLog.Modules.Usuarios.Models.Usuario>()
                    .WithMany()
                    .HasForeignKey(e => e.IdUsuario)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<GestLog.Modules.Usuarios.Models.Permiso>()
                    .WithMany()
                    .HasForeignKey(e => e.IdPermiso)
                    .OnDelete(DeleteBehavior.Cascade);
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

