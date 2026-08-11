using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyAgencyPackageBuilder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older drafts allowed zero-night route stops. The simplified builder treats
            // nights as the route-to-day allocation source, so retain those stops with the
            // smallest valid value instead of carrying invalid legacy data forward.
            migrationBuilder.Sql("UPDATE [TravelPackageCities] SET [Nights] = 1 WHERE [Nights] < 1;");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "TravelPackageItems");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "TravelPackageItems");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "TravelPackageItems");

            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                table: "TravelPackageItems");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "TravelPackageItems");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "TravelPackageItems");

            migrationBuilder.DropColumn(
                name: "Activities",
                table: "TravelPackageDays");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "TravelPackageCities");

            migrationBuilder.AddColumn<string>(
                name: "ProviderPhotoUrl",
                table: "TravelPackageItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderPhotoUrl",
                table: "TravelPackageItems");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "TravelPackageItems",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "TravelPackageItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "TravelPackageItems",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCost",
                table: "TravelPackageItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "TravelPackageItems",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "TravelPackageItems",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Activities",
                table: "TravelPackageDays",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "TravelPackageCities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
