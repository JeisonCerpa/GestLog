using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class RenameGestionEquiposMoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.EquiposInformaticos_MantenimientosCorrectivos','U') IS NOT NULL AND OBJECT_ID('dbo.GestionEquiposInformaticos_MantenimientosCorrectivos','U') IS NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_EquiposInformaticos_MantenimientosCorrectivos')
    BEGIN
        ALTER TABLE [EquiposInformaticos_MantenimientosCorrectivos] DROP CONSTRAINT [PK_EquiposInformaticos_MantenimientosCorrectivos];
    END

    EXEC sp_rename 'dbo.EquiposInformaticos_MantenimientosCorrectivos','GestionEquiposInformaticos_MantenimientosCorrectivos';

    IF OBJECT_ID('dbo.GestionEquiposInformaticos_MantenimientosCorrectivos','U') IS NOT NULL
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_MantenimientosCorrectivos')
        BEGIN
            ALTER TABLE [GestionEquiposInformaticos_MantenimientosCorrectivos] ADD CONSTRAINT [PK_GestionEquiposInformaticos_MantenimientosCorrectivos] PRIMARY KEY CLUSTERED ([Id]);
        END
    END
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.GestionEquiposInformaticos_MantenimientosCorrectivos','U') IS NOT NULL AND OBJECT_ID('dbo.EquiposInformaticos_MantenimientosCorrectivos','U') IS NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_MantenimientosCorrectivos')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_MantenimientosCorrectivos] DROP CONSTRAINT [PK_GestionEquiposInformaticos_MantenimientosCorrectivos];
    END

    EXEC sp_rename 'dbo.GestionEquiposInformaticos_MantenimientosCorrectivos','EquiposInformaticos_MantenimientosCorrectivos';

    IF OBJECT_ID('dbo.EquiposInformaticos_MantenimientosCorrectivos','U') IS NOT NULL
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_EquiposInformaticos_MantenimientosCorrectivos')
        BEGIN
            ALTER TABLE [EquiposInformaticos_MantenimientosCorrectivos] ADD CONSTRAINT [PK_EquiposInformaticos_MantenimientosCorrectivos] PRIMARY KEY CLUSTERED ([Id]);
        END
    END
END
");
        }
    }
}
