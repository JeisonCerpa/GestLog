using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class FixRenameSeguimientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SET NOCOUNT ON; SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Seguimientos','U') IS NOT NULL AND OBJECT_ID(N'dbo.GestionMantenimientos_Seguimientos','U') IS NULL
BEGIN
    BEGIN TRAN;

    CREATE TABLE #FkToRecreate (
        fk_name sysname,
        parent_schema sysname,
        parent_table sysname,
        parent_cols nvarchar(max),
        referenced_schema sysname,
        referenced_table sysname,
        referenced_cols nvarchar(max),
        delete_action tinyint,
        update_action tinyint
    );

    DECLARE fk_cursor CURSOR FOR
    SELECT fk.object_id, fk.name
    FROM sys.foreign_keys fk
    WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.Seguimientos');

    DECLARE @fk_objid INT; DECLARE @fk_name sysname;
    OPEN fk_cursor;
    FETCH NEXT FROM fk_cursor INTO @fk_objid, @fk_name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @parent_schema sysname, @parent_table sysname;
        SELECT @parent_schema = SCHEMA_NAME(o.schema_id), @parent_table = OBJECT_NAME(fk.parent_object_id)
        FROM sys.foreign_keys fk
        JOIN sys.objects o ON fk.parent_object_id = o.object_id
        WHERE fk.object_id = @fk_objid;

        DECLARE @parent_cols nvarchar(max) = N''; DECLARE @referenced_cols nvarchar(max) = N'';

        SELECT @parent_cols = STUFF((
            SELECT ',' + QUOTENAME(pc.name)
            FROM sys.foreign_key_columns fkc2
            JOIN sys.columns pc ON pc.object_id = fkc2.parent_object_id AND pc.column_id = fkc2.parent_column_id
            WHERE fkc2.constraint_object_id = @fk_objid
            ORDER BY fkc2.constraint_column_id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'),1,1,'');

        SELECT @referenced_cols = STUFF((
            SELECT ',' + QUOTENAME(rc.name)
            FROM sys.foreign_key_columns fkc2
            JOIN sys.columns rc ON rc.object_id = fkc2.referenced_object_id AND rc.column_id = fkc2.referenced_column_id
            WHERE fkc2.constraint_object_id = @fk_objid
            ORDER BY fkc2.constraint_column_id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'),1,1,'');

        DECLARE @del_action tinyint, @upd_action tinyint;
        SELECT @del_action = fk.delete_referential_action, @upd_action = fk.update_referential_action FROM sys.foreign_keys fk WHERE fk.object_id = @fk_objid;

        INSERT INTO #FkToRecreate (fk_name, parent_schema, parent_table, parent_cols, referenced_schema, referenced_table, referenced_cols, delete_action, update_action)
        VALUES (@fk_name, @parent_schema, @parent_table, @parent_cols, N'dbo', N'Seguimientos', @referenced_cols, @del_action, @upd_action);

        DECLARE @sql_drop nvarchar(max) = N'ALTER TABLE ' + QUOTENAME(@parent_schema) + N'.' + QUOTENAME(@parent_table) + N' DROP CONSTRAINT ' + QUOTENAME(@fk_name) + N';';
        EXEC sp_executesql @sql_drop;

        FETCH NEXT FROM fk_cursor INTO @fk_objid, @fk_name;
    END
    CLOSE fk_cursor; DEALLOCATE fk_cursor;

    -- Rename table
    EXEC sp_rename N'dbo.Seguimientos', N'GestionMantenimientos_Seguimientos';

    -- Recreate FKs pointing to new table name
    DECLARE recreate_cursor CURSOR FOR
        SELECT fk_name, parent_schema, parent_table, parent_cols, referenced_schema, referenced_table, referenced_cols, delete_action, update_action FROM #FkToRecreate;

    DECLARE @p_fk_name sysname, @p_parent_schema sysname, @p_parent_table sysname, @p_parent_cols nvarchar(max), @p_ref_schema sysname, @p_ref_table sysname, @p_ref_cols nvarchar(max);
    DECLARE @p_del tinyint, @p_upd tinyint;
    OPEN recreate_cursor;
    FETCH NEXT FROM recreate_cursor INTO @p_fk_name, @p_parent_schema, @p_parent_table, @p_parent_cols, @p_ref_schema, @p_ref_table, @p_ref_cols, @p_del, @p_upd;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @p_ref_schema = N'dbo' AND @p_ref_table = N'Seguimientos' SET @p_ref_table = N'GestionMantenimientos_Seguimientos';
        DECLARE @sql_create nvarchar(max) = N'ALTER TABLE ' + QUOTENAME(@p_parent_schema) + N'.' + QUOTENAME(@p_parent_table) + N' ADD CONSTRAINT ' + QUOTENAME(@p_fk_name) + N' FOREIGN KEY(' + @p_parent_cols + N') REFERENCES ' + QUOTENAME(@p_ref_schema) + N'.' + QUOTENAME(@p_ref_table) + N'(' + @p_ref_cols + N')';
        IF @p_del = 1 SET @sql_create = @sql_create + N' ON DELETE CASCADE';
        ELSE IF @p_del = 2 SET @sql_create = @sql_create + N' ON DELETE SET NULL';
        ELSE IF @p_del = 3 SET @sql_create = @sql_create + N' ON DELETE SET DEFAULT';
        IF @p_upd = 1 SET @sql_create = @sql_create + N' ON UPDATE CASCADE';
        ELSE IF @p_upd = 2 SET @sql_create = @sql_create + N' ON UPDATE SET NULL';
        ELSE IF @p_upd = 3 SET @sql_create = @sql_create + N' ON UPDATE SET DEFAULT';
        EXEC sp_executesql @sql_create;
        FETCH NEXT FROM recreate_cursor INTO @p_fk_name, @p_parent_schema, @p_parent_table, @p_parent_cols, @p_ref_schema, @p_ref_table, @p_ref_cols, @p_del, @p_upd;
    END
    CLOSE recreate_cursor; DEALLOCATE recreate_cursor;

    DROP TABLE #FkToRecreate;
    COMMIT;
END
ELSE
BEGIN
    PRINT 'No action: either source does not exist or target already present.';
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SET NOCOUNT ON; SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.GestionMantenimientos_Seguimientos','U') IS NOT NULL AND OBJECT_ID(N'dbo.Seguimientos','U') IS NULL
BEGIN
    BEGIN TRAN;

    CREATE TABLE #FkToRecreate (
        fk_name sysname,
        parent_schema sysname,
        parent_table sysname,
        parent_cols nvarchar(max),
        referenced_schema sysname,
        referenced_table sysname,
        referenced_cols nvarchar(max),
        delete_action tinyint,
        update_action tinyint
    );

    DECLARE fk_cursor CURSOR FOR
    SELECT fk.object_id, fk.name
    FROM sys.foreign_keys fk
    WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.GestionMantenimientos_Seguimientos');

    DECLARE @fk_objid INT; DECLARE @fk_name sysname;
    OPEN fk_cursor;
    FETCH NEXT FROM fk_cursor INTO @fk_objid, @fk_name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @parent_schema sysname, @parent_table sysname;
        SELECT @parent_schema = SCHEMA_NAME(o.schema_id), @parent_table = OBJECT_NAME(fk.parent_object_id)
        FROM sys.foreign_keys fk
        JOIN sys.objects o ON fk.parent_object_id = o.object_id
        WHERE fk.object_id = @fk_objid;

        DECLARE @parent_cols nvarchar(max) = N''; DECLARE @referenced_cols nvarchar(max) = N'';

        SELECT @parent_cols = STUFF((
            SELECT ',' + QUOTENAME(pc.name)
            FROM sys.foreign_key_columns fkc2
            JOIN sys.columns pc ON pc.object_id = fkc2.parent_object_id AND pc.column_id = fkc2.parent_column_id
            WHERE fkc2.constraint_object_id = @fk_objid
            ORDER BY fkc2.constraint_column_id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'),1,1,'');

        SELECT @referenced_cols = STUFF((
            SELECT ',' + QUOTENAME(rc.name)
            FROM sys.foreign_key_columns fkc2
            JOIN sys.columns rc ON rc.object_id = fkc2.referenced_object_id AND rc.column_id = fkc2.referenced_column_id
            WHERE fkc2.constraint_object_id = @fk_objid
            ORDER BY fkc2.constraint_column_id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'),1,1,'');

        DECLARE @del_action tinyint, @upd_action tinyint;
        SELECT @del_action = fk.delete_referential_action, @upd_action = fk.update_referential_action FROM sys.foreign_keys fk WHERE fk.object_id = @fk_objid;

        INSERT INTO #FkToRecreate (fk_name, parent_schema, parent_table, parent_cols, referenced_schema, referenced_table, referenced_cols, delete_action, update_action)
        VALUES (@fk_name, @parent_schema, @parent_table, @parent_cols, N'dbo', N'GestionMantenimientos_Seguimientos', @referenced_cols, @del_action, @upd_action);

        DECLARE @sql_drop nvarchar(max) = N'ALTER TABLE ' + QUOTENAME(@parent_schema) + N'.' + QUOTENAME(@parent_table) + N' DROP CONSTRAINT ' + QUOTENAME(@fk_name) + N';';
        EXEC sp_executesql @sql_drop;

        FETCH NEXT FROM fk_cursor INTO @fk_objid, @fk_name;
    END
    CLOSE fk_cursor; DEALLOCATE fk_cursor;

    -- Rename table back
    EXEC sp_rename N'dbo.GestionMantenimientos_Seguimientos', N'Seguimientos';

    -- Recreate FKs pointing to original table name
    DECLARE recreate_cursor CURSOR FOR
        SELECT fk_name, parent_schema, parent_table, parent_cols, referenced_schema, referenced_table, referenced_cols, delete_action, update_action FROM #FkToRecreate;

    DECLARE @p_fk_name sysname, @p_parent_schema sysname, @p_parent_table sysname, @p_parent_cols nvarchar(max), @p_ref_schema sysname, @p_ref_table sysname, @p_ref_cols nvarchar(max);
    DECLARE @p_del tinyint, @p_upd tinyint;
    OPEN recreate_cursor;
    FETCH NEXT FROM recreate_cursor INTO @p_fk_name, @p_parent_schema, @p_parent_table, @p_parent_cols, @p_ref_schema, @p_ref_table, @p_ref_cols, @p_del, @p_upd;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @p_ref_schema = N'dbo' AND @p_ref_table = N'GestionMantenimientos_Seguimientos' SET @p_ref_table = N'Seguimientos';
        DECLARE @sql_create nvarchar(max) = N'ALTER TABLE ' + QUOTENAME(@p_parent_schema) + N'.' + QUOTENAME(@p_parent_table) + N' ADD CONSTRAINT ' + QUOTENAME(@p_fk_name) + N' FOREIGN KEY(' + @p_parent_cols + N') REFERENCES ' + QUOTENAME(@p_ref_schema) + N'.' + QUOTENAME(@p_ref_table) + N'(' + @p_ref_cols + N')';
        IF @p_del = 1 SET @sql_create = @sql_create + N' ON DELETE CASCADE';
        ELSE IF @p_del = 2 SET @sql_create = @sql_create + N' ON DELETE SET NULL';
        ELSE IF @p_del = 3 SET @sql_create = @sql_create + N' ON DELETE SET DEFAULT';
        IF @p_upd = 1 SET @sql_create = @sql_create + N' ON UPDATE CASCADE';
        ELSE IF @p_upd = 2 SET @sql_create = @sql_create + N' ON UPDATE SET NULL';
        ELSE IF @p_upd = 3 SET @sql_create = @sql_create + N' ON UPDATE SET DEFAULT';
        EXEC sp_executesql @sql_create;
        FETCH NEXT FROM recreate_cursor INTO @p_fk_name, @p_parent_schema, @p_parent_table, @p_parent_cols, @p_ref_schema, @p_ref_table, @p_ref_cols, @p_del, @p_upd;
    END
    CLOSE recreate_cursor; DEALLOCATE recreate_cursor;

    DROP TABLE #FkToRecreate;
    COMMIT;
END
ELSE
BEGIN
    PRINT 'No action: either source does not exist or target already present.';
END");
        }
    }
}
