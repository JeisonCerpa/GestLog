using Microsoft.EntityFrameworkCore;
using System;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configuración adicional de entidades aquí
            var boolArrayToStringConverter = new ValueConverter<bool[], string>(
                v => string.Join(";", v.Select(b => b ? "1" : "0")),
                v => v.Length == 0 ? new bool[52] : v.Split(new[] {';'}, 52, System.StringSplitOptions.None).Select(s => s == "1").ToArray()
            );
            modelBuilder.Entity<CronogramaMantenimiento>()
                .Property(c => c.Semanas)
                .HasConversion(boolArrayToStringConverter);

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
        }
    }

    
}

