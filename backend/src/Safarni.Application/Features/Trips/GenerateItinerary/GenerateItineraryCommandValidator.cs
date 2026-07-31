using FluentValidation;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

public sealed class GenerateItineraryCommandValidator
    : AbstractValidator<GenerateItineraryCommand>
{
    private static readonly HashSet<string> SupportedInterests =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "adventure",
            "nature",
            "culture",
            "food",
            "shopping",
            "nightlife",
            "relaxation",
            "family"
        };

    private static readonly HashSet<string> SupportedPaces =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "relaxed",
            "balanced",
            "active"
        };

    private static readonly HashSet<string> SupportedTransportation =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "walking",
            "public_transport",
            "taxi_ride_share",
            "rental_car",
            "bicycle",
            "mixed"
        };

    private static readonly HashSet<string> SupportedAccommodationStyles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "already_booked",
            "budget",
            "mid_range",
            "luxury",
            "boutique",
            "resort"
        };

    private static readonly HashSet<string> SupportedDietaryNeeds =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "vegetarian",
            "vegan",
            "halal",
            "kosher",
            "gluten_free",
            "food_allergies"
        };

    private static readonly HashSet<string> SupportedAccessibilityNeeds =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "wheelchair_accessible",
            "limited_walking",
            "step_free",
            "visual_support",
            "hearing_support"
        };

    private static readonly HashSet<string> SupportedLanguages =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "en",
            "ar"
        };

    public GenerateItineraryCommandValidator()
    {
        RuleFor(command => command.TripId)
            .NotEmpty();

        RuleFor(command => command.Preferences)
            .NotNull();

        When(command => command.Preferences is not null, () =>
        {
            RuleFor(command => command.Preferences.Interests)
                .Must(interests =>
                    HasSupportedUniqueValues(
                        interests,
                        SupportedInterests,
                        minimumCount: 1,
                        maximumCount: 5))
                .WithMessage(
                    "Select between one and five supported interests.");

            RuleFor(command => command.Preferences.Pace)
                .Must(value => IsSupported(value, SupportedPaces))
                .WithMessage("Select a supported travel pace.");

            RuleFor(command => command.Preferences.Transportation)
                .Must(transportation =>
                    HasSupportedUniqueValues(
                        transportation,
                        SupportedTransportation,
                        minimumCount: 1,
                        maximumCount: 3))
                .WithMessage(
                    "Select between one and three supported transportation options.");

            RuleFor(command => command.Preferences.AccommodationStyle)
                .Must(value =>
                    IsSupported(
                        value,
                        SupportedAccommodationStyles))
                .WithMessage(
                    "Select a supported accommodation style.");

            RuleFor(command => command.Preferences.DietaryNeeds)
                .Must(values =>
                    HasSupportedUniqueValues(
                        values,
                        SupportedDietaryNeeds,
                        minimumCount: 0,
                        maximumCount: 4))
                .WithMessage(
                    "Select up to four supported dietary needs.");

            RuleFor(command => command.Preferences.AccessibilityNeeds)
                .Must(values =>
                    HasSupportedUniqueValues(
                        values,
                        SupportedAccessibilityNeeds,
                        minimumCount: 0,
                        maximumCount: 4))
                .WithMessage(
                    "Select up to four supported accessibility needs.");

            RuleFor(command => command.Preferences.PreferredLanguage)
                .Must(value => IsSupported(value, SupportedLanguages))
                .WithMessage(
                    "Preferred language must be either 'en' or 'ar'.");

            RuleFor(command => command.Preferences.AdditionalNotes)
                .MaximumLength(500);
        });

        RuleFor(command => command.BudgetAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(10_000_000);

        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must contain exactly three letters.");

        RuleFor(command => command.TravelerCount)
            .InclusiveBetween(1, 20);

    }

    private static bool IsSupported(
        string? value,
        IReadOnlySet<string> supportedValues)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               supportedValues.Contains(value.Trim());
    }

    private static bool HasSupportedUniqueValues(
        IReadOnlyList<string>? values,
        IReadOnlySet<string> supportedValues,
        int minimumCount,
        int maximumCount)
    {
        if (values is null ||
            values.Count < minimumCount ||
            values.Count > maximumCount)
        {
            return false;
        }

        var normalizedValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToList();

        return normalizedValues.Count == values.Count &&
               normalizedValues.All(supportedValues.Contains) &&
               normalizedValues
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .Count() == normalizedValues.Count;
    }
}
