using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    public partial class RemoveMaintenanceSchedulingTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar tablas si existen
            migrationBuilder.Sql("IF OBJECT_ID('dbo.MantenimientoOcurrencia', 'U') IS NOT NULL DROP TABLE dbo.MantenimientoOcurrencia;");
            migrationBuilder.Sql("IF OBJECT_ID('dbo.MantenimientoProgramado', 'U') IS NOT NULL DROP TABLE dbo.MantenimientoProgramado;");
            migrationBuilder.Sql("IF OBJECT_ID('dbo.MantenimientoPlantilla', 'U') IS NOT NULL DROP TABLE dbo.MantenimientoPlantilla;");
            migrationBuilder.Sql("IF OBJECT_ID('dbo.MantenimientoPlantillaTarea', 'U') IS NOT NULL DROP TABLE dbo.MantenimientoPlantillaTarea;");
            migrationBuilder.Sql("IF OBJECT_ID('dbo.SeguimientoMantenimientoTarea', 'U') IS NOT NULL DROP TABLE dbo.SeguimientoMantenimientoTarea;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No recreamos las tablas eliminadas. Si fuera necesario, agregar definición aquí.
        }
    }
}
