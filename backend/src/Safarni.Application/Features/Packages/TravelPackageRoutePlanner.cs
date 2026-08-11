using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Packages;

internal static class TravelPackageRoutePlanner
{
    public static TravelPackageCity? ResolveCity(TravelPackage package, int dayNumber)
    {
        var cities = package.Cities.OrderBy(city => city.OrderIndex).ToArray();
        if (cities.Length == 0) return null;

        var allocatedDays = 0;
        for (var index = 0; index < cities.Length - 1; index++)
        {
            allocatedDays += cities[index].Nights;
            if (dayNumber <= allocatedDays) return cities[index];
        }

        // The last route stop owns the departure day and any remaining days. This supports
        // both common shapes: 5 days / 4 nights and 5 days / 5 nights.
        return cities[^1];
    }

    public static void AssignCitiesToDays(TravelPackage package)
    {
        foreach (var day in package.Days)
        {
            var city = ResolveCity(package, day.DayNumber);
            day.TravelPackageCityId = city?.Id;
            day.TravelPackageCity = city;
        }
    }

    public static int TotalNights(TravelPackage package) => package.Cities.Sum(city => city.Nights);

    public static bool HasValidNightAllocation(TravelPackage package)
    {
        var totalNights = TotalNights(package);
        return package.Cities.Count > 0 &&
            totalNights >= Math.Max(1, package.DurationDays - 1) &&
            totalNights <= package.DurationDays;
    }
}
