using Microsoft.EntityFrameworkCore;
using System;

namespace GestLog.Modules.DatabaseConnection
{
    public class GestLogDbContext : DbContext
    {
        public GestLogDbContext(DbContextOptions<GestLogDbContext> options) : base(options)
        {
        }

        // Ejemplo de DbSet inicial, puedes agregar más entidades aquí
        // public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configuración adicional de entidades aquí
        }
    }

    
}

