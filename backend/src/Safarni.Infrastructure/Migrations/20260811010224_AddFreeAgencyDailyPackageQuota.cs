using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFreeAgencyDailyPackageQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "TravelPackages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelPackages_TravelAgencyId_PublishedAt",
                table: "TravelPackages",
                columns: new[] { "TravelAgencyId", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TravelPackages_TravelAgencyId_PublishedAt",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "TravelPackages");
        }
    }
}
