using FluentValidation;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

public sealed class GenerateItineraryCommandValidator
    : AbstractValidator<GenerateItineraryCommand>
{
    public GenerateItineraryCommandValidator()
    {
        RuleFor(command => command.TripId).NotEmpty();
        RuleFor(command => command.Preferences)
            .NotNull()
            .SetValidator(new GenerateItineraryPreferencesValidator());
        RuleFor(command => command.BudgetAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(10_000_000)
            .When(command => command.BudgetAmount.HasValue);
        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage(
                "Currency code must contain exactly three letters.");
        RuleFor(command => command.TravelerCount)
            .InclusiveBetween(1, 20);
    }
}

public sealed class GenerateItineraryPreferencesValidator
    : AbstractValidator<GenerateItineraryPreferences>
{
    private static readonly HashSet<string> SupportedInterests =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "adventure", "nature", "culture", "food", "shopping",
            "nightlife", "relaxation", "family"
        };

    private static readonly HashSet<string> SupportedPaces =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "relaxed", "balanced", "active"
        };

    private static readonly HashSet<string> SupportedTransportation =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "walking", "public_transport", "taxi_ride_share",
            "rental_car", "bicycle", "mixed"
        };

    private static readonly HashSet<string> SupportedAccommodationStyles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "already_booked", "budget", "mid_range", "luxury",
            "boutique", "resort"
        };

    private static readonly HashSet<string> SupportedLanguages =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "en", "ar"
        };

    public GenerateItineraryPreferencesValidator()
    {
        RuleFor(preferences => preferences.Interests)
            .Must(values => HasSupportedUniqueValues(
                values,
                SupportedInterests,
                1,
                5))
            .WithMessage(
                "Select between one and five supported interests.");

        RuleFor(preferences => preferences.Pace)
            .Must(value => IsSupported(value, SupportedPaces))
            .WithMessage("Select a supported travel pace.");

        RuleFor(preferences => preferences.Transportation)
            .Must(values => HasSupportedUniqueValues(
                values,
                SupportedTransportation,
                1,
                3))
            .WithMessage(
                "Select between one and three supported transportation options.");

        RuleFor(preferences => preferences.AccommodationStyle)
            .Must(value =>
                IsSupported(value, SupportedAccommodationStyles))
            .WithMessage(
                "Select a supported accommodation style.");

        RuleFor(preferences => preferences.PreferredLanguage)
            .Must(value => IsSupported(value, SupportedLanguages))
            .WithMessage(
                "Preferred language must be either 'en' or 'ar'.");

        RuleFor(preferences => preferences.AdditionalNotes)
            .MaximumLength(500);
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
