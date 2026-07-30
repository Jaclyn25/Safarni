using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateStructuredItineraries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Itinerary",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ItineraryGeneratedAt",
                table: "Trips");

            migrationBuilder.CreateTable(
                name: "Itineraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Preferences = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Itineraries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Itineraries_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItineraryDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItineraryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItineraryDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItineraryDays_Itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "Itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItineraryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItineraryDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItineraryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItineraryItems_ItineraryDays_ItineraryDayId",
                        column: x => x.ItineraryDayId,
                        principalTable: "ItineraryDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Itineraries_TripId_IsActive",
                table: "Itineraries",
                columns: new[] { "TripId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryDays_ItineraryId_DayNumber",
                table: "ItineraryDays",
                columns: new[] { "ItineraryId", "DayNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryItems_ItineraryDayId_OrderIndex",
                table: "ItineraryItems",
                columns: new[] { "ItineraryDayId", "OrderIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItineraryItems");

            migrationBuilder.DropTable(
                name: "ItineraryDays");

            migrationBuilder.DropTable(
                name: "Itineraries");

            migrationBuilder.AddColumn<string>(
                name: "Itinerary",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ItineraryGeneratedAt",
                table: "Trips",
                type: "datetime2",
                nullable: true);
        }
    }
}
