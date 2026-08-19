using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingTravelersAndDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageBookingTravelers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBookingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PassportNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PassportExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageBookingTravelers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageBookingTravelers_PackageBookingRequests_PackageBookingRequestId",
                        column: x => x.PackageBookingRequestId,
                        principalTable: "PackageBookingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageBookingDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBookingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBookingTravelerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StorageReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageBookingDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageBookingDocuments_PackageBookingRequests_PackageBookingRequestId",
                        column: x => x.PackageBookingRequestId,
                        principalTable: "PackageBookingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageBookingDocuments_PackageBookingTravelers_PackageBookingTravelerId",
                        column: x => x.PackageBookingTravelerId,
                        principalTable: "PackageBookingTravelers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageBookingDocuments_PackageBookingRequestId_CreatedAt",
                table: "PackageBookingDocuments",
                columns: new[] { "PackageBookingRequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageBookingDocuments_PackageBookingTravelerId",
                table: "PackageBookingDocuments",
                column: "PackageBookingTravelerId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageBookingTravelers_PackageBookingRequestId",
                table: "PackageBookingTravelers",
                column: "PackageBookingRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageBookingDocuments");

            migrationBuilder.DropTable(
                name: "PackageBookingTravelers");
        }
    }
}
