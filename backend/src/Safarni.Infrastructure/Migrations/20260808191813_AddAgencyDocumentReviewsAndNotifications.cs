using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgencyDocumentReviewsAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewReason",
                table: "AgencyVerificationDocuments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "AgencyVerificationDocuments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "AgencyVerificationDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "AgencyVerificationDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgencyVerificationDocuments_ReviewedByUserId",
                table: "AgencyVerificationDocuments",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_RecipientUserId_IsRead_CreatedAt",
                table: "UserNotifications",
                columns: new[] { "RecipientUserId", "IsRead", "CreatedAt" });

            // Preserve the trust state of agencies approved before individual
            // document decisions were introduced.
            migrationBuilder.Sql(
                """
                UPDATE documents
                SET documents.ReviewStatus = N'Approved',
                    documents.ReviewedAt = COALESCE(agencies.VerificationReviewedAt, documents.UploadedAt),
                    documents.ReviewedByUserId = agencies.VerificationReviewedByUserId
                FROM AgencyVerificationDocuments AS documents
                INNER JOIN TravelAgencies AS agencies
                    ON agencies.Id = documents.TravelAgencyId
                WHERE agencies.VerificationStatus = N'Verified';
                """);

            migrationBuilder.Sql(
                """
                UPDATE TravelAgencies
                SET VerificationDecisionReason = NULL
                WHERE VerificationStatus = N'Verified';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_AgencyVerificationDocuments_ReviewedByUserId",
                table: "AgencyVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "ReviewReason",
                table: "AgencyVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "AgencyVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "AgencyVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "AgencyVerificationDocuments");
        }
    }
}
