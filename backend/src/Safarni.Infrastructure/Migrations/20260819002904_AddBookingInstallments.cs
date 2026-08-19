using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinalPaymentDueAt",
                table: "PackageBookingOffers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InitialPaymentPercentage",
                table: "PackageBookingOffers",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 30m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                table: "BookingPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentNumber",
                table: "BookingPayments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayments_PackageBookingRequestId_InstallmentNumber",
                table: "BookingPayments",
                columns: new[] { "PackageBookingRequestId", "InstallmentNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingPayments_PackageBookingRequestId_InstallmentNumber",
                table: "BookingPayments");

            migrationBuilder.DropColumn(
                name: "FinalPaymentDueAt",
                table: "PackageBookingOffers");

            migrationBuilder.DropColumn(
                name: "InitialPaymentPercentage",
                table: "PackageBookingOffers");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "BookingPayments");

            migrationBuilder.DropColumn(
                name: "InstallmentNumber",
                table: "BookingPayments");
        }
    }
}
