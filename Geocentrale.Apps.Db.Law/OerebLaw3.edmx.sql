
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, and Azure
-- --------------------------------------------------
-- Date Created: 12/17/2013 12:07:18
-- Generated from EDMX file: C:\team\Geocentrale Apps\Geocentrale Apps\Geocentrale.Apps.Server.Adapters\Law3\OerebLaw3.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [geocentrale.apps.oereb.law3];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_OerebDefinitionRechtsvorschrift_OerebDefinition]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[OerebDefinitionRechtsvorschrift] DROP CONSTRAINT [FK_OerebDefinitionRechtsvorschrift_OerebDefinition];
GO
IF OBJECT_ID(N'[dbo].[FK_OerebDefinitionRechtsvorschrift_Rechtsvorschrift]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[OerebDefinitionRechtsvorschrift] DROP CONSTRAINT [FK_OerebDefinitionRechtsvorschrift_Rechtsvorschrift];
GO
IF OBJECT_ID(N'[dbo].[FK_OerebDefinitionOerebKThema]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[OerebDefinitionSet] DROP CONSTRAINT [FK_OerebDefinitionOerebKThema];
GO
IF OBJECT_ID(N'[dbo].[FK_OerebDefinitionRechtsstatus]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[OerebDefinitionSet] DROP CONSTRAINT [FK_OerebDefinitionRechtsstatus];
GO
IF OBJECT_ID(N'[dbo].[FK_OerebDefinitionArtCode]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[OerebDefinitionSet] DROP CONSTRAINT [FK_OerebDefinitionArtCode];
GO
IF OBJECT_ID(N'[dbo].[FK_OerebDefinitionOerebDefinition]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[OerebDefinitionSet] DROP CONSTRAINT [FK_OerebDefinitionOerebDefinition];
GO
IF OBJECT_ID(N'[dbo].[FK_RechtsvorschriftRechtsstatus]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[RechtsnormSet] DROP CONSTRAINT [FK_RechtsvorschriftRechtsstatus];
GO
IF OBJECT_ID(N'[dbo].[FK_RechtsnormRechtsnormTyp]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[RechtsnormSet] DROP CONSTRAINT [FK_RechtsnormRechtsnormTyp];
GO
IF OBJECT_ID(N'[dbo].[FK_OerebDefinitionZustStelle]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[OerebDefinitionSet] DROP CONSTRAINT [FK_OerebDefinitionZustStelle];
GO
IF OBJECT_ID(N'[dbo].[FK_RechtsnormZustStelle]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[RechtsnormSet] DROP CONSTRAINT [FK_RechtsnormZustStelle];
GO
IF OBJECT_ID(N'[dbo].[FK_OerebDefinitionArtikel_OerebDefinition]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[OerebDefinitionArtikel] DROP CONSTRAINT [FK_OerebDefinitionArtikel_OerebDefinition];
GO
IF OBJECT_ID(N'[dbo].[FK_OerebDefinitionArtikel_Artikel]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[OerebDefinitionArtikel] DROP CONSTRAINT [FK_OerebDefinitionArtikel_Artikel];
GO
IF OBJECT_ID(N'[dbo].[FK_RechtsnormRechtsnorm]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[RechtsnormSet] DROP CONSTRAINT [FK_RechtsnormRechtsnorm];
GO
IF OBJECT_ID(N'[dbo].[FK_ArtikelRechtsnorm]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ArtikelSet] DROP CONSTRAINT [FK_ArtikelRechtsnorm];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[OerebDefinitionSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[OerebDefinitionSet];
GO
IF OBJECT_ID(N'[dbo].[ZustStelleSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ZustStelleSet];
GO
IF OBJECT_ID(N'[dbo].[RechtsstatusSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[RechtsstatusSet];
GO
IF OBJECT_ID(N'[dbo].[OerebKThemaSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[OerebKThemaSet];
GO
IF OBJECT_ID(N'[dbo].[ArtCodeSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ArtCodeSet];
GO
IF OBJECT_ID(N'[dbo].[ArtikelSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ArtikelSet];
GO
IF OBJECT_ID(N'[dbo].[RechtsnormSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[RechtsnormSet];
GO
IF OBJECT_ID(N'[dbo].[RechtsnormTypSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[RechtsnormTypSet];
GO
IF OBJECT_ID(N'[dbo].[OerebDefinitionRechtsvorschrift]', 'U') IS NOT NULL
    DROP TABLE [dbo].[OerebDefinitionRechtsvorschrift];
GO
IF OBJECT_ID(N'[dbo].[OerebDefinitionArtikel]', 'U') IS NOT NULL
    DROP TABLE [dbo].[OerebDefinitionArtikel];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'OerebDefinitionSet'
CREATE TABLE [dbo].[OerebDefinitionSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Aussage] nvarchar(max)  NOT NULL,
    [UmschreibungRaumbezug] nvarchar(max)  NULL,
    [LinkGeobasisdaten] nvarchar(max)  NULL,
    [ArtCodeliste] nvarchar(max)  NULL,
    [IsLive] bit  NULL,
    [VisibilityDate] datetime  NULL,
    [Source] nvarchar(max)  NULL,
    [OriginalId] nvarchar(max)  NULL,
    [OerebKThema_Id] int  NOT NULL,
    [Rechtsstatus_Id] int  NOT NULL,
    [ArtCode_Id] int  NULL,
    [Parent_Id] int  NULL,
    [ZustStelle_Id] int  NOT NULL
);
GO

-- Creating table 'ZustStelleSet'
CREATE TABLE [dbo].[ZustStelleSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [Url] nvarchar(max)  NULL,
    [Kanton] nvarchar(max)  NULL,
    [Gemeinde] int  NULL,
    [Abkuerzung] nvarchar(max)  NULL,
    [Source] nvarchar(max)  NULL,
    [OriginalId] nvarchar(max)  NULL
);
GO

-- Creating table 'RechtsstatusSet'
CREATE TABLE [dbo].[RechtsstatusSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Bezeichnung] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'OerebKThemaSet'
CREATE TABLE [dbo].[OerebKThemaSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [Beschreibung] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'ArtCodeSet'
CREATE TABLE [dbo].[ArtCodeSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Wert] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'ArtikelSet'
CREATE TABLE [dbo].[ArtikelSet] (
    [Nr] nvarchar(max)  NOT NULL,
    [Text] nvarchar(max)  NULL,
    [Id] int IDENTITY(1,1) NOT NULL,
    [IsLive] bit  NULL,
    [VisibilityDate] datetime  NULL,
    [Source] nvarchar(max)  NULL,
    [OriginalId] nvarchar(max)  NULL,
    [Rechtsnorm_Id] int  NOT NULL
);
GO

-- Creating table 'RechtsnormSet'
CREATE TABLE [dbo].[RechtsnormSet] (
    [LinkLexFindId] nvarchar(max)  NULL,
    [LinkLexFindKtSysNr] nvarchar(max)  NULL,
    [Titel] nvarchar(max)  NOT NULL,
    [OffiziellerTitel] nvarchar(max)  NULL,
    [Abkuerzung] nvarchar(max)  NULL,
    [OffizielleNr] nvarchar(max)  NULL,
    [Kanton] nvarchar(max)  NULL,
    [Gemeinde] int  NULL,
    [DokumentBinary] varbinary(max)  NULL,
    [Id] int IDENTITY(1,1) NOT NULL,
    [Url] nvarchar(max)  NULL,
    [PubliziertAb] datetime  NOT NULL,
    [IsLive] bit  NULL,
    [VisibilityDate] datetime  NULL,
    [ArtikelBezeichner] nvarchar(max)  NULL,
    [Source] nvarchar(max)  NULL,
    [OriginalId] nvarchar(max)  NULL,
    [Rechtsstatus_Id] int  NOT NULL,
    [RechtsnormTyp_Id] int  NOT NULL,
    [ZustStelle_Id] int  NOT NULL,
    [Parent_Id] int  NULL
);
GO

-- Creating table 'RechtsnormTypSet'
CREATE TABLE [dbo].[RechtsnormTypSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Bezeichnung] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'OerebDefinitionRechtsvorschrift'
CREATE TABLE [dbo].[OerebDefinitionRechtsvorschrift] (
    [OerebDefinition_Id] int  NOT NULL,
    [Rechtsnorm_Id] int  NOT NULL
);
GO

-- Creating table 'OerebDefinitionArtikel'
CREATE TABLE [dbo].[OerebDefinitionArtikel] (
    [OerebDefinition_Id] int  NOT NULL,
    [Artikel_Id] int  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'OerebDefinitionSet'
ALTER TABLE [dbo].[OerebDefinitionSet]
ADD CONSTRAINT [PK_OerebDefinitionSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'ZustStelleSet'
ALTER TABLE [dbo].[ZustStelleSet]
ADD CONSTRAINT [PK_ZustStelleSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'RechtsstatusSet'
ALTER TABLE [dbo].[RechtsstatusSet]
ADD CONSTRAINT [PK_RechtsstatusSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'OerebKThemaSet'
ALTER TABLE [dbo].[OerebKThemaSet]
ADD CONSTRAINT [PK_OerebKThemaSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'ArtCodeSet'
ALTER TABLE [dbo].[ArtCodeSet]
ADD CONSTRAINT [PK_ArtCodeSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'ArtikelSet'
ALTER TABLE [dbo].[ArtikelSet]
ADD CONSTRAINT [PK_ArtikelSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'RechtsnormSet'
ALTER TABLE [dbo].[RechtsnormSet]
ADD CONSTRAINT [PK_RechtsnormSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'RechtsnormTypSet'
ALTER TABLE [dbo].[RechtsnormTypSet]
ADD CONSTRAINT [PK_RechtsnormTypSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [OerebDefinition_Id], [Rechtsnorm_Id] in table 'OerebDefinitionRechtsvorschrift'
ALTER TABLE [dbo].[OerebDefinitionRechtsvorschrift]
ADD CONSTRAINT [PK_OerebDefinitionRechtsvorschrift]
    PRIMARY KEY NONCLUSTERED ([OerebDefinition_Id], [Rechtsnorm_Id] ASC);
GO

-- Creating primary key on [OerebDefinition_Id], [Artikel_Id] in table 'OerebDefinitionArtikel'
ALTER TABLE [dbo].[OerebDefinitionArtikel]
ADD CONSTRAINT [PK_OerebDefinitionArtikel]
    PRIMARY KEY NONCLUSTERED ([OerebDefinition_Id], [Artikel_Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [OerebDefinition_Id] in table 'OerebDefinitionRechtsvorschrift'
ALTER TABLE [dbo].[OerebDefinitionRechtsvorschrift]
ADD CONSTRAINT [FK_OerebDefinitionRechtsvorschrift_OerebDefinition]
    FOREIGN KEY ([OerebDefinition_Id])
    REFERENCES [dbo].[OerebDefinitionSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Rechtsnorm_Id] in table 'OerebDefinitionRechtsvorschrift'
ALTER TABLE [dbo].[OerebDefinitionRechtsvorschrift]
ADD CONSTRAINT [FK_OerebDefinitionRechtsvorschrift_Rechtsvorschrift]
    FOREIGN KEY ([Rechtsnorm_Id])
    REFERENCES [dbo].[RechtsnormSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_OerebDefinitionRechtsvorschrift_Rechtsvorschrift'
CREATE INDEX [IX_FK_OerebDefinitionRechtsvorschrift_Rechtsvorschrift]
ON [dbo].[OerebDefinitionRechtsvorschrift]
    ([Rechtsnorm_Id]);
GO

-- Creating foreign key on [OerebKThema_Id] in table 'OerebDefinitionSet'
ALTER TABLE [dbo].[OerebDefinitionSet]
ADD CONSTRAINT [FK_OerebDefinitionOerebKThema]
    FOREIGN KEY ([OerebKThema_Id])
    REFERENCES [dbo].[OerebKThemaSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_OerebDefinitionOerebKThema'
CREATE INDEX [IX_FK_OerebDefinitionOerebKThema]
ON [dbo].[OerebDefinitionSet]
    ([OerebKThema_Id]);
GO

-- Creating foreign key on [Rechtsstatus_Id] in table 'OerebDefinitionSet'
ALTER TABLE [dbo].[OerebDefinitionSet]
ADD CONSTRAINT [FK_OerebDefinitionRechtsstatus]
    FOREIGN KEY ([Rechtsstatus_Id])
    REFERENCES [dbo].[RechtsstatusSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_OerebDefinitionRechtsstatus'
CREATE INDEX [IX_FK_OerebDefinitionRechtsstatus]
ON [dbo].[OerebDefinitionSet]
    ([Rechtsstatus_Id]);
GO

-- Creating foreign key on [ArtCode_Id] in table 'OerebDefinitionSet'
ALTER TABLE [dbo].[OerebDefinitionSet]
ADD CONSTRAINT [FK_OerebDefinitionArtCode]
    FOREIGN KEY ([ArtCode_Id])
    REFERENCES [dbo].[ArtCodeSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_OerebDefinitionArtCode'
CREATE INDEX [IX_FK_OerebDefinitionArtCode]
ON [dbo].[OerebDefinitionSet]
    ([ArtCode_Id]);
GO

-- Creating foreign key on [Parent_Id] in table 'OerebDefinitionSet'
ALTER TABLE [dbo].[OerebDefinitionSet]
ADD CONSTRAINT [FK_OerebDefinitionOerebDefinition]
    FOREIGN KEY ([Parent_Id])
    REFERENCES [dbo].[OerebDefinitionSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_OerebDefinitionOerebDefinition'
CREATE INDEX [IX_FK_OerebDefinitionOerebDefinition]
ON [dbo].[OerebDefinitionSet]
    ([Parent_Id]);
GO

-- Creating foreign key on [Rechtsstatus_Id] in table 'RechtsnormSet'
ALTER TABLE [dbo].[RechtsnormSet]
ADD CONSTRAINT [FK_RechtsvorschriftRechtsstatus]
    FOREIGN KEY ([Rechtsstatus_Id])
    REFERENCES [dbo].[RechtsstatusSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_RechtsvorschriftRechtsstatus'
CREATE INDEX [IX_FK_RechtsvorschriftRechtsstatus]
ON [dbo].[RechtsnormSet]
    ([Rechtsstatus_Id]);
GO

-- Creating foreign key on [RechtsnormTyp_Id] in table 'RechtsnormSet'
ALTER TABLE [dbo].[RechtsnormSet]
ADD CONSTRAINT [FK_RechtsnormRechtsnormTyp]
    FOREIGN KEY ([RechtsnormTyp_Id])
    REFERENCES [dbo].[RechtsnormTypSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_RechtsnormRechtsnormTyp'
CREATE INDEX [IX_FK_RechtsnormRechtsnormTyp]
ON [dbo].[RechtsnormSet]
    ([RechtsnormTyp_Id]);
GO

-- Creating foreign key on [ZustStelle_Id] in table 'OerebDefinitionSet'
ALTER TABLE [dbo].[OerebDefinitionSet]
ADD CONSTRAINT [FK_OerebDefinitionZustStelle]
    FOREIGN KEY ([ZustStelle_Id])
    REFERENCES [dbo].[ZustStelleSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_OerebDefinitionZustStelle'
CREATE INDEX [IX_FK_OerebDefinitionZustStelle]
ON [dbo].[OerebDefinitionSet]
    ([ZustStelle_Id]);
GO

-- Creating foreign key on [ZustStelle_Id] in table 'RechtsnormSet'
ALTER TABLE [dbo].[RechtsnormSet]
ADD CONSTRAINT [FK_RechtsnormZustStelle]
    FOREIGN KEY ([ZustStelle_Id])
    REFERENCES [dbo].[ZustStelleSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_RechtsnormZustStelle'
CREATE INDEX [IX_FK_RechtsnormZustStelle]
ON [dbo].[RechtsnormSet]
    ([ZustStelle_Id]);
GO

-- Creating foreign key on [OerebDefinition_Id] in table 'OerebDefinitionArtikel'
ALTER TABLE [dbo].[OerebDefinitionArtikel]
ADD CONSTRAINT [FK_OerebDefinitionArtikel_OerebDefinition]
    FOREIGN KEY ([OerebDefinition_Id])
    REFERENCES [dbo].[OerebDefinitionSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Artikel_Id] in table 'OerebDefinitionArtikel'
ALTER TABLE [dbo].[OerebDefinitionArtikel]
ADD CONSTRAINT [FK_OerebDefinitionArtikel_Artikel]
    FOREIGN KEY ([Artikel_Id])
    REFERENCES [dbo].[ArtikelSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_OerebDefinitionArtikel_Artikel'
CREATE INDEX [IX_FK_OerebDefinitionArtikel_Artikel]
ON [dbo].[OerebDefinitionArtikel]
    ([Artikel_Id]);
GO

-- Creating foreign key on [Parent_Id] in table 'RechtsnormSet'
ALTER TABLE [dbo].[RechtsnormSet]
ADD CONSTRAINT [FK_RechtsnormRechtsnorm]
    FOREIGN KEY ([Parent_Id])
    REFERENCES [dbo].[RechtsnormSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_RechtsnormRechtsnorm'
CREATE INDEX [IX_FK_RechtsnormRechtsnorm]
ON [dbo].[RechtsnormSet]
    ([Parent_Id]);
GO

-- Creating foreign key on [Rechtsnorm_Id] in table 'ArtikelSet'
ALTER TABLE [dbo].[ArtikelSet]
ADD CONSTRAINT [FK_ArtikelRechtsnorm]
    FOREIGN KEY ([Rechtsnorm_Id])
    REFERENCES [dbo].[RechtsnormSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ArtikelRechtsnorm'
CREATE INDEX [IX_FK_ArtikelRechtsnorm]
ON [dbo].[ArtikelSet]
    ([Rechtsnorm_Id]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------