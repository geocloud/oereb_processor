
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, and Azure
-- --------------------------------------------------
-- Date Created: 04/13/2012 17:45:59
-- Generated from EDMX file: C:\Users\db\Documents\enlistment\Geocentrale Apps\Geocentrale.Apps.Db\RuleEngine.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [RuleEngine];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_PointerRule_Pointer]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[PointerRule] DROP CONSTRAINT [FK_PointerRule_Pointer];
GO
IF OBJECT_ID(N'[dbo].[FK_PointerRule_Rule]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[PointerRule] DROP CONSTRAINT [FK_PointerRule_Rule];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[RuleSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[RuleSet];
GO
IF OBJECT_ID(N'[dbo].[PointerSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[PointerSet];
GO
IF OBJECT_ID(N'[dbo].[PointerRule]', 'U') IS NOT NULL
    DROP TABLE [dbo].[PointerRule];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Rules'
CREATE TABLE [dbo].[Rules] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [GAClassGuid] uniqueidentifier  NOT NULL,
    [Expression] nvarchar(max)  NOT NULL,
    [Name] nvarchar(max)  NULL,
    [Description] nvarchar(max)  NULL
);
GO

-- Creating table 'AssocationSubjects'
CREATE TABLE [dbo].[AssocationSubjects] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [GAClassGuid] uniqueidentifier  NOT NULL,
    [ObjectId] nvarchar(max)  NOT NULL,
    [Name] nvarchar(max)  NULL,
    [Description] nvarchar(max)  NULL
);
GO

-- Creating table 'PointerRule'
CREATE TABLE [dbo].[PointerRule] (
    [AssociationSubjects_Id] int  NOT NULL,
    [Rules_Id] int  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'Rules'
ALTER TABLE [dbo].[Rules]
ADD CONSTRAINT [PK_Rules]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'AssocationSubjects'
ALTER TABLE [dbo].[AssocationSubjects]
ADD CONSTRAINT [PK_AssocationSubjects]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [AssociationSubjects_Id], [Rules_Id] in table 'PointerRule'
ALTER TABLE [dbo].[PointerRule]
ADD CONSTRAINT [PK_PointerRule]
    PRIMARY KEY NONCLUSTERED ([AssociationSubjects_Id], [Rules_Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [AssociationSubjects_Id] in table 'PointerRule'
ALTER TABLE [dbo].[PointerRule]
ADD CONSTRAINT [FK_PointerRule_Pointer]
    FOREIGN KEY ([AssociationSubjects_Id])
    REFERENCES [dbo].[AssocationSubjects]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Rules_Id] in table 'PointerRule'
ALTER TABLE [dbo].[PointerRule]
ADD CONSTRAINT [FK_PointerRule_Rule]
    FOREIGN KEY ([Rules_Id])
    REFERENCES [dbo].[Rules]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_PointerRule_Rule'
CREATE INDEX [IX_FK_PointerRule_Rule]
ON [dbo].[PointerRule]
    ([Rules_Id]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------