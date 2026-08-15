namespace Safarni.Application.Common.Configuration;

public class PublicTripRankingOptions
{
    public double RatingWeight { get; set; } = 0.25;
    public double VerificationWeight { get; set; } = 0.15;
    public double EngagementWeight { get; set; } = 0.20;
    public double RecencyWeight { get; set; } = 0.15;
    public double RelevanceWeight { get; set; } = 0.15;
    public double AuthorCredibilityWeight { get; set; } = 0.10;

    public double RecencyThresholdDays { get; set; } = 90.0;
    public double RecencyDecayRate { get; set; } = 0.01;

    public double DiversityScoreTolerance { get; set; } = 0.10;
}
