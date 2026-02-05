using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class RenameGestionEquiposInformaticosRemaining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Renombrar SlotsRam -> GestionEquiposInformaticos_SlotsRam (idempotente)
IF OBJECT_ID('dbo.SlotsRam','U') IS NOT NULL AND OBJECT_ID('dbo.GestionEquiposInformaticos_SlotsRam','U') IS NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SlotsRam_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [SlotsRam] DROP CONSTRAINT [FK_SlotsRam_GestionEquiposInformaticos_Equipos_CodigoEquipo];
    END
    IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_SlotsRam')
    BEGIN
        ALTER TABLE [SlotsRam] DROP CONSTRAINT [PK_SlotsRam];
    END

    EXEC sp_rename 'dbo.SlotsRam','GestionEquiposInformaticos_SlotsRam';

    -- Renombrar índices si existen
    IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_SlotsRam_CodigoEquipo' AND object_id = OBJECT_ID('dbo.GestionEquiposInformaticos_SlotsRam'))
    BEGIN
        EXEC sp_rename N'[GestionEquiposInformaticos_SlotsRam].[IX_SlotsRam_CodigoEquipo]', N'IX_GestionEquiposInformaticos_SlotsRam_CodigoEquipo', 'INDEX';
    END

    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_SlotsRam')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_SlotsRam] ADD CONSTRAINT [PK_GestionEquiposInformaticos_SlotsRam] PRIMARY KEY CLUSTERED ([Id]);
    END

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GestionEquiposInformaticos_SlotsRam_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_SlotsRam] WITH NOCHECK ADD CONSTRAINT [FK_GestionEquiposInformaticos_SlotsRam_GestionEquiposInformaticos_Equipos_CodigoEquipo] FOREIGN KEY([CodigoEquipo]) REFERENCES [GestionEquiposInformaticos_Equipos]([Codigo]) ON DELETE CASCADE;
    END
END

-- Renombrar PlanCronogramaEquipo -> GestionEquiposInformaticos_PlanCronogramaEquipo
IF OBJECT_ID('dbo.PlanCronogramaEquipo','U') IS NOT NULL AND OBJECT_ID('dbo.GestionEquiposInformaticos_PlanCronogramaEquipo','U') IS NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PlanCronogramaEquipo_GestionEquiposInformaticos_Equipos_EquipoCodigo')
    BEGIN
        ALTER TABLE [PlanCronogramaEquipo] DROP CONSTRAINT [FK_PlanCronogramaEquipo_GestionEquiposInformaticos_Equipos_EquipoCodigo];
    END

    -- Intento directo: eliminar FK conocido que referencia PK_PlanCronogramaEquipo (por seguridad)
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId')
    BEGIN
        ALTER TABLE [EjecucionSemanal] DROP CONSTRAINT [FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId];
    END

    -- Eliminar cualquier FK que apunte a PlanCronogramaEquipo (ej. EjecucionSemanal) para permitir eliminar la PK de forma segura
    DECLARE @fkName sysname, @parentSchema sysname, @parentTable sysname, @sql nvarchar(max);
    DECLARE fk_cursor CURSOR FOR
        SELECT fk.name, OBJECT_SCHEMA_NAME(fk.parent_object_id), OBJECT_NAME(fk.parent_object_id)
        FROM sys.foreign_keys fk
        WHERE fk.referenced_object_id = OBJECT_ID('dbo.PlanCronogramaEquipo');
    OPEN fk_cursor;
    FETCH NEXT FROM fk_cursor INTO @fkName, @parentSchema, @parentTable;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = 'ALTER TABLE [' + @parentSchema + '].[' + @parentTable + '] DROP CONSTRAINT [' + @fkName + ']';
        EXEC sp_executesql @sql;
        FETCH NEXT FROM fk_cursor INTO @fkName, @parentSchema, @parentTable;
    END
    CLOSE fk_cursor;
    DEALLOCATE fk_cursor;

    IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PlanCronogramaEquipo')
    BEGIN
        ALTER TABLE [PlanCronogramaEquipo] DROP CONSTRAINT [PK_PlanCronogramaEquipo];
    END

    EXEC sp_rename 'dbo.PlanCronogramaEquipo','GestionEquiposInformaticos_PlanCronogramaEquipo';

    IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_PlanCronogramaEquipo_EquipoCodigo' AND object_id = OBJECT_ID('dbo.GestionEquiposInformaticos_PlanCronogramaEquipo'))
    BEGIN
        EXEC sp_rename N'[GestionEquiposInformaticos_PlanCronogramaEquipo].[IX_PlanCronogramaEquipo_EquipoCodigo]', N'IX_GestionEquiposInformaticos_PlanCronogramaEquipo_EquipoCodigo', 'INDEX';
    END

    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_PlanCronogramaEquipo')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_PlanCronogramaEquipo] ADD CONSTRAINT [PK_GestionEquiposInformaticos_PlanCronogramaEquipo] PRIMARY KEY CLUSTERED ([PlanId]);
    END

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GestionEquiposInformaticos_PlanCronogramaEquipo_GestionEquiposInformaticos_Equipos_EquipoCodigo')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_PlanCronogramaEquipo] WITH NOCHECK ADD CONSTRAINT [FK_GestionEquiposInformaticos_PlanCronogramaEquipo_GestionEquiposInformaticos_Equipos_EquipoCodigo] FOREIGN KEY([EquipoCodigo]) REFERENCES [GestionEquiposInformaticos_Equipos]([Codigo]);
    END
