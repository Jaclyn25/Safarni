using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItineraryBudgetAndPlaceDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "ItineraryItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleMapsUrl",
                table: "ItineraryItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GooglePlaceId",
                table: "ItineraryItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "ItineraryItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "ItineraryItems",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetAmount",
                table: "Itineraries",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TravelerCount",
                table: "Itineraries",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "GoogleMapsUrl",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "GooglePlaceId",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "BudgetAmount",
                table: "Itineraries");

            migrationBuilder.DropColumn(
                name: "TravelerCount",
                table: "Itineraries");
        }
    }
}
