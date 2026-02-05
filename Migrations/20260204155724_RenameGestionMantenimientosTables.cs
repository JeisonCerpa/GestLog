using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class RenameGestionMantenimientosTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET NOCOUNT ON;

-- Rename Equipos -> GestionMantenimientos_Equipos (si existe y no ha sido renombrada)
IF OBJECT_ID(N'dbo.Equipos','U') IS NOT NULL AND OBJECT_ID(N'dbo.GestionMantenimientos_Equipos','U') IS NULL
BEGIN
    EXEC sp_rename N'dbo.Equipos', N'GestionMantenimientos_Equipos';
    -- (PK rename intentionally skipped to avoid ambiguity)
    -- Rename indexes that include the old table name
    DECLARE @idx nvarchar(128);
    DECLARE idx_cursor CURSOR FOR
        SELECT i.name FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.GestionMantenimientos_Equipos') AND i.is_primary_key = 0 AND i.name LIKE '%Equipos%';
    OPEN idx_cursor;
    FETCH NEXT FROM idx_cursor INTO @idx;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @idx IS NOT NULL AND @idx <> ''
        BEGIN
            DECLARE @oldIndex nvarchar(400) = N'dbo.GestionMantenimientos_Equipos.' + @idx;
            DECLARE @newIndex nvarchar(400) = REPLACE(@idx, 'Equipos', 'GestionMantenimientos_Equipos');
            EXEC sp_rename @objname = @oldIndex, @newname = @newIndex, @objtype = 'INDEX';
        END
        FETCH NEXT FROM idx_cursor INTO @idx;
    END
    CLOSE idx_cursor; DEALLOCATE idx_cursor;
END

-- Rename Cronogramas -> GestionMantenimientos_Cronogramas
IF OBJECT_ID(N'dbo.Cronogramas','U') IS NOT NULL AND OBJECT_ID(N'dbo.GestionMantenimientos_Cronogramas','U') IS NULL
BEGIN
    EXEC sp_rename N'dbo.Cronogramas', N'GestionMantenimientos_Cronogramas';
    -- (PK rename intentionally skipped)
    DECLARE @idx2 nvarchar(128);
    DECLARE idx_cursor2 CURSOR FOR
        SELECT i.name FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.GestionMantenimientos_Cronogramas') AND i.is_primary_key = 0 AND i.name LIKE '%Cronogramas%';
    OPEN idx_cursor2;
    FETCH NEXT FROM idx_cursor2 INTO @idx2;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @idx2 IS NOT NULL AND @idx2 <> ''
        BEGIN
            DECLARE @oldIndex2 nvarchar(400) = N'dbo.GestionMantenimientos_Cronogramas.' + @idx2;
            DECLARE @newIndex2 nvarchar(400) = REPLACE(@idx2, 'Cronogramas', 'GestionMantenimientos_Cronogramas');
            EXEC sp_rename @objname = @oldIndex2, @newname = @newIndex2, @objtype = 'INDEX';
        END
        FETCH NEXT FROM idx_cursor2 INTO @idx2;
    END
    CLOSE idx_cursor2; DEALLOCATE idx_cursor2;
END

-- Rename Seguimientos -> GestionMantenimientos_Seguimientos
IF OBJECT_ID(N'dbo.Seguimientos','U') IS NOT NULL AND OBJECT_ID(N'dbo.GestionMantenimientos_Seguimientos','U') IS NULL
BEGIN
    EXEC sp_rename N'dbo.Seguimientos', N'GestionMantenimientos_Seguimientos';
    -- (PK rename intentionally skipped)
    DECLARE @idx3 nvarchar(128);
    DECLARE idx_cursor3 CURSOR FOR
        SELECT i.name FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.GestionMantenimientos_Seguimientos') AND i.is_primary_key = 0 AND i.name LIKE '%Seguimientos%';
    OPEN idx_cursor3;
    FETCH NEXT FROM idx_cursor3 INTO @idx3;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @idx3 IS NOT NULL AND @idx3 <> ''
        BEGIN
            DECLARE @oldIndex3 nvarchar(400) = N'dbo.GestionMantenimientos_Seguimientos.' + @idx3;
            DECLARE @newIndex3 nvarchar(400) = REPLACE(@idx3, 'Seguimientos', 'GestionMantenimientos_Seguimientos');
            EXEC sp_rename @objname = @oldIndex3, @newname = @newIndex3, @objtype = 'INDEX';
        END
        FETCH NEXT FROM idx_cursor3 INTO @idx3;
    END
    CLOSE idx_cursor3; DEALLOCATE idx_cursor3;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET NOCOUNT ON;

