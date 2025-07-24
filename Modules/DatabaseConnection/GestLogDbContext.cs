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
        }
    }

    
}

