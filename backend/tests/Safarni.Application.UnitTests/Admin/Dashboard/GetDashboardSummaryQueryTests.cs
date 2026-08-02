using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Features.Admin.Common;
using Safarni.Application.Features.Admin.Dashboard;

namespace Safarni.Application.UnitTests.Admin.Dashboard;

public class GetDashboardSummaryQueryTests
{
    [Fact]
    public async Task Handle_ShouldMapAllSummaryFields()
    {
        var adminRepository = new Mock<IAdminRepository>();
        adminRepository
            .Setup(x => x.GetDashboardSummaryAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSummaryData(
                TotalUsers: 10,
                ActiveUsers: 7,
                VerifiedUsers: 5,
                TotalTrips: 20,
                TotalVisaApplications: 4,
                TotalConversations: 30,
                TotalAiMessages: 120,
                ItineraryJobsTotal: 8,
                ItineraryJobsPending: 2,
                ItineraryJobsFailed: 1,
                RecentAiFailuresLast24Hours: 3,
                QdrantHealthy: true,
                DegradedServiceCount: 1));

        var handler = new GetDashboardSummaryQueryHandler(
            adminRepository.Object);

        var result = await handler.Handle(
            new GetDashboardSummaryQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var summary = result.Value!;
        summary.TotalUsers.Should().Be(10);
        summary.ActiveUsers.Should().Be(7);
        summary.VerifiedUsers.Should().Be(5);
        summary.TotalTrips.Should().Be(20);
        summary.TotalVisaApplications.Should().Be(4);
        summary.TotalConversations.Should().Be(30);
        summary.TotalAiMessages.Should().Be(120);
        summary.ItineraryJobsTotal.Should().Be(8);
        summary.ItineraryJobsPending.Should().Be(2);
        summary.ItineraryJobsFailed.Should().Be(1);
        summary.RecentAiFailuresLast24Hours.Should().Be(3);
        summary.QdrantHealthy.Should().BeTrue();
        summary.DegradedServiceCount.Should().Be(1);
    }
}
