IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Missions] (
    [Id] int NOT NULL IDENTITY,
    [PlayerId] int NOT NULL,
    [PlayerName] nvarchar(100) NOT NULL,
    [Ship] int NOT NULL,
    [Status] int NOT NULL,
    [DurationType] int NOT NULL,
    [Level] int NOT NULL,
    [DurationSeconds] int NOT NULL,
    [Capacity] int NOT NULL,
    [QualityBump] real NOT NULL,
    [TargetArtifact] int NOT NULL,
    [SecondsRemaining] real NOT NULL,
    [LaunchTime] datetime2 NOT NULL,
    [ReturnTime] datetime2 NOT NULL,
    [FuelList] nvarchar(max) NULL,
    [IsStandby] bit NOT NULL,
    [Created] datetime2 NOT NULL,
    [Updated] datetime2 NOT NULL,
    CONSTRAINT [PK_Missions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Missions_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Missions_PlayerId] ON [Missions] ([PlayerId]);

CREATE INDEX [IX_Missions_PlayerName] ON [Missions] ([PlayerName]);

CREATE INDEX [IX_Missions_ReturnTime] ON [Missions] ([ReturnTime]);

CREATE INDEX [IX_Missions_Ship] ON [Missions] ([Ship]);

CREATE INDEX [IX_Missions_Status] ON [Missions] ([Status]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240504000000_AddMissionsTable', N'9.0.4');

CREATE TABLE [Players] (
    [PlayerId] nvarchar(450) NOT NULL,
    [PlayerName] nvarchar(max) NOT NULL,
    [Updated] datetime2 NOT NULL,
    [TotalCraftsThatCanBeLegendary] int NOT NULL,
    [ExpectedLegendaryCrafts] real NOT NULL,
    [ExpectedLegendaryDropsFromShips] real NOT NULL,
    [ExpectedLegendaries] real NOT NULL,
    [PlayerLegendaries] real NOT NULL,
    [PlayerLegendariesExcludingLunarTotem] real NOT NULL,
    [LLC] real NOT NULL,
    [ProphecyEggs] int NOT NULL,
    [SoulEggs] nvarchar(max) NOT NULL,
    [MER] real NOT NULL,
    [JER] real NOT NULL,
    [CraftingLevel] int NOT NULL,
    [PiggyConsumeValue] int NOT NULL,
    [ShipLaunchPoints] real NOT NULL,
    [HoarderScore] real NOT NULL,
    CONSTRAINT [PK_Players] PRIMARY KEY ([PlayerId])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240524025554_Initial', N'9.0.4');

ALTER TABLE [Players] DROP CONSTRAINT [PK_Players];

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Players]') AND [c].[name] = N'PlayerId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Players] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Players] DROP COLUMN [PlayerId];

ALTER TABLE [Players] ADD [Id] int NOT NULL IDENTITY;

ALTER TABLE [Players] ADD [EID] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Players] ADD CONSTRAINT [PK_Players] PRIMARY KEY ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240524032236_Id changes', N'9.0.4');

ALTER TABLE [Players] ADD [EarningsBonusPercentage] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Players] ADD [SoulEggsFull] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240525152834_Add Earnings Bonus', N'9.0.4');

ALTER TABLE [Players] ADD [Title] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240525161428_Add Title', N'9.0.4');

ALTER TABLE [Players] ADD [NextTitle] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Players] ADD [TitleProgress] float NOT NULL DEFAULT 0.0E0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240525190758_Add NextTitle and TitleProgress', N'9.0.4');

ALTER TABLE [Players] ADD [ProjectedTitleChange] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240527222157_Add ProjectedTitleChange', N'9.0.4');

ALTER TABLE [Players] ADD [EarningsBonusPerHour] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240528191123_Add EarningsBonusPerHour', N'9.0.4');

COMMIT;
GO

