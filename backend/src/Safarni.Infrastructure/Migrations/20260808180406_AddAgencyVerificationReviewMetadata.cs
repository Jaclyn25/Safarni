using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgencyVerificationReviewMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationDecisionReason",
                table: "TravelAgencies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationReviewedAt",
                table: "TravelAgencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerificationReviewedByUserId",
                table: "TravelAgencies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelAgencies_VerificationReviewedByUserId",
                table: "TravelAgencies",
                column: "VerificationReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TravelAgencies_VerificationReviewedByUserId",
                table: "TravelAgencies");

            migrationBuilder.DropColumn(
                name: "VerificationDecisionReason",
                table: "TravelAgencies");

            migrationBuilder.DropColumn(
                name: "VerificationReviewedAt",
                table: "TravelAgencies");

            migrationBuilder.DropColumn(
                name: "VerificationReviewedByUserId",
                table: "TravelAgencies");
        }
    }
}
