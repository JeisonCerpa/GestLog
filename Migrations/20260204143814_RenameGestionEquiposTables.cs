using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class RenameGestionEquiposTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usar SQL condicional para evitar errores si los nombres de constraints/tablas difieren en la BD
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ConexionesEquiposInformaticos_EquiposInformaticos_CodigoEquipo')
BEGIN
    ALTER TABLE [ConexionesEquiposInformaticos] DROP CONSTRAINT [FK_ConexionesEquiposInformaticos_EquiposInformaticos_CodigoEquipo];
END
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Discos_EquiposInformaticos_CodigoEquipo')
BEGIN
    ALTER TABLE [Discos] DROP CONSTRAINT [FK_Discos_EquiposInformaticos_CodigoEquipo];
END
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EjecucionSemanal_EquiposInformaticos_CodigoEquipo')
BEGIN
    ALTER TABLE [EjecucionSemanal] DROP CONSTRAINT [FK_EjecucionSemanal_EquiposInformaticos_CodigoEquipo];
END
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PerifericosEquiposInformaticos_EquiposInformaticos_CodigoEquipoAsignado')
BEGIN
    ALTER TABLE [PerifericosEquiposInformaticos] DROP CONSTRAINT [FK_PerifericosEquiposInformaticos_EquiposInformaticos_CodigoEquipoAsignado];
END
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PlanCronogramaEquipo_EquiposInformaticos_EquipoCodigo')
BEGIN
    ALTER TABLE [PlanCronogramaEquipo] DROP CONSTRAINT [FK_PlanCronogramaEquipo_EquiposInformaticos_EquipoCodigo];
END
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SlotsRam_EquiposInformaticos_CodigoEquipo')
BEGIN
    ALTER TABLE [SlotsRam] DROP CONSTRAINT [FK_SlotsRam_EquiposInformaticos_CodigoEquipo];
END
");

            // Eliminar tablas antiguas solo si existen (evita errores en BD de pruebas)
            migrationBuilder.Sql(@"IF OBJECT_ID('dbo.MantenimientoPlantillaTarea','U') IS NOT NULL DROP TABLE [dbo].[MantenimientoPlantillaTarea];");
            migrationBuilder.Sql(@"IF OBJECT_ID('dbo.SeguimientoMantenimientoTarea','U') IS NOT NULL DROP TABLE [dbo].[SeguimientoMantenimientoTarea];");

            // Eliminar PKs solo si existen
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PerifericosEquiposInformaticos')
BEGIN
    ALTER TABLE [PerifericosEquiposInformaticos] DROP CONSTRAINT [PK_PerifericosEquiposInformaticos];
END
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_EquiposInformaticos')
BEGIN
    ALTER TABLE [EquiposInformaticos] DROP CONSTRAINT [PK_EquiposInformaticos];
END
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_ConexionesEquiposInformaticos')
BEGIN
    ALTER TABLE [ConexionesEquiposInformaticos] DROP CONSTRAINT [PK_ConexionesEquiposInformaticos];
END
");

            migrationBuilder.RenameTable(
                name: "PerifericosEquiposInformaticos",
                newName: "GestionEquiposInformaticos_Perifericos");

            migrationBuilder.RenameTable(
                name: "EquiposInformaticos",
                newName: "GestionEquiposInformaticos_Equipos");

            migrationBuilder.RenameTable(
                name: "ConexionesEquiposInformaticos",
                newName: "GestionEquiposInformaticos_ConexionesEquipos");

            migrationBuilder.RenameIndex(
                name: "IX_PerifericosEquiposInformaticos_CodigoEquipoAsignado",
                table: "GestionEquiposInformaticos_Perifericos",
                newName: "IX_GestionEquiposInformaticos_Perifericos_CodigoEquipoAsignado");

            migrationBuilder.RenameIndex(
                name: "IX_ConexionesEquiposInformaticos_CodigoEquipo",
                table: "GestionEquiposInformaticos_ConexionesEquipos",
                newName: "IX_GestionEquiposInformaticos_ConexionesEquipos_CodigoEquipo");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_Perifericos')
BEGIN
    ALTER TABLE [GestionEquiposInformaticos_Perifericos] ADD CONSTRAINT [PK_GestionEquiposInformaticos_Perifericos] PRIMARY KEY CLUSTERED ([Codigo]);
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_Equipos')
BEGIN
    ALTER TABLE [GestionEquiposInformaticos_Equipos] ADD CONSTRAINT [PK_GestionEquiposInformaticos_Equipos] PRIMARY KEY CLUSTERED ([Codigo]);
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_ConexionesEquipos')
BEGIN
    ALTER TABLE [GestionEquiposInformaticos_ConexionesEquipos] ADD CONSTRAINT [PK_GestionEquiposInformaticos_ConexionesEquipos] PRIMARY KEY CLUSTERED ([Id]);
