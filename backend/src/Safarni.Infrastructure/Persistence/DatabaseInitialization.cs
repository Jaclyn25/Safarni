using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Safarni.Infrastructure.Persistence;

public static class DatabaseInitialization
{
    public static async Task InitializeAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
        await RepairLegacyTripSchemaAsync(dbContext);
    }

    private static Task<int> RepairLegacyTripSchemaAsync(ApplicationDbContext dbContext)
    {
        // A previously published migration was recorded as applied even though
        // it contained no DDL. Keep existing production databases self-healing
        // while the corrected schema is also represented in source control.
        return dbContext.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH(N'dbo.Trips', N'Status') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Trips]
                ADD [Status] int NOT NULL
                    CONSTRAINT [DF_Trips_Status] DEFAULT (0) WITH VALUES;
            END;

            IF COL_LENGTH(N'dbo.Trips', N'IsPublished') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Trips]
                ADD [IsPublished] bit NOT NULL
                    CONSTRAINT [DF_Trips_IsPublished] DEFAULT (0) WITH VALUES;
            END;

            IF COL_LENGTH(N'dbo.Trips', N'PublishedAt') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Trips]
                ADD [PublishedAt] datetime2 NULL;
            END;

            IF COL_LENGTH(N'dbo.Trips', N'Visibility') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Trips]
                ADD [Visibility] int NOT NULL
                    CONSTRAINT [DF_Trips_Visibility] DEFAULT (0) WITH VALUES;
            END;

            IF OBJECT_ID(N'dbo.PublicTripSnapshots', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[PublicTripSnapshots]
                (
                    [Id] uniqueidentifier NOT NULL
                        CONSTRAINT [PK_PublicTripSnapshots] PRIMARY KEY,
                    [TripId] uniqueidentifier NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [FK_PublicTripSnapshots_Trips_TripId]
                        FOREIGN KEY ([TripId]) REFERENCES [dbo].[Trips] ([Id])
                );

                CREATE UNIQUE INDEX [IX_PublicTripSnapshots_TripId]
                    ON [dbo].[PublicTripSnapshots] ([TripId]);
            END;

            IF OBJECT_ID(N'dbo.SnapshotVersions', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SnapshotVersions]
                (
                    [Id] uniqueidentifier NOT NULL
                        CONSTRAINT [PK_SnapshotVersions] PRIMARY KEY,
                    [SnapshotId] uniqueidentifier NOT NULL,
                    [VersionNumber] int NOT NULL,
                    [ItineraryJson] nvarchar(max) NOT NULL,
                    [CostShown] bit NOT NULL,
                    [CostAmount] decimal(18, 2) NULL,
                    [IncludedPhotoIdsJson] nvarchar(max) NOT NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [ViewsCount] int NOT NULL,
                    [LikesCount] int NOT NULL,
                    [SavesCount] int NOT NULL,
                    [CopiesCount] int NOT NULL,
                    [RatingsCount] int NOT NULL,
                    [AverageRating] float NOT NULL,
                    CONSTRAINT [FK_SnapshotVersions_PublicTripSnapshots_SnapshotId]
                        FOREIGN KEY ([SnapshotId])
                        REFERENCES [dbo].[PublicTripSnapshots] ([Id])
                        ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX [IX_SnapshotVersions_SnapshotId_VersionNumber]
                    ON [dbo].[SnapshotVersions] ([SnapshotId], [VersionNumber]);
            END;

            IF OBJECT_ID(N'dbo.TripInteractions', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[TripInteractions]
                (
                    [Id] uniqueidentifier NOT NULL
                        CONSTRAINT [PK_TripInteractions] PRIMARY KEY,
                    [UserId] uniqueidentifier NOT NULL,
                    [SnapshotVersionId] uniqueidentifier NOT NULL,
                    [Type] int NOT NULL,
                    [Reason] nvarchar(max) NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [FK_TripInteractions_SnapshotVersions_SnapshotVersionId]
                        FOREIGN KEY ([SnapshotVersionId])
                        REFERENCES [dbo].[SnapshotVersions] ([Id])
                        ON DELETE CASCADE,
                    CONSTRAINT [FK_TripInteractions_Users_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
                );

                CREATE INDEX [IX_TripInteractions_SnapshotVersionId]
                    ON [dbo].[TripInteractions] ([SnapshotVersionId]);

                CREATE UNIQUE INDEX [IX_TripInteractions_UserId_SnapshotVersionId_Type]
                    ON [dbo].[TripInteractions] ([UserId], [SnapshotVersionId], [Type]);
            END;

            IF OBJECT_ID(N'dbo.TripRatings', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[TripRatings]
                (
                    [Id] uniqueidentifier NOT NULL
                        CONSTRAINT [PK_TripRatings] PRIMARY KEY,
                    [UserId] uniqueidentifier NOT NULL,
                    [SnapshotVersionId] uniqueidentifier NOT NULL,
                    [Score] int NOT NULL,
                    [Comment] nvarchar(max) NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [FK_TripRatings_SnapshotVersions_SnapshotVersionId]
                        FOREIGN KEY ([SnapshotVersionId])
                        REFERENCES [dbo].[SnapshotVersions] ([Id])
                        ON DELETE CASCADE,
                    CONSTRAINT [FK_TripRatings_Users_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
                );

                CREATE INDEX [IX_TripRatings_SnapshotVersionId]
                    ON [dbo].[TripRatings] ([SnapshotVersionId]);

                CREATE UNIQUE INDEX [IX_TripRatings_UserId_SnapshotVersionId]
                    ON [dbo].[TripRatings] ([UserId], [SnapshotVersionId]);
            END;
            """);
    }
}
