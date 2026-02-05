using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace GestLog.Migrations
{
    [DbContext(typeof(GestLog.Modules.DatabaseConnection.GestLogDbContext))]
    [Migration("20260204190000_RenameGestionUsuariosPersonas")]
    public partial class RenameGestionUsuariosPersonas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent T-SQL: rename old tables to new names if needed
            migrationBuilder.Sql(@"
SET NOCOUNT ON;
DECLARE @OldName sysname, @NewName sysname, @sql nvarchar(max);

-- Pairs ordered so lookups (TipoDocumento, Cargos) are renamed before dependent tables (Personas, Usuarios)
DECLARE @pairs TABLE (OldName sysname, NewName sysname, Ord int);
INSERT INTO @pairs (OldName, NewName, Ord) VALUES
  ('TipoDocumento','GestionUsuarios_TiposDocumento', 1),
  ('Cargos','GestionUsuarios_Cargos', 2),
  ('Usuarios','GestionUsuarios_Usuarios', 3),
  ('Roles','GestionUsuarios_Roles', 4),
  ('Permisos','GestionUsuarios_Permisos', 5),
  ('UsuarioRoles','GestionUsuarios_UsuarioRoles', 6),
  ('RolPermisos','GestionUsuarios_RolPermisos', 7),
  ('UsuarioPermisos','GestionUsuarios_UsuarioPermisos', 8),
  ('Auditoria','GestionUsuarios_Auditorias', 9),
  ('Personas','GestionPersonas_Personas', 10);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
  SELECT OldName, NewName FROM @pairs ORDER BY Ord;
OPEN cur;
FETCH NEXT FROM cur INTO @OldName, @NewName;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID('dbo.' + @OldName) IS NOT NULL AND OBJECT_ID('dbo.' + @NewName) IS NULL
    BEGIN
        PRINT 'Preparing rename dbo.' + @OldName + ' -> dbo.' + @NewName;

        -- Temp table to store FK creation statements
        IF OBJECT_ID('tempdb..#fks_to_recreate') IS NOT NULL DROP TABLE #fks_to_recreate;
        CREATE TABLE #fks_to_recreate (fkName sysname, parentSchema sysname, parentTable sysname, createSql nvarchar(max));

        -- Capture all FKs where the table is parent (owns FK) or referenced (target of FK)
        DECLARE fk_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT fk.object_id
        FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = OBJECT_ID('dbo.' + @OldName)
           OR fk.referenced_object_id = OBJECT_ID('dbo.' + @OldName);

        DECLARE @fkId int;
        OPEN fk_cursor;
        FETCH NEXT FROM fk_cursor INTO @fkId;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            BEGIN TRY
                DECLARE @fkName sysname, @parentObj int, @refObj int;
                SELECT @fkName = fk.name, @parentObj = fk.parent_object_id, @refObj = fk.referenced_object_id
                FROM sys.foreign_keys fk WHERE fk.object_id = @fkId;

                -- build parent and referenced column lists
                DECLARE @colsParent nvarchar(max) = '';
                DECLARE @colsRef nvarchar(max) = '';
                SELECT @colsParent = STUFF((
                    SELECT ',' + QUOTENAME(pc.name)
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
                    WHERE fkc.constraint_object_id = @fkId
                    ORDER BY fkc.constraint_column_id
                    FOR XML PATH('')
                ),1,1,'');
                SELECT @colsRef = STUFF((
                    SELECT ',' + QUOTENAME(rc.name)
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
                    WHERE fkc.constraint_object_id = @fkId
                    ORDER BY fkc.constraint_column_id
                    FOR XML PATH('')
                ),1,1,'');

                DECLARE @parentSchema sysname, @parentTable sysname, @refSchema sysname, @refTable sysname;
                SELECT @parentTable = o.name, @parentSchema = s.name FROM sys.objects o JOIN sys.schemas s ON o.schema_id = s.schema_id WHERE o.object_id = @parentObj;
                SELECT @refTable = o.name, @refSchema = s.name FROM sys.objects o JOIN sys.schemas s ON o.schema_id = s.schema_id WHERE o.object_id = @refObj;

                -- ON DELETE / ON UPDATE actions
                DECLARE @onDelete nvarchar(100) = '';
                DECLARE @onUpdate nvarchar(100) = '';
                SELECT @onDelete = CASE fk.delete_referential_action WHEN 0 THEN '' WHEN 1 THEN ' ON DELETE CASCADE' WHEN 2 THEN ' ON DELETE SET NULL' WHEN 3 THEN ' ON DELETE SET DEFAULT' END,
                       @onUpdate = CASE fk.update_referential_action WHEN 0 THEN '' WHEN 1 THEN ' ON UPDATE CASCADE' WHEN 2 THEN ' ON UPDATE SET NULL' WHEN 3 THEN ' ON UPDATE SET DEFAULT' END
                FROM sys.foreign_keys fk WHERE fk.object_id = @fkId;

                DECLARE @create nvarchar(max) = 'ALTER TABLE ' + QUOTENAME(@parentSchema) + '.' + QUOTENAME(@parentTable) +
                    ' ADD CONSTRAINT ' + QUOTENAME(@fkName) + ' FOREIGN KEY (' + @colsParent + ') REFERENCES ' + QUOTENAME(@refSchema) + '.' + QUOTENAME(@refTable) + ' (' + @colsRef + ')' + ISNULL(@onDelete,'') + ISNULL(@onUpdate,'');

                INSERT INTO #fks_to_recreate (fkName, parentSchema, parentTable, createSql) VALUES (@fkName, @parentSchema, @parentTable, @create);

                -- Drop constraint now (if exists)
                SET @sql = 'ALTER TABLE ' + QUOTENAME(@parentSchema) + '.' + QUOTENAME(@parentTable) + ' DROP CONSTRAINT ' + QUOTENAME(@fkName) + ';';
                BEGIN TRY
                    EXEC sp_executesql @sql;
                    PRINT 'Dropped FK ' + @fkName + ' on ' + @parentSchema + '.' + @parentTable;
                END TRY
                BEGIN CATCH
                    PRINT 'Warning dropping FK ' + ISNULL(@fkName,'(unknown)') + ': ' + ERROR_MESSAGE();
                END CATCH

            END TRY
            BEGIN CATCH
                PRINT 'Error capturing FK ' + CAST(@fkId AS nvarchar(20)) + ': ' + ERROR_MESSAGE();
            END CATCH

            FETCH NEXT FROM fk_cursor INTO @fkId;
        END
        CLOSE fk_cursor; DEALLOCATE fk_cursor;

        -- perform the table rename
        BEGIN TRY
            SET @sql = N'EXEC sp_rename ''' + 'dbo.' + @OldName + ''', ''' + @NewName + ''';';
            EXEC sp_executesql @sql;
            PRINT 'Renamed dbo.' + @OldName + ' -> dbo.' + @NewName;
        END TRY
        BEGIN CATCH
            PRINT 'Error renaming table dbo.' + @OldName + ': ' + ERROR_MESSAGE();
        END CATCH

        -- rename indexes that still contain the old table name
        DECLARE @idx nvarchar(128), @idxNew nvarchar(128);
        DECLARE idx_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT i.name
        FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID('dbo.' + @NewName) AND i.name LIKE '%' + @OldName + '%';
        OPEN idx_cur;
        FETCH NEXT FROM idx_cur INTO @idx;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @idxNew = REPLACE(@idx, @OldName, @NewName);
            SET @sql = N'EXEC sp_rename N''' + @NewName + '.' + @idx + ''', N''' + @idxNew + ''', N''INDEX'';';
            BEGIN TRY
                EXEC sp_executesql @sql;
                PRINT 'Renamed index ' + @idx + ' -> ' + @idxNew;
            END TRY
            BEGIN CATCH
                PRINT 'Warning renaming index ' + @idx + ': ' + ERROR_MESSAGE();
            END CATCH
            FETCH NEXT FROM idx_cur INTO @idx;
        END
        CLOSE idx_cur; DEALLOCATE idx_cur;

        -- recreate FKs using stored statements, replacing old table name tokens with new
        IF OBJECT_ID('tempdb..#fks_to_recreate') IS NOT NULL
        BEGIN
            DECLARE recreate_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT createSql FROM #fks_to_recreate;
            DECLARE @createStmt nvarchar(max);
            OPEN recreate_cursor;
            FETCH NEXT FROM recreate_cursor INTO @createStmt;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                BEGIN TRY
                    -- ensure any reference to [OldName] is replaced with [NewName]
                    SET @createStmt = REPLACE(@createStmt, QUOTENAME(@OldName), QUOTENAME(@NewName));
                    EXEC sp_executesql @createStmt;
                    PRINT 'Recreated FK: ' + LEFT(ISNULL(@createStmt, ''), 200);
                END TRY
                BEGIN CATCH
                    PRINT 'Warning recreating FK: ' + ERROR_MESSAGE();
                END CATCH
                FETCH NEXT FROM recreate_cursor INTO @createStmt;
            END
            CLOSE recreate_cursor; DEALLOCATE recreate_cursor;
            DROP TABLE #fks_to_recreate;
        END

    END
    ELSE
    BEGIN
        PRINT 'Skip rename for dbo.' + @OldName + ' (old or new state already present)';
    END
    FETCH NEXT FROM cur INTO @OldName, @NewName;
END
CLOSE cur; DEALLOCATE cur;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse renames if necessary (reverse order)
            migrationBuilder.Sql(@"
SET NOCOUNT ON;
DECLARE @OldName sysname, @NewName sysname, @sql nvarchar(max);

DECLARE @pairs TABLE (OldName sysname, NewName sysname, Ord int);
INSERT INTO @pairs (OldName, NewName, Ord) VALUES
  ('TipoDocumento','GestionUsuarios_TiposDocumento', 1),
  ('Cargos','GestionUsuarios_Cargos', 2),
  ('Usuarios','GestionUsuarios_Usuarios', 3),
  ('Roles','GestionUsuarios_Roles', 4),
  ('Permisos','GestionUsuarios_Permisos', 5),
  ('UsuarioRoles','GestionUsuarios_UsuarioRoles', 6),
  ('RolPermisos','GestionUsuarios_RolPermisos', 7),
  ('UsuarioPermisos','GestionUsuarios_UsuarioPermisos', 8),
  ('Auditoria','GestionUsuarios_Auditorias', 9),
  ('Personas','GestionPersonas_Personas', 10);

-- Process in reverse order to safely restore dependencies
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
  SELECT OldName, NewName FROM @pairs ORDER BY Ord DESC;
OPEN cur;
FETCH NEXT FROM cur INTO @OldName, @NewName;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID('dbo.' + @NewName) IS NOT NULL AND OBJECT_ID('dbo.' + @OldName) IS NULL
    BEGIN
        PRINT 'Preparing revert rename dbo.' + @NewName + ' -> dbo.' + @OldName;

        IF OBJECT_ID('tempdb..#fks_to_recreate') IS NOT NULL DROP TABLE #fks_to_recreate;
        CREATE TABLE #fks_to_recreate (fkName sysname, parentSchema sysname, parentTable sysname, createSql nvarchar(max));

        DECLARE fk_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT fk.object_id
        FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = OBJECT_ID('dbo.' + @NewName)
           OR fk.referenced_object_id = OBJECT_ID('dbo.' + @NewName);

        DECLARE @fkId int;
        OPEN fk_cursor;
        FETCH NEXT FROM fk_cursor INTO @fkId;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            BEGIN TRY
                DECLARE @fkName sysname, @parentObj int, @refObj int;
                SELECT @fkName = fk.name, @parentObj = fk.parent_object_id, @refObj = fk.referenced_object_id
                FROM sys.foreign_keys fk WHERE fk.object_id = @fkId;

                DECLARE @colsParent nvarchar(max) = '';
                DECLARE @colsRef nvarchar(max) = '';
                SELECT @colsParent = STUFF((
                    SELECT ',' + QUOTENAME(pc.name)
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
                    WHERE fkc.constraint_object_id = @fkId
                    ORDER BY fkc.constraint_column_id
                    FOR XML PATH('')
                ),1,1,'');
                SELECT @colsRef = STUFF((
                    SELECT ',' + QUOTENAME(rc.name)
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
                    WHERE fkc.constraint_object_id = @fkId
                    ORDER BY fkc.constraint_column_id
                    FOR XML PATH('')
                ),1,1,'');

                DECLARE @parentSchema sysname, @parentTable sysname, @refSchema sysname, @refTable sysname;
                SELECT @parentTable = o.name, @parentSchema = s.name FROM sys.objects o JOIN sys.schemas s ON o.schema_id = s.schema_id WHERE o.object_id = @parentObj;
                SELECT @refTable = o.name, @refSchema = s.name FROM sys.objects o JOIN sys.schemas s ON o.schema_id = s.schema_id WHERE o.object_id = @refObj;

                DECLARE @onDelete nvarchar(100) = '';
                DECLARE @onUpdate nvarchar(100) = '';
                SELECT @onDelete = CASE fk.delete_referential_action WHEN 0 THEN '' WHEN 1 THEN ' ON DELETE CASCADE' WHEN 2 THEN ' ON DELETE SET NULL' WHEN 3 THEN ' ON DELETE SET DEFAULT' END,
                       @onUpdate = CASE fk.update_referential_action WHEN 0 THEN '' WHEN 1 THEN ' ON UPDATE CASCADE' WHEN 2 THEN ' ON UPDATE SET NULL' WHEN 3 THEN ' ON UPDATE SET DEFAULT' END
                FROM sys.foreign_keys fk WHERE fk.object_id = @fkId;

                DECLARE @create nvarchar(max) = 'ALTER TABLE ' + QUOTENAME(@parentSchema) + '.' + QUOTENAME(@parentTable) +
                    ' ADD CONSTRAINT ' + QUOTENAME(@fkName) + ' FOREIGN KEY (' + @colsParent + ') REFERENCES ' + QUOTENAME(@refSchema) + '.' + QUOTENAME(@refTable) + ' (' + @colsRef + ')' + ISNULL(@onDelete,'') + ISNULL(@onUpdate,'');

                INSERT INTO #fks_to_recreate (fkName, parentSchema, parentTable, createSql) VALUES (@fkName, @parentSchema, @parentTable, @create);

                SET @sql = 'ALTER TABLE ' + QUOTENAME(@parentSchema) + '.' + QUOTENAME(@parentTable) + ' DROP CONSTRAINT ' + QUOTENAME(@fkName) + ';';
                BEGIN TRY
                    EXEC sp_executesql @sql;
                    PRINT 'Dropped FK ' + @fkName + ' on ' + @parentSchema + '.' + @parentTable;
                END TRY
                BEGIN CATCH
                    PRINT 'Warning dropping FK ' + ISNULL(@fkName,'(unknown)') + ': ' + ERROR_MESSAGE();
                END CATCH

            END TRY
            BEGIN CATCH
                PRINT 'Error capturing FK ' + CAST(@fkId AS nvarchar(20)) + ': ' + ERROR_MESSAGE();
            END CATCH

            FETCH NEXT FROM fk_cursor INTO @fkId;
        END
        CLOSE fk_cursor; DEALLOCATE fk_cursor;

        BEGIN TRY
            SET @sql = N'EXEC sp_rename ''' + 'dbo.' + @NewName + ''', ''' + @OldName + ''';';
            EXEC sp_executesql @sql;
            PRINT 'Renamed dbo.' + @NewName + ' -> dbo.' + @OldName;
        END TRY
        BEGIN CATCH
            PRINT 'Error renaming table dbo.' + @NewName + ': ' + ERROR_MESSAGE();
        END CATCH

        -- rename indexes back if necessary
        DECLARE @idx nvarchar(128), @idxNew nvarchar(128);
        DECLARE idx_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT i.name
        FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID('dbo.' + @OldName) AND i.name LIKE '%' + @NewName + '%';
        OPEN idx_cur;
        FETCH NEXT FROM idx_cur INTO @idx;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @idxNew = REPLACE(@idx, @NewName, @OldName);
            SET @sql = N'EXEC sp_rename N''' + @OldName + '.' + @idx + ''', N''' + @idxNew + ''', N''INDEX'';';
            BEGIN TRY
                EXEC sp_executesql @sql;
                PRINT 'Renamed index ' + @idx + ' -> ' + @idxNew;
            END TRY
            BEGIN CATCH
                PRINT 'Warning renaming index ' + @idx + ': ' + ERROR_MESSAGE();
            END CATCH
            FETCH NEXT FROM idx_cur INTO @idx;
        END
        CLOSE idx_cur; DEALLOCATE idx_cur;

        -- recreate FKs, replacing occurrences of NewName with OldName
        IF OBJECT_ID('tempdb..#fks_to_recreate') IS NOT NULL
        BEGIN
            DECLARE recreate_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT createSql FROM #fks_to_recreate;
            DECLARE @createStmt nvarchar(max);
            OPEN recreate_cursor;
            FETCH NEXT FROM recreate_cursor INTO @createStmt;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                BEGIN TRY
                    SET @createStmt = REPLACE(@createStmt, QUOTENAME(@NewName), QUOTENAME(@OldName));
                    EXEC sp_executesql @createStmt;
                    PRINT 'Recreated FK: ' + LEFT(ISNULL(@createStmt, ''), 200);
                END TRY
                BEGIN CATCH
                    PRINT 'Warning recreating FK: ' + ERROR_MESSAGE();
                END CATCH
                FETCH NEXT FROM recreate_cursor INTO @createStmt;
            END
            CLOSE recreate_cursor; DEALLOCATE recreate_cursor;
            DROP TABLE #fks_to_recreate;
        END

    END
    ELSE
    BEGIN
        PRINT 'Skip revert for dbo.' + @NewName + ' (new or old state already present)';
    END
    FETCH NEXT FROM cur INTO @OldName, @NewName;
END
CLOSE cur; DEALLOCATE cur;
");
        }
    }
}
