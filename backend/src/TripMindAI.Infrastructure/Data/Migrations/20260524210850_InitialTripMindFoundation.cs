using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TripMindAI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialTripMindFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NationalityCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Egypt"),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "ar"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaseCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    TargetCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "EGP"),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RetrievedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AverageDailyBudgetUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BestSeason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisaAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryFrom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Egypt"),
                    CountryTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisaRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryFrom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Egypt"),
                    CountryTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VisaType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinPassportValidityMonths = table.Column<int>(type: "int", nullable: false),
                    MinBlankPassportPages = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OfficialSourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "New Travel Chat"),
                    Purpose = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VisaApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalityCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Egypt"),
                    DestinationCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VisaType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TravelStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TravelEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadinessScore = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisaApplications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PointsOfInterest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    ExternalPlaceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EstimatedCostUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsOfInterest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsOfInterest_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisaRuleDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisaRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaRuleDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisaRuleDocuments_VisaRules_VisaRuleId",
                        column: x => x.VisaRuleId,
                        principalTable: "VisaRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VisaApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DestinationCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DestinationCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BudgetUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BudgetEgp = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AiSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripPlans_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripPlans_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TripPlans_VisaApplications_VisaApplicationId",
                        column: x => x.VisaApplicationId,
                        principalTable: "VisaApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UploadedDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisaApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnalysisStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedDocuments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UploadedDocuments_VisaApplications_VisaApplicationId",
                        column: x => x.VisaApplicationId,
                        principalTable: "VisaApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VisaApplicationIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisaApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaApplicationIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisaApplicationIssues_VisaApplications_VisaApplicationId",
                        column: x => x.VisaApplicationId,
                        principalTable: "VisaApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedPlaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointOfInterestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPlaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedPlaces_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedPlaces_PointsOfInterest_PointOfInterestId",
                        column: x => x.PointOfInterestId,
                        principalTable: "PointsOfInterest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiToolCalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiToolCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiToolCalls_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlightSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginAirportCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DestinationAirportCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DepartureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Children = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightSearches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightSearches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlightSearches_TripPlans_TripPlanId",
                        column: x => x.TripPlanId,
                        principalTable: "TripPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VisaApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedReports_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeneratedReports_TripPlans_TripPlanId",
                        column: x => x.TripPlanId,
                        principalTable: "TripPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GeneratedReports_VisaApplications_VisaApplicationId",
                        column: x => x.VisaApplicationId,
                        principalTable: "VisaApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HotelSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DestinationCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Rooms = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelSearches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotelSearches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HotelSearches_TripPlans_TripPlanId",
                        column: x => x.TripPlanId,
                        principalTable: "TripPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TripCostItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountEgp = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripCostItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripCostItems_TripPlans_TripPlanId",
                        column: x => x.TripPlanId,
                        principalTable: "TripPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripDays_TripPlans_TripPlanId",
                        column: x => x.TripPlanId,
                        principalTable: "TripPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAnalysisResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Gemini"),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: false),
                    IsReadable = table.Column<bool>(type: "bit", nullable: false),
                    HasGlare = table.Column<bool>(type: "bit", nullable: false),
                    IsBlurred = table.Column<bool>(type: "bit", nullable: false),
                    IsCropped = table.Column<bool>(type: "bit", nullable: false),
                    ExtractedDataJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false, defaultValue: "{}"),
                    QualityIssuesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false, defaultValue: "[]"),
                    RawAiResponseJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAnalysisResults_UploadedDocuments_UploadedDocumentId",
                        column: x => x.UploadedDocumentId,
                        principalTable: "UploadedDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlightOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlightSearchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AirlineCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AirlineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FlightNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DepartureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginAirportCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DestinationAirportCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PriceUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceEgp = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StopsCount = table.Column<int>(type: "int", nullable: true),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightOffers_FlightSearches_FlightSearchId",
                        column: x => x.FlightSearchId,
                        principalTable: "FlightSearches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HotelOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelSearchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    PriceUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceEgp = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotelOffers_HotelSearches_HotelSearchId",
                        column: x => x.HotelSearchId,
                        principalTable: "HotelSearches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointOfInterestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimeSlot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    EstimatedCostUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EstimatedCostEgp = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripActivities_PointsOfInterest_PointOfInterestId",
                        column: x => x.PointOfInterestId,
                        principalTable: "PointsOfInterest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TripActivities_TripDays_TripDayId",
                        column: x => x.TripDayId,
                        principalTable: "TripDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripWeatherForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ForecastDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TemperatureCelsius = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WeatherDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RainProbability = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WindSpeed = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    RetrievedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripWeatherForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripWeatherForecasts_TripDays_TripDayId",
                        column: x => x.TripDayId,
                        principalTable: "TripDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Destinations",
                columns: new[] { "Id", "AverageDailyBudgetUsd", "BestSeason", "City", "Country", "Description", "Latitude", "Longitude" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333301"), 150m, "Spring (April-June) and Fall (September-October)", "Paris", "France", "The City of Light, known for its art, fashion, and culture.", 48.8566m, 2.3522m },
                    { new Guid("33333333-3333-3333-3333-333333333302"), 120m, "Spring (April-June) and Fall (September-October)", "Rome", "Italy", "The Eternal City, home to ancient ruins and Renaissance art.", 41.9028m, 12.4964m },
                    { new Guid("33333333-3333-3333-3333-333333333303"), 80m, "Spring (April-June) and Fall (September-November)", "Istanbul", "Turkey", "A transcontinental city straddling Europe and Asia.", 41.0082m, 28.9784m },
                    { new Guid("33333333-3333-3333-3333-333333333304"), 200m, "November to March", "Dubai", "UAE", "A futuristic metropolis with luxury shopping and landmarks.", 25.2048m, 55.2708m },
                    { new Guid("33333333-3333-3333-3333-333333333305"), 110m, "May to September", "Berlin", "Germany", "A city of history, culture, and vibrant nightlife.", 52.5200m, 13.4050m }
                });

            migrationBuilder.InsertData(
                table: "VisaAlerts",
                columns: new[] { "Id", "AlertType", "CountryFrom", "CountryTo", "IsActive", "Message", "PublishedAt", "Severity", "SourceUrl", "Title" },
                values: new object[] { new Guid("55555555-5555-5555-5555-555555555501"), "ProcessingDelay", "Egypt", "France", true, "Visa appointments may take longer during peak travel seasons. Always check the official embassy or visa center website.", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Warning", null, "Possible appointment delays" });

            migrationBuilder.InsertData(
                table: "VisaRules",
                columns: new[] { "Id", "CountryFrom", "CountryTo", "LastUpdated", "MinBlankPassportPages", "MinPassportValidityMonths", "Notes", "OfficialSourceUrl", "VisaType" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "Egypt", "France", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), 2, 6, "Schengen visa required. Biometric appointment needed.", "https://france-visas.gouv.fr", "Tourism" },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "Egypt", "Germany", new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), 2, 6, "Schengen visa required. Appointment booking via external service provider.", "https://www.auswaertiges-amt.de", "Tourism" },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "Egypt", "Italy", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 6, "Schengen visa required. Visa application center in Cairo.", "https://italy-visa.com", "Tourism" },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "Egypt", "Turkey", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 6, "E-visa available for Egyptian passport holders.", "https://www.evisa.gov.tr", "Tourism" },
                    { new Guid("11111111-1111-1111-1111-111111111105"), "Egypt", "UAE", new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), 2, 6, "Visa on arrival or pre-arranged visa via airline/hotel.", "https://u.ae", "Tourism" }
                });

            migrationBuilder.InsertData(
                table: "PointsOfInterest",
                columns: new[] { "Id", "Address", "Category", "CreatedAt", "DestinationId", "EstimatedCostUsd", "ExternalPlaceId", "Latitude", "Longitude", "Name", "Provider", "Rating" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444401"), "Champ de Mars, 5 Avenue Anatole France, 75007 Paris", "Landmark", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333301"), 25m, null, 48.8584m, 2.2945m, "Eiffel Tower", "Manual", 4.6m },
                    { new Guid("44444444-4444-4444-4444-444444444402"), "Rue de Rivoli, 75001 Paris", "Museum", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333301"), 17m, null, 48.8606m, 2.3376m, "Louvre Museum", "Manual", 4.7m },
                    { new Guid("44444444-4444-4444-4444-444444444403"), "172 Boulevard Saint-Germain, 75006 Paris", "Cafe", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333301"), 15m, null, 48.8540m, 2.3324m, "Café de Flore", "Manual", 4.3m },
                    { new Guid("44444444-4444-4444-4444-444444444404"), "Piazza del Colosseo, 1, 00184 Roma RM", "Landmark", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333302"), 16m, null, 41.8902m, 12.4922m, "Colosseum", "Manual", 4.7m },
                    { new Guid("44444444-4444-4444-4444-444444444405"), "Viale Vaticano, 00165 Roma RM", "Museum", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333302"), 20m, null, 41.9065m, 12.4536m, "Vatican Museums", "Manual", 4.6m },
                    { new Guid("44444444-4444-4444-4444-444444444406"), "Piazza di Trevi, 00187 Roma RM", "Landmark", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333302"), 0m, null, 41.9009m, 12.4833m, "Trevi Fountain", "Manual", 4.5m },
                    { new Guid("44444444-4444-4444-4444-444444444407"), "Sultan Ahmet Mahallesi, Ayasofya Meydanı, Fatih/İstanbul", "Landmark", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333303"), 10m, null, 41.0086m, 28.9802m, "Hagia Sophia", "Manual", 4.8m },
                    { new Guid("44444444-4444-4444-4444-444444444408"), "Beyazıt, 34126 Fatih/İstanbul", "Shopping", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333303"), 0m, null, 41.0107m, 28.9680m, "Grand Bazaar", "Manual", 4.3m },
                    { new Guid("44444444-4444-4444-4444-444444444409"), "Eminönü Pier, Fatih/İstanbul", "Attraction", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333303"), 15m, null, 41.0223m, 28.9739m, "Bosphorus Cruise", "Manual", 4.6m },
                    { new Guid("44444444-4444-4444-4444-444444444410"), "1 Sheikh Mohammed bin Rashid Blvd, Dubai", "Landmark", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333304"), 40m, null, 25.1972m, 55.2744m, "Burj Khalifa", "Manual", 4.6m },
                    { new Guid("44444444-4444-4444-4444-444444444411"), "Financial Center Rd, Dubai", "Shopping", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333304"), 0m, null, 25.1986m, 55.2796m, "Dubai Mall", "Manual", 4.5m },
                    { new Guid("44444444-4444-4444-4444-444444444412"), "Palm Jumeirah, Dubai", "Attraction", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333304"), 80m, null, 25.1307m, 55.1176m, "Atlantis Aquaventure", "Manual", 4.4m },
                    { new Guid("44444444-4444-4444-4444-444444444413"), "Pariser Platz, 10117 Berlin", "Landmark", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333305"), 0m, null, 52.5163m, 13.3777m, "Brandenburg Gate", "Manual", 4.6m },
                    { new Guid("44444444-4444-4444-4444-444444444414"), "Bernauer Str. 111, 13355 Berlin", "Museum", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333305"), 0m, null, 52.5350m, 13.3901m, "Berlin Wall Memorial", "Manual", 4.5m },
                    { new Guid("44444444-4444-4444-4444-444444444415"), "Platz der Republik 1, 11011 Berlin", "Landmark", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333305"), 0m, null, 52.5186m, 13.3761m, "Reichstag Building", "Manual", 4.5m }
                });

            migrationBuilder.InsertData(
                table: "VisaRuleDocuments",
                columns: new[] { "Id", "DocumentType", "IsRequired", "Notes", "VisaRuleId", "Weight" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222220001"), "Passport", true, "Must be valid for at least 6 months", new Guid("11111111-1111-1111-1111-111111111101"), 35 },
                    { new Guid("22222222-2222-2222-2222-222222220002"), "BankStatement", false, null, new Guid("11111111-1111-1111-1111-111111111101"), 20 },
                    { new Guid("22222222-2222-2222-2222-222222220003"), "TravelInsurance", false, null, new Guid("11111111-1111-1111-1111-111111111101"), 15 },
                    { new Guid("22222222-2222-2222-2222-222222220004"), "HotelBooking", false, null, new Guid("11111111-1111-1111-1111-111111111101"), 10 },
                    { new Guid("22222222-2222-2222-2222-222222220005"), "FlightReservation", false, null, new Guid("11111111-1111-1111-1111-111111111101"), 10 },
                    { new Guid("22222222-2222-2222-2222-222222220006"), "Passport", true, "Must be valid for at least 6 months", new Guid("11111111-1111-1111-1111-111111111102"), 35 },
                    { new Guid("22222222-2222-2222-2222-222222220007"), "BankStatement", false, null, new Guid("11111111-1111-1111-1111-111111111102"), 20 },
                    { new Guid("22222222-2222-2222-2222-222222220008"), "TravelInsurance", false, null, new Guid("11111111-1111-1111-1111-111111111102"), 15 },
                    { new Guid("22222222-2222-2222-2222-222222220009"), "HotelBooking", false, null, new Guid("11111111-1111-1111-1111-111111111102"), 10 },
                    { new Guid("22222222-2222-2222-2222-222222220010"), "FlightReservation", false, null, new Guid("11111111-1111-1111-1111-111111111102"), 10 },
                    { new Guid("22222222-2222-2222-2222-222222220011"), "Passport", true, "Must be valid for at least 6 months", new Guid("11111111-1111-1111-1111-111111111103"), 35 },
                    { new Guid("22222222-2222-2222-2222-222222220012"), "BankStatement", false, null, new Guid("11111111-1111-1111-1111-111111111103"), 20 },
                    { new Guid("22222222-2222-2222-2222-222222220013"), "TravelInsurance", false, null, new Guid("11111111-1111-1111-1111-111111111103"), 15 },
                    { new Guid("22222222-2222-2222-2222-222222220014"), "HotelBooking", false, null, new Guid("11111111-1111-1111-1111-111111111103"), 10 },
                    { new Guid("22222222-2222-2222-2222-222222220015"), "FlightReservation", false, null, new Guid("11111111-1111-1111-1111-111111111103"), 10 },
                    { new Guid("22222222-2222-2222-2222-222222220016"), "Passport", true, "Must be valid for at least 6 months", new Guid("11111111-1111-1111-1111-111111111104"), 35 },
                    { new Guid("22222222-2222-2222-2222-222222220017"), "BankStatement", false, null, new Guid("11111111-1111-1111-1111-111111111104"), 20 },
                    { new Guid("22222222-2222-2222-2222-222222220018"), "TravelInsurance", false, null, new Guid("11111111-1111-1111-1111-111111111104"), 15 },
                    { new Guid("22222222-2222-2222-2222-222222220019"), "HotelBooking", false, null, new Guid("11111111-1111-1111-1111-111111111104"), 10 },
                    { new Guid("22222222-2222-2222-2222-222222220020"), "FlightReservation", false, null, new Guid("11111111-1111-1111-1111-111111111104"), 10 },
                    { new Guid("22222222-2222-2222-2222-222222220021"), "Passport", true, "Must be valid for at least 6 months", new Guid("11111111-1111-1111-1111-111111111105"), 35 },
                    { new Guid("22222222-2222-2222-2222-222222220022"), "BankStatement", false, null, new Guid("11111111-1111-1111-1111-111111111105"), 20 },
                    { new Guid("22222222-2222-2222-2222-222222220023"), "TravelInsurance", false, null, new Guid("11111111-1111-1111-1111-111111111105"), 15 },
                    { new Guid("22222222-2222-2222-2222-222222220024"), "HotelBooking", false, null, new Guid("11111111-1111-1111-1111-111111111105"), 10 },
                    { new Guid("22222222-2222-2222-2222-222222220025"), "FlightReservation", false, null, new Guid("11111111-1111-1111-1111-111111111105"), 10 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiToolCalls_MessageId",
                table: "AiToolCalls",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId",
                table: "Conversations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAnalysisResults_UploadedDocumentId",
                table: "DocumentAnalysisResults",
                column: "UploadedDocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightOffers_FlightSearchId",
                table: "FlightOffers",
                column: "FlightSearchId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightSearches_TripPlanId",
                table: "FlightSearches",
                column: "TripPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightSearches_UserId",
                table: "FlightSearches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_TripPlanId",
                table: "GeneratedReports",
                column: "TripPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_UserId",
                table: "GeneratedReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_VisaApplicationId",
                table: "GeneratedReports",
                column: "VisaApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelOffers_HotelSearchId",
                table: "HotelOffers",
                column: "HotelSearchId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelSearches_TripPlanId",
                table: "HotelSearches",
                column: "TripPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelSearches_UserId",
                table: "HotelSearches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsOfInterest_DestinationId",
                table: "PointsOfInterest",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPlaces_PointOfInterestId",
                table: "SavedPlaces",
                column: "PointOfInterestId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPlaces_UserId",
                table: "SavedPlaces",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TripActivities_PointOfInterestId",
                table: "TripActivities",
                column: "PointOfInterestId");

            migrationBuilder.CreateIndex(
                name: "IX_TripActivities_TripDayId",
                table: "TripActivities",
                column: "TripDayId");

            migrationBuilder.CreateIndex(
                name: "IX_TripCostItems_TripPlanId",
                table: "TripCostItems",
                column: "TripPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TripDays_TripPlanId",
                table: "TripDays",
                column: "TripPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TripPlans_ConversationId",
                table: "TripPlans",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_TripPlans_UserId",
                table: "TripPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TripPlans_VisaApplicationId",
                table: "TripPlans",
                column: "VisaApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_TripWeatherForecasts_TripDayId",
                table: "TripWeatherForecasts",
                column: "TripDayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedDocuments_UserId",
                table: "UploadedDocuments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedDocuments_VisaApplicationId",
                table: "UploadedDocuments",
                column: "VisaApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplicationIssues_VisaApplicationId",
                table: "VisaApplicationIssues",
                column: "VisaApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_UserId",
                table: "VisaApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisaRuleDocuments_VisaRuleId",
                table: "VisaRuleDocuments",
                column: "VisaRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiToolCalls");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CurrencyRates");

            migrationBuilder.DropTable(
                name: "DocumentAnalysisResults");

            migrationBuilder.DropTable(
                name: "FlightOffers");

            migrationBuilder.DropTable(
                name: "GeneratedReports");

            migrationBuilder.DropTable(
                name: "HotelOffers");

            migrationBuilder.DropTable(
                name: "SavedPlaces");

            migrationBuilder.DropTable(
                name: "TripActivities");

            migrationBuilder.DropTable(
                name: "TripCostItems");

            migrationBuilder.DropTable(
                name: "TripWeatherForecasts");

            migrationBuilder.DropTable(
                name: "VisaAlerts");

            migrationBuilder.DropTable(
                name: "VisaApplicationIssues");

            migrationBuilder.DropTable(
                name: "VisaRuleDocuments");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "UploadedDocuments");

            migrationBuilder.DropTable(
                name: "FlightSearches");

            migrationBuilder.DropTable(
                name: "HotelSearches");

            migrationBuilder.DropTable(
                name: "PointsOfInterest");

            migrationBuilder.DropTable(
                name: "TripDays");

            migrationBuilder.DropTable(
                name: "VisaRules");

            migrationBuilder.DropTable(
                name: "Destinations");

            migrationBuilder.DropTable(
                name: "TripPlans");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "VisaApplications");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
