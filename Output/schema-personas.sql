CREATE TABLE [Auditoria] (
    [IdAuditoria] uniqueidentifier NOT NULL,
    [EntidadAfectada] nvarchar(100) NOT NULL,
    [IdEntidad] uniqueidentifier NOT NULL,
    [Accion] nvarchar(50) NOT NULL,
    [UsuarioResponsable] nvarchar(100) NOT NULL,
    [FechaHora] datetime2 NOT NULL,
    [Detalle] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Auditoria] PRIMARY KEY ([IdAuditoria])
);
GO


CREATE TABLE [Cargos] (
    [IdCargo] uniqueidentifier NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Cargos] PRIMARY KEY ([IdCargo])
);
GO


CREATE TABLE [Cronogramas] (
    [Id] int NOT NULL IDENTITY,
    [Codigo] nvarchar(max) NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [Marca] nvarchar(max) NULL,
    [Sede] nvarchar(max) NULL,
    [FrecuenciaMtto] int NULL,
    [Semanas] nvarchar(max) NOT NULL,
    [Anio] int NOT NULL,
    CONSTRAINT [PK_Cronogramas] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Equipos] (
    [Id] int NOT NULL IDENTITY,
    [Codigo] nvarchar(max) NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [Marca] nvarchar(max) NULL,
    [Estado] int NOT NULL,
    [Sede] int NULL,
    [FechaRegistro] datetime2 NOT NULL,
    [Precio] decimal(18,2) NULL,
    [Observaciones] nvarchar(max) NULL,
    [FrecuenciaMtto] int NULL,
    [FechaBaja] datetime2 NULL,
    CONSTRAINT [PK_Equipos] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Seguimientos] (
    [Id] int NOT NULL IDENTITY,
    [Codigo] nvarchar(max) NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [TipoMtno] int NOT NULL,
    [Descripcion] nvarchar(200) NOT NULL,
    [Responsable] nvarchar(max) NOT NULL,
    [Costo] decimal(18,2) NULL,
    [Observaciones] nvarchar(max) NULL,
    [FechaRegistro] datetime2 NOT NULL,
    [Semana] int NOT NULL,
    [Anio] int NOT NULL,
    [Estado] int NOT NULL,
    [FechaRealizacion] datetime2 NULL,
    [Frecuencia] int NULL,
    CONSTRAINT [PK_Seguimientos] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [TipoDocumento] (
    [IdTipoDocumento] uniqueidentifier NOT NULL,
    [Nombre] nvarchar(50) NOT NULL,
    [Codigo] nvarchar(10) NOT NULL,
    [Descripcion] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_TipoDocumento] PRIMARY KEY ([IdTipoDocumento])
);
GO


CREATE TABLE [Usuarios] (
    [IdUsuario] uniqueidentifier NOT NULL,
    [PersonaId] uniqueidentifier NOT NULL,
    [NombreUsuario] nvarchar(max) NOT NULL,
    [HashContrasena] nvarchar(max) NOT NULL,
    [Salt] nvarchar(max) NOT NULL,
    [Activo] bit NOT NULL,
    [Desactivado] bit NOT NULL,
    [FechaUltimoAcceso] datetime2 NULL,
    [FechaCreacion] datetime2 NOT NULL,
    [FechaModificacion] datetime2 NOT NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([IdUsuario])
);
GO


CREATE TABLE [Personas] (
    [IdPersona] uniqueidentifier NOT NULL,
    [Nombres] nvarchar(max) NOT NULL,
    [Apellidos] nvarchar(max) NOT NULL,
    [TipoDocumentoId] uniqueidentifier NOT NULL,
    [NumeroDocumento] nvarchar(max) NOT NULL,
    [Correo] nvarchar(max) NOT NULL,
    [Telefono] nvarchar(max) NOT NULL,
    [CargoId] uniqueidentifier NOT NULL,
    [Activo] bit NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    [FechaModificacion] datetime2 NOT NULL,
    [Estado] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Personas] PRIMARY KEY ([IdPersona]),
    CONSTRAINT [FK_Personas_Cargos_CargoId] FOREIGN KEY ([CargoId]) REFERENCES [Cargos] ([IdCargo]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Personas_TipoDocumento_TipoDocumentoId] FOREIGN KEY ([TipoDocumentoId]) REFERENCES [TipoDocumento] ([IdTipoDocumento]) ON DELETE NO ACTION
);
GO


CREATE INDEX [IX_Personas_CargoId] ON [Personas] ([CargoId]);
GO


CREATE INDEX [IX_Personas_TipoDocumentoId] ON [Personas] ([TipoDocumentoId]);
GO


CREATE UNIQUE INDEX [IX_TipoDocumento_Codigo] ON [TipoDocumento] ([Codigo]);
GO


