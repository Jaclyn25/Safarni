using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDev4PreferencesAndVisaApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PassportInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PassportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassportInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassportInfos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TravelPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreferredDestinationCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TravelPurpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TravelStyle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MinBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisaApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TravelPurpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlannedTravelDate = table.Column<DateOnly>(type: "date", nullable: false),
                    VisaType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisaApplications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PassportInfos_UserId",
                table: "PassportInfos",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelPreferences_UserId",
                table: "TravelPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_UserId",
                table: "VisaApplications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PassportInfos");

            migrationBuilder.DropTable(
                name: "TravelPreferences");

            migrationBuilder.DropTable(
                name: "VisaApplications");
        }
    }
}
