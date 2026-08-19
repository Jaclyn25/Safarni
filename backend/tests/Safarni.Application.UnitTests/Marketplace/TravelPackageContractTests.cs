using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Safarni.Application.Features.Packages;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Application.UnitTests.Marketplace;

public sealed class TravelPackageContractTests
{
    [Fact]
    public void PackageModel_ConfiguresAgencyOwnershipAndPublicFields()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SafarniModelTests;Trusted_Connection=True;")
            .Options;
        using var context = new ApplicationDbContext(options);

        var package = context.Model.FindEntityType(typeof(TravelPackage));
        var day = context.Model.FindEntityType(typeof(TravelPackageDay));
        var booking = context.Model.FindEntityType(typeof(PackageBookingRequest));

        package.Should().NotBeNull();
        package!.GetTableName().Should().Be("TravelPackages");
        package.FindProperty(nameof(TravelPackage.Price))!.GetPrecision().Should().Be(18);
        package.FindProperty(nameof(TravelPackage.Price))!.GetScale().Should().Be(2);
        package.GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(TravelAgency))
            .DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        day.Should().NotBeNull();
        day!.GetIndexes().Single(index => index.Properties.Select(property => property.Name)
            .SequenceEqual([nameof(TravelPackageDay.TravelPackageId), nameof(TravelPackageDay.DayNumber)])
        ).IsUnique.Should().BeTrue();

        booking.Should().NotBeNull();
        booking!.GetTableName().Should().Be("PackageBookingRequests");
        booking.FindProperty(nameof(PackageBookingRequest.Status))!
            .GetMaxLength().Should().Be(20);
    }

    [Fact]
    public void PackageBookingStatuses_ExposeRequiredContractValues()
    {
        Enum.GetNames<PackageBookingRequestStatus>().Should().Equal(
            "Pending",
            "Accepted",
            "Rejected",
            "Cancelled",
            "OfferSent",
            "AwaitingPayment",
            "Confirmed",
            "InProgress",
            "Completed",
            "Expired",
            "CancelledByTraveler",
            "CancelledByAgency",
            "Refunded",
            "Disputed");
    }

    [Fact]
    public void PackageValidator_RejectsDurationThatDoesNotMatchDates()
    {
        var command = new CreateTravelPackageCommand(
            Guid.NewGuid(), "Cairo", "A package", "Egypt",
            new DateTime(2026, 8, 10), new DateTime(2026, 8, 12),
            2, 100, "USD", 10, null, null, null);

        var result = new CreateTravelPackageCommandValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(value => value.DurationDays)
            .WithErrorMessage("Duration must match the inclusive package date range.");
    }

    [Fact]
    public void PackageValidator_AcceptsAValidPackageContract()
    {
        var command = new CreateTravelPackageCommand(
            Guid.NewGuid(), "Cairo Escape", "A package", "Egypt",
            new DateTime(2026, 8, 10), new DateTime(2026, 8, 12),
            3, 100, "usd", 10, null, 15, "Summer offer");

        var result = new CreateTravelPackageCommandValidator().TestValidate(command);

        result.IsValid.Should().BeTrue();
    }
}
