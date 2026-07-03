:setvar DatabaseName "AgendamientoMKT"

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    PRINT N'Creating database $(DatabaseName)...';
    DECLARE @CreateDatabaseSql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(N'$(DatabaseName)');
    EXEC sys.sp_executesql @CreateDatabaseSql;
END
ELSE
BEGIN
    PRINT N'Database $(DatabaseName) already exists.';
END;
GO

ALTER DATABASE [$(DatabaseName)] SET READ_COMMITTED_SNAPSHOT ON;
GO

