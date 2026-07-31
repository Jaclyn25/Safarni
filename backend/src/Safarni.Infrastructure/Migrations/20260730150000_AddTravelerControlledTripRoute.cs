using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Safarni.Infrastructure.Persistence;

#nullable disable

namespace Safarni.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260730150000_AddTravelerControlledTripRoute")]
public partial class AddTravelerControlledTripRoute
    : Migration
{
    protected override void Up(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OriginCountry",
            table: "Trips",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "SelectedCitiesJson",
            table: "Trips",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: false,
            defaultValue: "[]");
    }

    protected override void Down(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "OriginCountry",
            table: "Trips");

        migrationBuilder.DropColumn(
            name: "SelectedCitiesJson",
            table: "Trips");
    }
}
