using FluentValidation;

namespace Safarni.Application.Features.Trips.GetNearbyAttractions;

public sealed class GetNearbyAttractionsQueryValidator : AbstractValidator<GetNearbyAttractionsQuery>
{
    public GetNearbyAttractionsQueryValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.Radius).GreaterThan(0).WithMessage("Radius must be greater than 0.");
        // We can add validation for Category if needed, but it's optional in the query.
    }
}
