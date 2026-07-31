using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Safarni.Infrastructure.Persistence;

#nullable disable

namespace Safarni.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260730090000_EnrichItineraryTravelData")]
public partial class EnrichItineraryTravelData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CostSource",
            table: "ItineraryItems",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExternalUrl",
            table: "ItineraryItems",
            type: "nvarchar(1500)",
            maxLength: 1500,
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "Latitude",
            table: "ItineraryItems",
            type: "float",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "Longitude",
            table: "ItineraryItems",
            type: "float",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PriceLevel",
            table: "ItineraryItems",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PriceSource",
            table: "ItineraryItems",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "RatePerNight",
            table: "ItineraryItems",
            type: "decimal(12,2)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "RouteDistanceMeters",
            table: "ItineraryItems",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "RouteDurationMinutes",
            table: "ItineraryItems",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RouteTransitDetails",
            table: "ItineraryItems",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RouteTravelMode",
            table: "ItineraryItems",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CostSource",
            table: "ItineraryItems");

        migrationBuilder.DropColumn(
            name: "ExternalUrl",
            table: "ItineraryItems");

        migrationBuilder.DropColumn(
            name: "Latitude",
            table: "ItineraryItems");

        migrationBuilder.DropColumn(
            name: "Longitude",
            table: "ItineraryItems");

        migrationBuilder.DropColumn(
            name: "PriceLevel",
            table: "ItineraryItems");

        migrationBuilder.DropColumn(
            name: "PriceSource",
            table: "ItineraryItems");

        migrationBuilder.DropColumn(
            name: "RatePerNight",
            table: "ItineraryItems");

        migrationBuilder.DropColumn(
            name: "RouteDistanceMeters",
            table: "ItineraryItems");

        migrationBuilder.DropColumn(
            name: "RouteDurationMinutes",
            table: "ItineraryItems");

        migrationBuilder.DropColumn(
            name: "RouteTransitDetails",
            table: "ItineraryItems");

        migrationBuilder.DropColumn(
            name: "RouteTravelMode",
            table: "ItineraryItems");
    }
}
