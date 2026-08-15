using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicTripViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[PublicTripSnapshots]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PublicTripSnapshots]
                    (
                        [Id] uniqueidentifier NOT NULL,
                        [TripId] uniqueidentifier NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,

                        CONSTRAINT [PK_PublicTripSnapshots]
                            PRIMARY KEY ([Id]),

                        CONSTRAINT [FK_PublicTripSnapshots_Trips_TripId]
                            FOREIGN KEY ([TripId])
                            REFERENCES [dbo].[Trips] ([Id])
                            ON DELETE NO ACTION
                    );

                    CREATE UNIQUE INDEX [IX_PublicTripSnapshots_TripId]
                        ON [dbo].[PublicTripSnapshots] ([TripId]);
                END;


                IF OBJECT_ID(N'[dbo].[SnapshotVersions]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[SnapshotVersions]
                    (
                        [Id] uniqueidentifier NOT NULL,
                        [SnapshotId] uniqueidentifier NOT NULL,
                        [VersionNumber] int NOT NULL,
                        [ItineraryJson] nvarchar(max) NOT NULL,
                        [CostShown] bit NOT NULL,
                        [CostAmount] decimal(18,2) NULL,
                        [IncludedPhotoIdsJson] nvarchar(max) NOT NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [ViewsCount] int NOT NULL,
                        [LikesCount] int NOT NULL,
                        [SavesCount] int NOT NULL,
                        [CopiesCount] int NOT NULL,
                        [RatingsCount] int NOT NULL,
                        [AverageRating] float NOT NULL,

                        CONSTRAINT [PK_SnapshotVersions]
                            PRIMARY KEY ([Id]),

                        CONSTRAINT [FK_SnapshotVersions_PublicTripSnapshots_SnapshotId]
                            FOREIGN KEY ([SnapshotId])
                            REFERENCES [dbo].[PublicTripSnapshots] ([Id])
                            ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX [IX_SnapshotVersions_SnapshotId_VersionNumber]
                        ON [dbo].[SnapshotVersions]
                        ([SnapshotId], [VersionNumber]);
                END;


                IF OBJECT_ID(N'[dbo].[TripInteractions]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[TripInteractions]
                    (
                        [Id] uniqueidentifier NOT NULL,
                        [SnapshotVersionId] uniqueidentifier NOT NULL,
                        [UserId] uniqueidentifier NOT NULL,
                        [Type] int NOT NULL,
                        [Reason] nvarchar(max) NULL,
                        [CreatedAt] datetime2 NOT NULL,

                        CONSTRAINT [PK_TripInteractions]
                            PRIMARY KEY ([Id]),

                        CONSTRAINT [FK_TripInteractions_SnapshotVersions_SnapshotVersionId]
                            FOREIGN KEY ([SnapshotVersionId])
                            REFERENCES [dbo].[SnapshotVersions] ([Id])
                            ON DELETE CASCADE,

                        CONSTRAINT [FK_TripInteractions_Users_UserId]
                            FOREIGN KEY ([UserId])
                            REFERENCES [dbo].[Users] ([Id])
                            ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_TripInteractions_SnapshotVersionId]
                        ON [dbo].[TripInteractions] ([SnapshotVersionId]);

                    CREATE UNIQUE INDEX [IX_TripInteractions_UserId_SnapshotVersionId_Type]
                        ON [dbo].[TripInteractions]
                        ([UserId], [SnapshotVersionId], [Type]);
                END;


                IF OBJECT_ID(N'[dbo].[TripRatings]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[TripRatings]
                    (
                        [Id] uniqueidentifier NOT NULL,
                        [SnapshotVersionId] uniqueidentifier NOT NULL,
                        [UserId] uniqueidentifier NOT NULL,
                        [Score] int NOT NULL,
                        [Comment] nvarchar(max) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,

                        CONSTRAINT [PK_TripRatings]
                            PRIMARY KEY ([Id]),

                        CONSTRAINT [FK_TripRatings_SnapshotVersions_SnapshotVersionId]
                            FOREIGN KEY ([SnapshotVersionId])
                            REFERENCES [dbo].[SnapshotVersions] ([Id])
                            ON DELETE CASCADE,

                        CONSTRAINT [FK_TripRatings_Users_UserId]
                            FOREIGN KEY ([UserId])
                            REFERENCES [dbo].[Users] ([Id])
                            ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_TripRatings_SnapshotVersionId]
                        ON [dbo].[TripRatings] ([SnapshotVersionId]);

                    CREATE UNIQUE INDEX [IX_TripRatings_UserId_SnapshotVersionId]
                        ON [dbo].[TripRatings]
                        ([UserId], [SnapshotVersionId]);
                END;
                """
            );
            migrationBuilder.CreateTable(
                name: "PublicTripViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    SnapshotVersionId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    UserId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: true),

                    SessionId = table.Column<string>(
                        type: "nvarchar(450)",
                        nullable: true),

                    ViewedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_PublicTripViews",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_PublicTripViews_SnapshotVersions_SnapshotVersionId",
                        column: x => x.SnapshotVersionId,
                        principalTable: "SnapshotVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_PublicTripViews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicTripViews_SnapshotVersionId_UserId_SessionId_ViewedAt",
                table: "PublicTripViews",
                columns: new[]
                {
                    "SnapshotVersionId",
                    "UserId",
                    "SessionId",
                    "ViewedAt"
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicTripViews_UserId",
                table: "PublicTripViews",
                column: "UserId");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicTripViews");
        }
    }
}