-- Reverse rename: GestionMantenimientos_Equipos -> Equipos (si existe)
IF OBJECT_ID(N'dbo.GestionMantenimientos_Equipos','U') IS NOT NULL AND OBJECT_ID(N'dbo.Equipos','U') IS NULL
BEGIN
    EXEC sp_rename N'dbo.GestionMantenimientos_Equipos', N'Equipos';
    -- (PK rename intentionally skipped)
    DECLARE @idxr nvarchar(128);
    DECLARE idx_cursorr CURSOR FOR
        SELECT i.name FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.Equipos') AND i.is_primary_key = 0 AND i.name LIKE '%GestionMantenimientos_Equipos%';
    OPEN idx_cursorr;
    FETCH NEXT FROM idx_cursorr INTO @idxr;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @idxr IS NOT NULL AND @idxr <> ''
        BEGIN
            DECLARE @oldIndexR nvarchar(400) = N'dbo.Equipos.' + @idxr;
            DECLARE @newIndexR nvarchar(400) = REPLACE(@idxr, 'GestionMantenimientos_Equipos', 'Equipos');
            EXEC sp_rename @objname = @oldIndexR, @newname = @newIndexR, @objtype = 'INDEX';
        END
        FETCH NEXT FROM idx_cursorr INTO @idxr;
    END
    CLOSE idx_cursorr; DEALLOCATE idx_cursorr;
END

-- Reverse rename: GestionMantenimientos_Cronogramas -> Cronogramas
IF OBJECT_ID(N'dbo.GestionMantenimientos_Cronogramas','U') IS NOT NULL AND OBJECT_ID(N'dbo.Cronogramas','U') IS NULL
BEGIN
    EXEC sp_rename N'dbo.GestionMantenimientos_Cronogramas', N'Cronogramas';
    -- (PK rename intentionally skipped)
    DECLARE @idxr2 nvarchar(128);
    DECLARE idx_cursorr2 CURSOR FOR
        SELECT i.name FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.Cronogramas') AND i.is_primary_key = 0 AND i.name LIKE '%GestionMantenimientos_Cronogramas%';
    OPEN idx_cursorr2;
    FETCH NEXT FROM idx_cursorr2 INTO @idxr2;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @idxr2 IS NOT NULL AND @idxr2 <> ''
        BEGIN
            DECLARE @oldIndexR2 nvarchar(400) = N'dbo.Cronogramas.' + @idxr2;
            DECLARE @newIndexR2 nvarchar(400) = REPLACE(@idxr2, 'GestionMantenimientos_Cronogramas', 'Cronogramas');
            EXEC sp_rename @objname = @oldIndexR2, @newname = @newIndexR2, @objtype = 'INDEX';
        END
        FETCH NEXT FROM idx_cursorr2 INTO @idxr2;
    END
    CLOSE idx_cursorr2; DEALLOCATE idx_cursorr2;
END

-- Reverse rename: GestionMantenimientos_Seguimientos -> Seguimientos
IF OBJECT_ID(N'dbo.GestionMantenimientos_Seguimientos','U') IS NOT NULL AND OBJECT_ID(N'dbo.Seguimientos','U') IS NULL
BEGIN
    EXEC sp_rename N'dbo.GestionMantenimientos_Seguimientos', N'Seguimientos';
    -- (PK rename intentionally skipped)
    DECLARE @idxr3 nvarchar(128);
    DECLARE idx_cursorr3 CURSOR FOR
        SELECT i.name FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.Seguimientos') AND i.is_primary_key = 0 AND i.name LIKE '%GestionMantenimientos_Seguimientos%';
    OPEN idx_cursorr3;
    FETCH NEXT FROM idx_cursorr3 INTO @idxr3;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @idxr3 IS NOT NULL AND @idxr3 <> ''
        BEGIN
            DECLARE @oldIndexR3 nvarchar(400) = N'dbo.Seguimientos.' + @idxr3;
            DECLARE @newIndexR3 nvarchar(400) = REPLACE(@idxr3, 'GestionMantenimientos_Seguimientos', 'Seguimientos');
            EXEC sp_rename @objname = @oldIndexR3, @newname = @newIndexR3, @objtype = 'INDEX';
        END
        FETCH NEXT FROM idx_cursorr3 INTO @idxr3;
    END
    CLOSE idx_cursorr3; DEALLOCATE idx_cursorr3;
END
");
        }
    }
}
