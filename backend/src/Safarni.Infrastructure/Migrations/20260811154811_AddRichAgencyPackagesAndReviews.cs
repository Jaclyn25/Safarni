using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRichAgencyPackagesAndReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationPolicy",
                table: "TravelPackages",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Exclusions",
                table: "TravelPackages",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportantNotes",
                table: "TravelPackages",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Inclusions",
                table: "TravelPackages",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TravelPackageCityId",
                table: "TravelPackageDays",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdultCount",
                table: "PackageBookingRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AlternatePhone",
                table: "PackageBookingRequests",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChildCount",
                table: "PackageBookingRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ChildrenAgesJson",
                table: "PackageBookingRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "PackageBookingRequests",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Not provided");

            migrationBuilder.AddColumn<string>(
                name: "TravelerCity",
                table: "PackageBookingRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Not provided");

            migrationBuilder.AddColumn<string>(
                name: "TravelerCountry",
                table: "PackageBookingRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Not provided");

            migrationBuilder.Sql(
                "UPDATE PackageBookingRequests SET AdultCount = TravelerCount WHERE AdultCount = 0");

            migrationBuilder.CreateTable(
                name: "AgencyReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelAgencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageBookingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    AgencyResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgencyRespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgencyReviews_PackageBookingRequests_PackageBookingRequestId",
                        column: x => x.PackageBookingRequestId,
                        principalTable: "PackageBookingRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AgencyReviews_TravelAgencies_TravelAgencyId",
                        column: x => x.TravelAgencyId,
                        principalTable: "TravelAgencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgencyReviews_Users_TravelerUserId",
                        column: x => x.TravelerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TravelPackageCities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Nights = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelPackageCities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelPackageCities_TravelPackages_TravelPackageId",
                        column: x => x.TravelPackageId,
                        principalTable: "TravelPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TravelPackageItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelPackageDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    MapUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProviderPlaceId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Cuisine = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MealType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReservationRequired = table.Column<bool>(type: "bit", nullable: true),
                    AccommodationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StarRating = table.Column<int>(type: "int", nullable: true),
                    RoomType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    BoardBasis = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TransportMode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FromLocation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ToLocation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    MeetingPoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AdditionalDetails = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelPackageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelPackageItems_TravelPackageDays_TravelPackageDayId",
                        column: x => x.TravelPackageDayId,
                        principalTable: "TravelPackageDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TravelPackageImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelPackageDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TravelPackageItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelPackageImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelPackageImages_TravelPackageDays_TravelPackageDayId",
                        column: x => x.TravelPackageDayId,
                        principalTable: "TravelPackageDays",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TravelPackageImages_TravelPackageItems_TravelPackageItemId",
                        column: x => x.TravelPackageItemId,
                        principalTable: "TravelPackageItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TravelPackageImages_TravelPackages_TravelPackageId",
                        column: x => x.TravelPackageId,
                        principalTable: "TravelPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TravelPackageDays_TravelPackageCityId",
                table: "TravelPackageDays",
                column: "TravelPackageCityId");

            migrationBuilder.CreateIndex(
                name: "IX_AgencyReviews_PackageBookingRequestId",
                table: "AgencyReviews",
                column: "PackageBookingRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgencyReviews_TravelAgencyId_IsPublished_CreatedAt",
                table: "AgencyReviews",
                columns: new[] { "TravelAgencyId", "IsPublished", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgencyReviews_TravelerUserId",
                table: "AgencyReviews",
                column: "TravelerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPackageCities_TravelPackageId_OrderIndex",
                table: "TravelPackageCities",
                columns: new[] { "TravelPackageId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelPackageImages_TravelPackageDayId",
                table: "TravelPackageImages",
                column: "TravelPackageDayId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPackageImages_TravelPackageId_SortOrder",
                table: "TravelPackageImages",
                columns: new[] { "TravelPackageId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TravelPackageImages_TravelPackageItemId",
                table: "TravelPackageImages",
                column: "TravelPackageItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPackageItems_Provider_ProviderPlaceId",
                table: "TravelPackageItems",
                columns: new[] { "Provider", "ProviderPlaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_TravelPackageItems_TravelPackageDayId_OrderIndex",
                table: "TravelPackageItems",
                columns: new[] { "TravelPackageDayId", "OrderIndex" });

            migrationBuilder.AddForeignKey(
                name: "FK_TravelPackageDays_TravelPackageCities_TravelPackageCityId",
                table: "TravelPackageDays",
                column: "TravelPackageCityId",
                principalTable: "TravelPackageCities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TravelPackageDays_TravelPackageCities_TravelPackageCityId",
                table: "TravelPackageDays");

            migrationBuilder.DropTable(
                name: "AgencyReviews");

            migrationBuilder.DropTable(
                name: "TravelPackageCities");

            migrationBuilder.DropTable(
                name: "TravelPackageImages");

            migrationBuilder.DropTable(
                name: "TravelPackageItems");

            migrationBuilder.DropIndex(
                name: "IX_TravelPackageDays_TravelPackageCityId",
                table: "TravelPackageDays");

            migrationBuilder.DropColumn(
                name: "CancellationPolicy",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "Exclusions",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "ImportantNotes",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "Inclusions",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "TravelPackageCityId",
                table: "TravelPackageDays");

            migrationBuilder.DropColumn(
                name: "AdultCount",
                table: "PackageBookingRequests");

            migrationBuilder.DropColumn(
                name: "AlternatePhone",
                table: "PackageBookingRequests");

            migrationBuilder.DropColumn(
                name: "ChildCount",
                table: "PackageBookingRequests");

            migrationBuilder.DropColumn(
                name: "ChildrenAgesJson",
                table: "PackageBookingRequests");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "PackageBookingRequests");

            migrationBuilder.DropColumn(
                name: "TravelerCity",
                table: "PackageBookingRequests");

            migrationBuilder.DropColumn(
                name: "TravelerCountry",
                table: "PackageBookingRequests");
        }
    }
}
