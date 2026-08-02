using FluentValidation;

namespace Safarni.Application.Features.Trips.GetTripRoute;

public sealed class GetTripRouteQueryValidator : AbstractValidator<GetTripRouteQuery>
{
    public GetTripRouteQueryValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
    }
}