END

-- Renombrar EjecucionSemanal -> GestionEquiposInformaticos_EjecucionSemanal
IF OBJECT_ID('dbo.EjecucionSemanal','U') IS NOT NULL AND OBJECT_ID('dbo.GestionEquiposInformaticos_EjecucionSemanal','U') IS NULL
BEGIN
    -- quitar FK a Plan y a Equipos si existen
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId')
    BEGIN
        ALTER TABLE [EjecucionSemanal] DROP CONSTRAINT [FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId];
    END
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EjecucionSemanal_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [EjecucionSemanal] DROP CONSTRAINT [FK_EjecucionSemanal_GestionEquiposInformaticos_Equipos_CodigoEquipo];
    END
    IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_EjecucionSemanal')
    BEGIN
        ALTER TABLE [EjecucionSemanal] DROP CONSTRAINT [PK_EjecucionSemanal];
    END

    EXEC sp_rename 'dbo.EjecucionSemanal','GestionEquiposInformaticos_EjecucionSemanal';

    IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_EjecucionSemanal_PlanId' AND object_id = OBJECT_ID('dbo.GestionEquiposInformaticos_EjecucionSemanal'))
    BEGIN
        EXEC sp_rename N'[GestionEquiposInformaticos_EjecucionSemanal].[IX_EjecucionSemanal_PlanId]', N'IX_GestionEquiposInformaticos_EjecucionSemanal_PlanId', 'INDEX';
    END
    IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_EjecucionSemanal_CodigoEquipo_AnioISO_SemanaISO' AND object_id = OBJECT_ID('dbo.GestionEquiposInformaticos_EjecucionSemanal'))
    BEGIN
        EXEC sp_rename N'[GestionEquiposInformaticos_EjecucionSemanal].[IX_EjecucionSemanal_CodigoEquipo_AnioISO_SemanaISO]', N'IX_GestionEquiposInformaticos_EjecucionSemanal_CodigoEquipo_AnioISO_SemanaISO', 'INDEX';
    END

    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_EjecucionSemanal')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_EjecucionSemanal] ADD CONSTRAINT [PK_GestionEquiposInformaticos_EjecucionSemanal] PRIMARY KEY CLUSTERED ([EjecucionId]);
    END

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GestionEquiposInformaticos_EjecucionSemanal_GestionEquiposInformaticos_PlanCronogramaEquipo_PlanId')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_EjecucionSemanal] WITH NOCHECK ADD CONSTRAINT [FK_GestionEquiposInformaticos_EjecucionSemanal_GestionEquiposInformaticos_PlanCronogramaEquipo_PlanId] FOREIGN KEY([PlanId]) REFERENCES [GestionEquiposInformaticos_PlanCronogramaEquipo]([PlanId]) ON DELETE SET NULL;
    END
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GestionEquiposInformaticos_EjecucionSemanal_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_EjecucionSemanal] WITH NOCHECK ADD CONSTRAINT [FK_GestionEquiposInformaticos_EjecucionSemanal_GestionEquiposInformaticos_Equipos_CodigoEquipo] FOREIGN KEY([CodigoEquipo]) REFERENCES [GestionEquiposInformaticos_Equipos]([Codigo]);
    END
END