END");

            migrationBuilder.AddForeignKey(
                name: "FK_Discos_GestionEquiposInformaticos_Equipos_CodigoEquipo",
                table: "Discos",
                column: "CodigoEquipo",
                principalTable: "GestionEquiposInformaticos_Equipos",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EjecucionSemanal_GestionEquiposInformaticos_Equipos_CodigoEquipo",
                table: "EjecucionSemanal",
                column: "CodigoEquipo",
                principalTable: "GestionEquiposInformaticos_Equipos",
                principalColumn: "Codigo");

            migrationBuilder.AddForeignKey(
                name: "FK_GestionEquiposInformaticos_ConexionesEquipos_GestionEquiposInformaticos_Equipos_CodigoEquipo",
                table: "GestionEquiposInformaticos_ConexionesEquipos",
                column: "CodigoEquipo",
                principalTable: "GestionEquiposInformaticos_Equipos",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GestionEquiposInformaticos_Perifericos_GestionEquiposInformaticos_Equipos_CodigoEquipoAsignado",
                table: "GestionEquiposInformaticos_Perifericos",
                column: "CodigoEquipoAsignado",
                principalTable: "GestionEquiposInformaticos_Equipos",
                principalColumn: "Codigo");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanCronogramaEquipo_GestionEquiposInformaticos_Equipos_EquipoCodigo",
                table: "PlanCronogramaEquipo",
                column: "EquipoCodigo",
                principalTable: "GestionEquiposInformaticos_Equipos",
                principalColumn: "Codigo");

            migrationBuilder.AddForeignKey(
                name: "FK_SlotsRam_GestionEquiposInformaticos_Equipos_CodigoEquipo",
                table: "SlotsRam",
                column: "CodigoEquipo",
                principalTable: "GestionEquiposInformaticos_Equipos",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Discos_GestionEquiposInformaticos_Equipos_CodigoEquipo",
                table: "Discos");

            migrationBuilder.DropForeignKey(
                name: "FK_EjecucionSemanal_GestionEquiposInformaticos_Equipos_CodigoEquipo",
                table: "EjecucionSemanal");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionEquiposInformaticos_ConexionesEquipos_GestionEquiposInformaticos_Equipos_CodigoEquipo",
                table: "GestionEquiposInformaticos_ConexionesEquipos");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionEquiposInformaticos_Perifericos_GestionEquiposInformaticos_Equipos_CodigoEquipoAsignado",
                table: "GestionEquiposInformaticos_Perifericos");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanCronogramaEquipo_GestionEquiposInformaticos_Equipos_EquipoCodigo",
                table: "PlanCronogramaEquipo");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotsRam_GestionEquiposInformaticos_Equipos_CodigoEquipo",
                table: "SlotsRam");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionEquiposInformaticos_Perifericos",
                table: "GestionEquiposInformaticos_Perifericos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionEquiposInformaticos_Equipos",
                table: "GestionEquiposInformaticos_Equipos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionEquiposInformaticos_ConexionesEquipos",
                table: "GestionEquiposInformaticos_ConexionesEquipos");

            migrationBuilder.RenameTable(
                name: "GestionEquiposInformaticos_Perifericos",
                newName: "PerifericosEquiposInformaticos");

            migrationBuilder.RenameTable(
                name: "GestionEquiposInformaticos_Equipos",
                newName: "EquiposInformaticos");

            migrationBuilder.RenameTable(
                name: "GestionEquiposInformaticos_ConexionesEquipos",
                newName: "ConexionesEquiposInformaticos");

            migrationBuilder.RenameIndex(
                name: "IX_GestionEquiposInformaticos_Perifericos_CodigoEquipoAsignado",
                table: "PerifericosEquiposInformaticos",
                newName: "IX_PerifericosEquiposInformaticos_CodigoEquipoAsignado");

            migrationBuilder.RenameIndex(
                name: "IX_GestionEquiposInformaticos_ConexionesEquipos_CodigoEquipo",
                table: "ConexionesEquiposInformaticos",
                newName: "IX_ConexionesEquiposInformaticos_CodigoEquipo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PerifericosEquiposInformaticos",
                table: "PerifericosEquiposInformaticos",
                column: "Codigo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EquiposInformaticos",
                table: "EquiposInformaticos",
                column: "Codigo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConexionesEquiposInformaticos",
                table: "ConexionesEquiposInformaticos",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "MantenimientoPlantillaTarea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Predeterminada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MantenimientoPlantillaTarea", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeguimientoMantenimientoTarea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Completada = table.Column<bool>(type: "bit", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MantenimientoPlantillaTareaId = table.Column<int>(type: "int", nullable: true),
                    NombreTarea = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RepuestoUsado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SeguimientoMantenimientoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeguimientoMantenimientoTarea", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeguimientoMantenimientoTarea_SeguimientoMantenimientoId",
                table: "SeguimientoMantenimientoTarea",
                column: "SeguimientoMantenimientoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConexionesEquiposInformaticos_EquiposInformaticos_CodigoEquipo",
                table: "ConexionesEquiposInformaticos",
                column: "CodigoEquipo",
                principalTable: "EquiposInformaticos",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Discos_EquiposInformaticos_CodigoEquipo",
                table: "Discos",
                column: "CodigoEquipo",
                principalTable: "EquiposInformaticos",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EjecucionSemanal_EquiposInformaticos_CodigoEquipo",
                table: "EjecucionSemanal",
                column: "CodigoEquipo",
                principalTable: "EquiposInformaticos",
                principalColumn: "Codigo");

            migrationBuilder.AddForeignKey(
                name: "FK_PerifericosEquiposInformaticos_EquiposInformaticos_CodigoEquipoAsignado",
                table: "PerifericosEquiposInformaticos",
                column: "CodigoEquipoAsignado",
                principalTable: "EquiposInformaticos",
                principalColumn: "Codigo");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanCronogramaEquipo_EquiposInformaticos_EquipoCodigo",
                table: "PlanCronogramaEquipo",
                column: "EquipoCodigo",
                principalTable: "EquiposInformaticos",
                principalColumn: "Codigo");

            migrationBuilder.AddForeignKey(
                name: "FK_SlotsRam_EquiposInformaticos_CodigoEquipo",
                table: "SlotsRam",
                column: "CodigoEquipo",
                principalTable: "EquiposInformaticos",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
