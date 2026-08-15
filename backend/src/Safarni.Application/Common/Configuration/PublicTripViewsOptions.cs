namespace Safarni.Application.Common.Configuration;

public class PublicTripViewsOptions
{
    public const string SectionName = "PublicTripViews";

    public int DeduplicationWindowMinutes { get; set; } = 30;
}