-- Renombrar Discos -> GestionEquiposInformaticos_Discos
IF OBJECT_ID('dbo.Discos','U') IS NOT NULL AND OBJECT_ID('dbo.GestionEquiposInformaticos_Discos','U') IS NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Discos_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [Discos] DROP CONSTRAINT [FK_Discos_GestionEquiposInformaticos_Equipos_CodigoEquipo];
    END
    IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Discos')
    BEGIN
        ALTER TABLE [Discos] DROP CONSTRAINT [PK_Discos];
    END

    EXEC sp_rename 'dbo.Discos','GestionEquiposInformaticos_Discos';

    IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_Discos_CodigoEquipo' AND object_id = OBJECT_ID('dbo.GestionEquiposInformaticos_Discos'))
    BEGIN
        EXEC sp_rename N'[GestionEquiposInformaticos_Discos].[IX_Discos_CodigoEquipo]', N'IX_GestionEquiposInformaticos_Discos_CodigoEquipo', 'INDEX';
    END

    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_Discos')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_Discos] ADD CONSTRAINT [PK_GestionEquiposInformaticos_Discos] PRIMARY KEY CLUSTERED ([Id]);
    END

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GestionEquiposInformaticos_Discos_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_Discos] WITH NOCHECK ADD CONSTRAINT [FK_GestionEquiposInformaticos_Discos_GestionEquiposInformaticos_Equipos_CodigoEquipo] FOREIGN KEY([CodigoEquipo]) REFERENCES [GestionEquiposInformaticos_Equipos]([Codigo]) ON DELETE CASCADE;
    END
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Revertir Discos
IF OBJECT_ID('dbo.GestionEquiposInformaticos_Discos','U') IS NOT NULL AND OBJECT_ID('dbo.Discos','U') IS NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_Discos')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_Discos] DROP CONSTRAINT [PK_GestionEquiposInformaticos_Discos];
    END
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GestionEquiposInformaticos_Discos_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_Discos] DROP CONSTRAINT [FK_GestionEquiposInformaticos_Discos_GestionEquiposInformaticos_Equipos_CodigoEquipo];
    END

    EXEC sp_rename 'dbo.GestionEquiposInformaticos_Discos','Discos';

    IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_GestionEquiposInformaticos_Discos_CodigoEquipo' AND object_id = OBJECT_ID('dbo.Discos'))
    BEGIN
        EXEC sp_rename N'[Discos].[IX_GestionEquiposInformaticos_Discos_CodigoEquipo]', N'IX_Discos_CodigoEquipo', 'INDEX';
    END

    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Discos')
    BEGIN
        ALTER TABLE [Discos] ADD CONSTRAINT [PK_Discos] PRIMARY KEY CLUSTERED ([Id]);
    END

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Discos_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [Discos] WITH NOCHECK ADD CONSTRAINT [FK_Discos_GestionEquiposInformaticos_Equipos_CodigoEquipo] FOREIGN KEY([CodigoEquipo]) REFERENCES [GestionEquiposInformaticos_Equipos]([Codigo]) ON DELETE CASCADE;
    END
END

-- Revertir EjecucionSemanal
IF OBJECT_ID('dbo.GestionEquiposInformaticos_EjecucionSemanal','U') IS NOT NULL AND OBJECT_ID('dbo.EjecucionSemanal','U') IS NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_EjecucionSemanal')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_EjecucionSemanal] DROP CONSTRAINT [PK_GestionEquiposInformaticos_EjecucionSemanal];
    END
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GestionEquiposInformaticos_EjecucionSemanal_GestionEquiposInformaticos_PlanCronogramaEquipo_PlanId')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_EjecucionSemanal] DROP CONSTRAINT [FK_GestionEquiposInformaticos_EjecucionSemanal_GestionEquiposInformaticos_PlanCronogramaEquipo_PlanId];
    END
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GestionEquiposInformaticos_EjecucionSemanal_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_EjecucionSemanal] DROP CONSTRAINT [FK_GestionEquiposInformaticos_EjecucionSemanal_GestionEquiposInformaticos_Equipos_CodigoEquipo];
    END

    EXEC sp_rename 'dbo.GestionEquiposInformaticos_EjecucionSemanal','EjecucionSemanal';

    IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_GestionEquiposInformaticos_EjecucionSemanal_PlanId' AND object_id = OBJECT_ID('dbo.EjecucionSemanal'))
    BEGIN
        EXEC sp_rename N'[EjecucionSemanal].[IX_GestionEquiposInformaticos_EjecucionSemanal_PlanId]', N'IX_EjecucionSemanal_PlanId', 'INDEX';
    END
    IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_GestionEquiposInformaticos_EjecucionSemanal_CodigoEquipo_AnioISO_SemanaISO' AND object_id = OBJECT_ID('dbo.EjecucionSemanal'))
    BEGIN
        EXEC sp_rename N'[EjecucionSemanal].[IX_GestionEquiposInformaticos_EjecucionSemanal_CodigoEquipo_AnioISO_SemanaISO]', N'IX_EjecucionSemanal_CodigoEquipo_AnioISO_SemanaISO', 'INDEX';
    END

    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_EjecucionSemanal')
    BEGIN
        ALTER TABLE [EjecucionSemanal] ADD CONSTRAINT [PK_EjecucionSemanal] PRIMARY KEY CLUSTERED ([EjecucionId]);
    END

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId')
    BEGIN
        ALTER TABLE [EjecucionSemanal] WITH NOCHECK ADD CONSTRAINT [FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId] FOREIGN KEY([PlanId]) REFERENCES [PlanCronogramaEquipo]([PlanId]) ON DELETE SET NULL;
    END
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EjecucionSemanal_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [EjecucionSemanal] WITH NOCHECK ADD CONSTRAINT [FK_EjecucionSemanal_GestionEquiposInformaticos_Equipos_CodigoEquipo] FOREIGN KEY([CodigoEquipo]) REFERENCES [GestionEquiposInformaticos_Equipos]([Codigo]);
    END
