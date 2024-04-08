﻿IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240408185717_InitialCreate'
)
BEGIN
    CREATE TABLE [CensusConfig] (
        [Id] nvarchar(450) NOT NULL,
        [FacilityID] nvarchar(max) NOT NULL,
        [ScheduledTrigger] nvarchar(max) NOT NULL,
        [CreateDate] datetime2 NOT NULL,
        [ModifyDate] datetime2 NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [LastModifiedOn] datetime2 NULL,
        CONSTRAINT [PK_CensusConfig] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240408185717_InitialCreate'
)
BEGIN
    CREATE TABLE [CensusPatientList] (
        [Id] nvarchar(450) NOT NULL,
        [FacilityId] nvarchar(max) NOT NULL,
        [PatientId] nvarchar(max) NOT NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [AdmitDate] datetime2 NOT NULL,
        [IsDischarged] bit NOT NULL,
        [DischargeDate] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [LastModifiedOn] datetime2 NULL,
        CONSTRAINT [PK_CensusPatientList] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240408185717_InitialCreate'
)
BEGIN
    CREATE TABLE [PatientCensusHistory] (
        [Id] nvarchar(450) NOT NULL,
        [FacilityId] nvarchar(max) NOT NULL,
        [CensusDateTime] datetime2 NOT NULL,
        [ReportId] nvarchar(max) NOT NULL,
        [CreatedOn] datetime2 NOT NULL,
        [LastModifiedOn] datetime2 NULL,
        CONSTRAINT [PK_PatientCensusHistory] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240408185717_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240408185717_InitialCreate', N'8.0.3');
END;
GO

COMMIT;
GO

