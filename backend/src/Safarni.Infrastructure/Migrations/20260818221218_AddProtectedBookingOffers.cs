using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProtectedBookingOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageBookingOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBookingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TravelerCount = table.Column<int>(type: "int", nullable: false),
                    Included = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Excluded = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CancellationPolicy = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AgencyNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupersededAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageBookingOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageBookingOffers_PackageBookingRequests_PackageBookingRequestId",
                        column: x => x.PackageBookingRequestId,
                        principalTable: "PackageBookingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageBookingOffers_PackageBookingRequestId_Status_ExpiresAt",
                table: "PackageBookingOffers",
                columns: new[] { "PackageBookingRequestId", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageBookingOffers_PackageBookingRequestId_Version",
                table: "PackageBookingOffers",
                columns: new[] { "PackageBookingRequestId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageBookingOffers");
        }
    }
}