END

-- Revertir PlanCronogramaEquipo
IF OBJECT_ID('dbo.GestionEquiposInformaticos_PlanCronogramaEquipo','U') IS NOT NULL AND OBJECT_ID('dbo.PlanCronogramaEquipo','U') IS NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_PlanCronogramaEquipo')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_PlanCronogramaEquipo] DROP CONSTRAINT [PK_GestionEquiposInformaticos_PlanCronogramaEquipo];
    END
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GestionEquiposInformaticos_PlanCronogramaEquipo_GestionEquiposInformaticos_Equipos_EquipoCodigo')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_PlanCronogramaEquipo] DROP CONSTRAINT [FK_GestionEquiposInformaticos_PlanCronogramaEquipo_GestionEquiposInformaticos_Equipos_EquipoCodigo];
    END

    EXEC sp_rename 'dbo.GestionEquiposInformaticos_PlanCronogramaEquipo','PlanCronogramaEquipo';

    IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_GestionEquiposInformaticos_PlanCronogramaEquipo_EquipoCodigo' AND object_id = OBJECT_ID('dbo.PlanCronogramaEquipo'))
    BEGIN
        EXEC sp_rename N'[PlanCronogramaEquipo].[IX_GestionEquiposInformaticos_PlanCronogramaEquipo_EquipoCodigo]', N'IX_PlanCronogramaEquipo_EquipoCodigo', 'INDEX';
    END

    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PlanCronogramaEquipo')
    BEGIN
        ALTER TABLE [PlanCronogramaEquipo] ADD CONSTRAINT [PK_PlanCronogramaEquipo] PRIMARY KEY CLUSTERED ([PlanId]);
    END

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PlanCronogramaEquipo_GestionEquiposInformaticos_Equipos_EquipoCodigo')
    BEGIN
        ALTER TABLE [PlanCronogramaEquipo] WITH NOCHECK ADD CONSTRAINT [FK_PlanCronogramaEquipo_GestionEquiposInformaticos_Equipos_EquipoCodigo] FOREIGN KEY([EquipoCodigo]) REFERENCES [GestionEquiposInformaticos_Equipos]([Codigo]);
    END
END

-- Revertir SlotsRam
IF OBJECT_ID('dbo.GestionEquiposInformaticos_SlotsRam','U') IS NOT NULL AND OBJECT_ID('dbo.SlotsRam','U') IS NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_GestionEquiposInformaticos_SlotsRam')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_SlotsRam] DROP CONSTRAINT [PK_GestionEquiposInformaticos_SlotsRam];
    END
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GestionEquiposInformaticos_SlotsRam_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [GestionEquiposInformaticos_SlotsRam] DROP CONSTRAINT [FK_GestionEquiposInformaticos_SlotsRam_GestionEquiposInformaticos_Equipos_CodigoEquipo];
    END

    EXEC sp_rename 'dbo.GestionEquiposInformaticos_SlotsRam','SlotsRam';

    IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_GestionEquiposInformaticos_SlotsRam_CodigoEquipo' AND object_id = OBJECT_ID('dbo.SlotsRam'))
    BEGIN
        EXEC sp_rename N'[SlotsRam].[IX_GestionEquiposInformaticos_SlotsRam_CodigoEquipo]', N'IX_SlotsRam_CodigoEquipo', 'INDEX';
    END

    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE name = 'PK_SlotsRam')
    BEGIN
        ALTER TABLE [SlotsRam] ADD CONSTRAINT [PK_SlotsRam] PRIMARY KEY CLUSTERED ([Id]);
    END

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SlotsRam_GestionEquiposInformaticos_Equipos_CodigoEquipo')
    BEGIN
        ALTER TABLE [SlotsRam] WITH NOCHECK ADD CONSTRAINT [FK_SlotsRam_GestionEquiposInformaticos_Equipos_CodigoEquipo] FOREIGN KEY([CodigoEquipo]) REFERENCES [GestionEquiposInformaticos_Equipos]([Codigo]) ON DELETE CASCADE;
    END
END
");
        }
    }
}
