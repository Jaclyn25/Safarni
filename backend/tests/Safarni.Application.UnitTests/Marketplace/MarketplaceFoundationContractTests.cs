using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Application.UnitTests.Marketplace;

public sealed class MarketplaceFoundationContractTests
{
    [Fact]
    public void SharedEnums_ExposeStableAgencyContractValues()
    {
        Enum.GetNames<AgencyVerificationStatus>().Should().Equal(
            "Pending",
            "ChangesRequested",
            "Verified",
            "Rejected",
            "Suspended");
        Enum.GetNames<AgencyVerificationDocumentType>().Should().Equal(
            "CommercialRegistration",
            "TaxCard",
            "TourismCompanyLicense");
        UserRole.TravelAgency.Should().HaveValue(5);
    }

    [Fact]
    public void MarketplaceFoundationModel_ConfiguresOwnershipAndPrivacyBoundaries()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=SafarniModelTests;Trusted_Connection=True;")
            .Options;
        using var context = new ApplicationDbContext(options);

        var agency = context.Model.FindEntityType(typeof(TravelAgency));
        var document = context.Model.FindEntityType(
            typeof(AgencyVerificationDocument));

        agency.Should().NotBeNull();
        agency!.GetTableName().Should().Be("TravelAgencies");
        agency.FindProperty(nameof(TravelAgency.VerificationStatus))!
            .GetMaxLength()
            .Should()
            .Be(30);

        var userRelationship = agency.GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(User));
        userRelationship.IsUnique.Should().BeTrue();
        userRelationship.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        agency.GetIndexes()
            .Single(index => index.Properties
                .Select(property => property.Name)
                .SequenceEqual([nameof(TravelAgency.UserId)]))
            .IsUnique
            .Should()
            .BeTrue();

        document.Should().NotBeNull();
        document!.GetTableName().Should().Be(
            "AgencyVerificationDocuments");
        var agencyRelationship = document.GetForeignKeys()
            .Single(key =>
                key.PrincipalEntityType.ClrType == typeof(TravelAgency));
        agencyRelationship.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        document.GetIndexes()
            .Single(index => index.Properties
                .Select(property => property.Name)
                .SequenceEqual([
                    nameof(AgencyVerificationDocument.TravelAgencyId),
                    nameof(AgencyVerificationDocument.DocumentType)
                ]))
            .IsUnique
            .Should()
            .BeTrue();
    }
}
