
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, and Azure
-- --------------------------------------------------
-- Date Created: 09/18/2013 18:15:48
-- Generated from EDMX file: C:\team\Geocentrale Apps\Geocentrale Apps\Geocentrale.Apps.Server.Adapters\Law3\LexFind\LexFindCache.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [geocentrale.apps.oereb.lexfind];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_RechtsnormRechtsnormAusserKraft]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[RechtsnormAusserKraftSet] DROP CONSTRAINT [FK_RechtsnormRechtsnormAusserKraft];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[RechtsnormSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[RechtsnormSet];
GO
IF OBJECT_ID(N'[dbo].[RechtsnormAusserKraftSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[RechtsnormAusserKraftSet];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'RechtsnormSet'
CREATE TABLE [dbo].[RechtsnormSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Kanton] nvarchar(max)  NULL,
    [SysNr] nvarchar(max)  NULL,
    [Infkrafttreten] datetime  NULL,
    [LexFindId] int  NULL,
    [LexFindUrl] nvarchar(max)  NULL,
    [OrigUrl] nvarchar(max)  NULL,
    [InKraftSeit] datetime  NULL,
    [Abk] nvarchar(max)  NULL,
    [Titel] nvarchar(max)  NULL,
    [AusserKraft] bit  NULL,
    [IsUpdated] bit  NULL,
    [IsNew] bit  NULL,
    [SeenInNewestSync] bit  NULL
);
GO

-- Creating table 'RechtsnormAusserKraftSet'
CREATE TABLE [dbo].[RechtsnormAusserKraftSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [LexFindId] int  NULL,
    [LexFindUrl] nvarchar(max)  NULL,
    [InKraftVon] datetime  NULL,
    [InKraftBis] datetime  NULL,
    [FormlosBerichtigtAm] datetime  NULL,
    [RechtsnormId] int  NOT NULL,
    [IsNew] bit  NULL,
    [SeenInNewestSync] bit  NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'RechtsnormSet'
ALTER TABLE [dbo].[RechtsnormSet]
ADD CONSTRAINT [PK_RechtsnormSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'RechtsnormAusserKraftSet'
ALTER TABLE [dbo].[RechtsnormAusserKraftSet]
ADD CONSTRAINT [PK_RechtsnormAusserKraftSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [RechtsnormId] in table 'RechtsnormAusserKraftSet'
ALTER TABLE [dbo].[RechtsnormAusserKraftSet]
ADD CONSTRAINT [FK_RechtsnormRechtsnormAusserKraft]
    FOREIGN KEY ([RechtsnormId])
    REFERENCES [dbo].[RechtsnormSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_RechtsnormRechtsnormAusserKraft'
CREATE INDEX [IX_FK_RechtsnormRechtsnormAusserKraft]
ON [dbo].[RechtsnormAusserKraftSet]
    ([RechtsnormId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------