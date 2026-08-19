using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingSettlementFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AgencyServiceFeeAmount",
                table: "PackageBookingOffers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SafarniProtectionFeeAmount",
                table: "PackageBookingOffers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupplierCostAmount",
                table: "PackageBookingOffers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BookingSupplierEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBookingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBookingOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelAgencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConfirmationCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    StorageReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSupplierEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingSupplierEvidence_PackageBookingOffers_PackageBookingOfferId",
                        column: x => x.PackageBookingOfferId,
                        principalTable: "PackageBookingOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingSupplierEvidence_PackageBookingRequests_PackageBookingRequestId",
                        column: x => x.PackageBookingRequestId,
                        principalTable: "PackageBookingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingSupplierEvidence_TravelAgencies_TravelAgencyId",
                        column: x => x.TravelAgencyId,
                        principalTable: "TravelAgencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBookingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBookingOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelAgencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExternalTransferId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingPayouts_BookingSupplierEvidence_SupplierEvidenceId",
                        column: x => x.SupplierEvidenceId,
                        principalTable: "BookingSupplierEvidence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_BookingPayouts_PackageBookingOffers_PackageBookingOfferId",
                        column: x => x.PackageBookingOfferId,
                        principalTable: "PackageBookingOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingPayouts_PackageBookingRequests_PackageBookingRequestId",
                        column: x => x.PackageBookingRequestId,
                        principalTable: "PackageBookingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_BookingPayouts_TravelAgencies_TravelAgencyId",
                        column: x => x.TravelAgencyId,
                        principalTable: "TravelAgencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayouts_PackageBookingOfferId",
                table: "BookingPayouts",
                column: "PackageBookingOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayouts_PackageBookingRequestId",
                table: "BookingPayouts",
                column: "PackageBookingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayouts_Status_CreatedAt",
                table: "BookingPayouts",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayouts_SupplierEvidenceId",
                table: "BookingPayouts",
                column: "SupplierEvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayouts_TravelAgencyId",
                table: "BookingPayouts",
                column: "TravelAgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSupplierEvidence_PackageBookingOfferId",
                table: "BookingSupplierEvidence",
                column: "PackageBookingOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSupplierEvidence_PackageBookingRequestId",
                table: "BookingSupplierEvidence",
                column: "PackageBookingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSupplierEvidence_Status_CreatedAt",
                table: "BookingSupplierEvidence",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingSupplierEvidence_TravelAgencyId",
                table: "BookingSupplierEvidence",
                column: "TravelAgencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingPayouts");

            migrationBuilder.DropTable(
                name: "BookingSupplierEvidence");

            migrationBuilder.DropColumn(
                name: "AgencyServiceFeeAmount",
                table: "PackageBookingOffers");

            migrationBuilder.DropColumn(
                name: "SafarniProtectionFeeAmount",
                table: "PackageBookingOffers");

            migrationBuilder.DropColumn(
                name: "SupplierCostAmount",
                table: "PackageBookingOffers");
        }
    }
}
