using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenTripsAndItineraries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trips_UserId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Itineraries_TripId_IsActive",
                table: "Itineraries");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Trips",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "SelectedCitiesJson",
                table: "Trips",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Trips",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "OriginAirportCode",
                table: "Trips",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginAirportName",
                table: "Trips",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginCity",
                table: "Trips",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginCountryCode",
                table: "Trips",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Trips",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "FlightAirlines",
                table: "ItineraryItems",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlightArrivalAirportCode",
                table: "ItineraryItems",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlightArrivalAirportName",
                table: "ItineraryItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlightDepartureAirportCode",
                table: "ItineraryItems",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlightDepartureAirportName",
                table: "ItineraryItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FlightDurationMinutes",
                table: "ItineraryItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlightNumbers",
                table: "ItineraryItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FlightStopCount",
                table: "ItineraryItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ItineraryItems",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "StayNights",
                table: "ItineraryItems",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CurrencyCode",
                table: "Itineraries",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "BudgetAmount",
                table: "Itineraries",
                type: "decimal(12,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Itineraries",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "ItineraryGenerationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Preferences = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BudgetAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    TravelerCount = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ItineraryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItineraryGenerationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItineraryGenerationJobs_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripCities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Days = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    AirportCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: true),
                    AirportName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripCities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripCities_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [TripCities]
                    ([Id], [TripId], [Name], [Days], [OrderIndex], [AirportCode], [AirportName])
                SELECT
                    NEWID(),
                    t.[Id],
                    LTRIM(RTRIM(COALESCE(
                        JSON_VALUE(j.[value], '$.name'),
                        JSON_VALUE(j.[value], '$.Name')))),
                    TRY_CONVERT(int, COALESCE(
                        JSON_VALUE(j.[value], '$.days'),
                        JSON_VALUE(j.[value], '$.Days'))),
                    TRY_CONVERT(int, j.[key]),
                    UPPER(NULLIF(LTRIM(RTRIM(COALESCE(
                        JSON_VALUE(j.[value], '$.airportCode'),
                        JSON_VALUE(j.[value], '$.AirportCode')))), '')),
                    NULLIF(LTRIM(RTRIM(COALESCE(
                        JSON_VALUE(j.[value], '$.airportName'),
                        JSON_VALUE(j.[value], '$.AirportName')))), '')
                FROM [Trips] AS t
                CROSS APPLY OPENJSON(t.[SelectedCitiesJson]) AS j
                WHERE ISJSON(t.[SelectedCitiesJson]) = 1
                  AND COALESCE(
                        JSON_VALUE(j.[value], '$.name'),
                        JSON_VALUE(j.[value], '$.Name')) IS NOT NULL
                  AND TRY_CONVERT(int, COALESCE(
                        JSON_VALUE(j.[value], '$.days'),
                        JSON_VALUE(j.[value], '$.Days'))) > 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_UserId_CreatedAt",
                table: "Trips",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.Sql(
                """
                ;WITH RankedItineraries AS
                (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY [TripId]
                            ORDER BY [UpdatedAt] DESC, [CreatedAt] DESC, [Id] DESC
                        ) AS [RowNumber]
                    FROM [Itineraries]
                    WHERE [IsActive] = 1
                )
                UPDATE itinerary
                SET [IsActive] = 0
                FROM [Itineraries] AS itinerary
                INNER JOIN RankedItineraries AS ranked
                    ON ranked.[Id] = itinerary.[Id]
                WHERE ranked.[RowNumber] > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Itineraries_TripId",
                table: "Itineraries",
                column: "TripId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryGenerationJobs_TripId",
                table: "ItineraryGenerationJobs",
                column: "TripId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryGenerationJobs_TripId_CreatedAt",
                table: "ItineraryGenerationJobs",
                columns: new[] { "TripId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryGenerationJobs_UserId_IdempotencyKey",
                table: "ItineraryGenerationJobs",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TripCities_TripId_OrderIndex",
                table: "TripCities",
                columns: new[] { "TripId", "OrderIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItineraryGenerationJobs");

            migrationBuilder.DropTable(
                name: "TripCities");

            migrationBuilder.DropIndex(
                name: "IX_Trips_UserId_CreatedAt",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Itineraries_TripId",
                table: "Itineraries");

            migrationBuilder.DropColumn(
                name: "OriginAirportCode",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "OriginAirportName",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "OriginCity",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "OriginCountryCode",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "FlightAirlines",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "FlightArrivalAirportCode",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "FlightArrivalAirportName",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "FlightDepartureAirportCode",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "FlightDepartureAirportName",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "FlightDurationMinutes",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "FlightNumbers",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "FlightStopCount",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "StayNights",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Itineraries");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Trips",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "SelectedCitiesJson",
                table: "Trips",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Trips",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "CurrencyCode",
                table: "Itineraries",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldUnicode: false,
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "BudgetAmount",
                table: "Itineraries",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_UserId",
                table: "Trips",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Itineraries_TripId_IsActive",
                table: "Itineraries",
                columns: new[] { "TripId", "IsActive" });
        }
    }
}