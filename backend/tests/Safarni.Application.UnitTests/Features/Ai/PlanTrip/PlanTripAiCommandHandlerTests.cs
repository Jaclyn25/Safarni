using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Features.Chat.Services;
using ChatBotApi.Domain.Entities;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Safarni.Application.Common.Configuration;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Ai.PlanTrip;
using Safarni.Application.Features.PublicTrips.Queries;
using Xunit;

namespace Safarni.Application.UnitTests.Features.Ai.PlanTrip;

public class PlanTripAiCommandHandlerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<IAIProvider> _aiProviderMock;
    private readonly Mock<IIntentDetectionService> _intentDetectionServiceMock;
    private readonly Mock<IPromptBuilder> _promptBuilderMock;
    private readonly Mock<ILogger<PlanTripAiCommandHandler>> _loggerMock;
    private readonly AiTripPlanOptions _options;
    private readonly PlanTripAiCommandHandler _handler;

    public PlanTripAiCommandHandlerTests()
    {
        _senderMock = new Mock<ISender>();
        _aiProviderMock = new Mock<IAIProvider>();
        _intentDetectionServiceMock = new Mock<IIntentDetectionService>();
        _promptBuilderMock = new Mock<IPromptBuilder>();
        _loggerMock = new Mock<ILogger<PlanTripAiCommandHandler>>();
        _options = new AiTripPlanOptions { TimeoutSeconds = 2 };

        _handler = new PlanTripAiCommandHandler(
            _senderMock.Object,
            _aiProviderMock.Object,
            _intentDetectionServiceMock.Object,
            _promptBuilderMock.Object,
            _loggerMock.Object,
            Options.Create(_options));
    }

    [Fact]
    public async Task Handle_WithPublicTrips_CreatesGroundingContextAndSucceeds() // Test A
    {
        // Arrange
        var prompt = "France";
        var publicTrips = new List<PublicTripSearchResponse>
        {
            new PublicTripSearchResponse(Guid.NewGuid(), Guid.NewGuid(), "Trip 1", "France", DateTime.Now, DateTime.Now.AddDays(5), 5, 100, "Author", "", 0, 0, 5, DateTime.Now, false, ItineraryJson: "{\"days\":[{\"items\":[{\"title\":\"Eiffel Tower\"}]}]}")
        };

        _senderMock.Setup(s => s.Send(It.IsAny<SearchPublicTripsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyCollection<PublicTripSearchResponse>>.Success(publicTrips));

        _promptBuilderMock.Setup(p => p.BuildTripPlanSystemPrompt(prompt, It.Is<string>(ctx => ctx.Contains("Eiffel Tower"))))
            .Returns("System Prompt With Context");

        _aiProviderMock.Setup(a => a.GetChatCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("- Eiffel Tower\n- Louvre Museum");

        // Act
        var result = await _handler.Handle(new PlanTripAiCommand(prompt), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.GeneratedItinerary.Should().Contain("*(✓ Verified by community)*"); // Verified
        result.Value!.GeneratedItinerary.Should().Contain("Louvre Museum"); // Not verified
    }

    [Fact]
    public async Task Handle_WithNoPublicTrips_ExecutesWithoutGroundingContext() // Test B
    {
        // Arrange
        var prompt = "Unknown Destination";
        var publicTrips = new List<PublicTripSearchResponse>(); // Empty

        _senderMock.Setup(s => s.Send(It.IsAny<SearchPublicTripsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyCollection<PublicTripSearchResponse>>.Success(publicTrips));

        _promptBuilderMock.Setup(p => p.BuildTripPlanSystemPrompt(prompt, string.Empty))
            .Returns("System Prompt General Knowledge");

        _aiProviderMock.Setup(a => a.GetChatCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("- Random Place");

        // Act
        var result = await _handler.Handle(new PlanTripAiCommand(prompt), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.GeneratedItinerary.Should().NotContain("Verified by community");
    }

    [Theory]
    // 1. Exact or partial specific matches
    [InlineData("Hagia Sophia Grand Mosque", "- Hagia Sophia", "- Hagia Sophia *(✓ Verified by community)*")] 
    [InlineData("The Blue Mosque", "- Blue Mosque", "- Blue Mosque *(✓ Verified by community)*")] 
    [InlineData("Dolmabahçe Palace", "- Dolmabahçe Palace", "- Dolmabahçe Palace *(✓ Verified by community)*")] 
    [InlineData("Garden 1897 Restaurant", "- Garden 1897 Restaurant", "- Garden 1897 Restaurant *(✓ Verified by community)*")] 
    
    // 2. Generic matches should be rejected (NO FALSE POSITIVES)
    [InlineData("Garden 1897 Restaurant", "- Restaurant", "- Restaurant")] 
    [InlineData("Conrad Istanbul Bosphorus", "- hotel", "- hotel")] 
    [InlineData("Mahalle Restoran", "- Breakfast", "- Breakfast")] 
    [InlineData("Mahalle Restoran", "- Lunch", "- Lunch")] 
    [InlineData("Mahalle Restoran", "- Dinner at a nearby café", "- Dinner at a nearby café")] 
    [InlineData("Mahalle Restoran", "- nearby restaurant", "- nearby restaurant")] 
    [InlineData("Museum Pass Istanbul", "- Purchase a Museum Pass Istanbul", "- Purchase a Museum Pass Istanbul")] 

    // 3. Markdown parsing & idempotency
    [InlineData("Hagia Sophia Grand Mosque", "- Visit **Hagia Sophia**", "- Visit **Hagia Sophia** *(✓ Verified by community)*")] 
    [InlineData("Hagia Sophia Grand Mosque", "- Hagia Sophia *(✓ Verified by community)*", "- Hagia Sophia *(✓ Verified by community)*")] 

    // 4. Unicode and Turkish Character Matching
    [InlineData("İzmir Historical Elevator Building", "- İzmir Historical Elevator Building", "- İzmir Historical Elevator Building *(✓ Verified by community)*")] 

    // 5. Unrelated place
    [InlineData("Eiffel Tower", "- Random Place", "- Random Place")] 
    public async Task Handle_VerificationMatrix_AppliesCorrectly(string dbPlaceName, string generatedLine, string expectedLine)
    {
        // Arrange
        var prompt = "Turkey";
        var publicTrips = new List<PublicTripSearchResponse>
        {
            new PublicTripSearchResponse(Guid.NewGuid(), Guid.NewGuid(), "Trip", "Turkey", DateTime.Now, DateTime.Now.AddDays(5), 5, 100, "Author", "", 0, 0, 5, DateTime.Now, false, ItineraryJson: $"{{\"days\":[{{\"items\":[{{\"title\":\"{dbPlaceName}\"}}]}}]}}")
        };

        _senderMock.Setup(s => s.Send(It.IsAny<SearchPublicTripsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyCollection<PublicTripSearchResponse>>.Success(publicTrips));

        _aiProviderMock.Setup(a => a.GetChatCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedLine);

        // Act
        var result = await _handler.Handle(new PlanTripAiCommand(prompt), CancellationToken.None);

        // Assert
        result.Value!.GeneratedItinerary.Should().Contain(expectedLine);
    }

    [Fact]
    public async Task Handle_MultipleVerifiedPlacesInOneItinerary() // 5. Multiple verified places
    {
        // Arrange
        var prompt = "Turkey";
        var publicTrips = new List<PublicTripSearchResponse>
        {
            new PublicTripSearchResponse(Guid.NewGuid(), Guid.NewGuid(), "Trip", "Turkey", DateTime.Now, DateTime.Now.AddDays(5), 5, 100, "Author", "", 0, 0, 5, DateTime.Now, false, ItineraryJson: "{\"days\":[{\"items\":[{\"title\":\"Hagia Sophia\"}, {\"title\":\"Blue Mosque\"}]}]}")
        };

        _senderMock.Setup(s => s.Send(It.IsAny<SearchPublicTripsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyCollection<PublicTripSearchResponse>>.Success(publicTrips));

        _aiProviderMock.Setup(a => a.GetChatCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("- Hagia Sophia\n- Blue Mosque");

        // Act
        var result = await _handler.Handle(new PlanTripAiCommand(prompt), CancellationToken.None);

        // Assert
        result.Value!.GeneratedItinerary.Should().Contain("- Hagia Sophia *(✓ Verified by community)*");
        result.Value!.GeneratedItinerary.Should().Contain("- Blue Mosque *(✓ Verified by community)*");
    }

    [Fact]
    public async Task Handle_AiProviderFails_GracefullyDegradesAndKeepsPublicTrips() // Test 4
    {
        // Arrange
        var prompt = "Turkey";
        var publicTrips = new List<PublicTripSearchResponse>
        {
            new PublicTripSearchResponse(Guid.NewGuid(), Guid.NewGuid(), "Trip", "Turkey", DateTime.Now, DateTime.Now.AddDays(5), 5, 100, "Author", "", 0, 0, 5, DateTime.Now, false, ItineraryJson: "{}")
        };

        _senderMock.Setup(s => s.Send(It.IsAny<SearchPublicTripsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyCollection<PublicTripSearchResponse>>.Success(publicTrips));

        _aiProviderMock.Setup(a => a.GetChatCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("429 RESOURCE_EXHAUSTED"));

        // Act
        var result = await _handler.Handle(new PlanTripAiCommand(prompt), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.GeneratedItinerary.Should().BeNull(); // AI failed, so it's null
        result.Value.RelatedPublicTrips.Should().HaveCount(1); // But public trips are retained!
    }
    [Fact]
    public async Task Handle_NoPublicTrips_AiProviderFails_GracefullyDegrades() // Test 4
    {
        // Arrange
        var prompt = "Japan";
        var publicTrips = new List<PublicTripSearchResponse>(); // Empty

        _senderMock.Setup(s => s.Send(It.IsAny<SearchPublicTripsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyCollection<PublicTripSearchResponse>>.Success(publicTrips));

        _aiProviderMock.Setup(a => a.GetChatCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Timeout"));

        // Act
        var result = await _handler.Handle(new PlanTripAiCommand(prompt), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.GeneratedItinerary.Should().BeNull(); // AI failed gracefully
        result.Value.RelatedPublicTrips.Should().BeEmpty();
    }
}
