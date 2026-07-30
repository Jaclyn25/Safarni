using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Features.Trips;
using Safarni.Application.Features.Trips.CreateTrip;
using Safarni.Domain.Entities;
using Xunit;

namespace Safarni.Application.UnitTests.Trips;

public class CreateTripCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ITripRepository> _tripRepositoryMock;
    private readonly CreateTripCommandHandler _handler;

    public CreateTripCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _tripRepositoryMock = new Mock<ITripRepository>();
        _handler = new CreateTripCommandHandler(_currentUserServiceMock.Object, _tripRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessResult_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateTripCommand(
            "Paris Trip",
            "France",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(7),
            "Egypt",
            [new TripCitySelection("Paris", 7)]);
        var userId = Guid.NewGuid();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Paris Trip");
        result.Value.Destination.Should().Be("France");
        result.Value.OriginCountry.Should().Be("Egypt");
        result.Value.Cities.Single().Name.Should().Be("Paris");
        
        _tripRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()), Times.Once);
        _tripRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailureResult_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var command = new CreateTripCommand(
            "Paris Trip",
            "France",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(7),
            "Egypt",
            [new TripCitySelection("Paris", 7)]);
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.AuthenticationRequired");
    }
}

public class CreateTripCommandValidatorTests
{
    private readonly CreateTripCommandValidator _validator;

    public CreateTripCommandValidatorTests()
    {
        _validator = new CreateTripCommandValidator();
    }

    [Fact]
    public void Validate_Should_HaveError_WhenNameIsEmpty()
    {
        var command = new CreateTripCommand(
            "",
            "France",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(7),
            "Egypt",
            [new TripCitySelection("Paris", 7)]);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
    
    [Fact]
    public void Validate_Should_HaveError_WhenEndDateIsBeforeStartDate()
    {
        var command = new CreateTripCommand(
            "Paris Trip",
            "France",
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(1),
            "Egypt",
            [new TripCitySelection("Paris", 1)]);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EndDate");
    }

    [Fact]
    public void Validate_Should_RejectMoreCitiesThanTripDays()
    {
        var date = DateTime.UtcNow.AddDays(1).Date;
        var command = new CreateTripCommand(
            "One day in France",
            "France",
            date,
            date,
            "Egypt",
            [
                new TripCitySelection("Paris", 1),
                new TripCitySelection("Lyon", 1)
            ]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage ==
            "Choose more days in your trip to be able to choose more cities.");
    }

    [Fact]
    public void Validate_Should_RequireCityDaysToEqualTripDays()
    {
        var startDate =
            DateTime.UtcNow.AddDays(1).Date;
        var command = new CreateTripCommand(
            "Four days in France",
            "France",
            startDate,
            startDate.AddDays(3),
            "Egypt",
            [
                new TripCitySelection("Paris", 2),
                new TripCitySelection("Lyon", 1)
            ]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage ==
            "City day allocations must equal the total trip days.");
    }
}
