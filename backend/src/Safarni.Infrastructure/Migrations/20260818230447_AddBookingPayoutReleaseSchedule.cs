using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPayoutReleaseSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReleaseAt",
                table: "BookingPayouts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayouts_Status_ReleaseAt",
                table: "BookingPayouts",
                columns: new[] { "Status", "ReleaseAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingPayouts_Status_ReleaseAt",
                table: "BookingPayouts");

            migrationBuilder.DropColumn(
                name: "ReleaseAt",
                table: "BookingPayouts");
        }
    }
}
