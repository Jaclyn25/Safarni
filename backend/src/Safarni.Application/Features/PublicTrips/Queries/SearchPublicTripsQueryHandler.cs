using MediatR;
using Microsoft.Extensions.Options;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Configuration;
using Safarni.Application.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Safarni.Application.Features.PublicTrips.Queries;

public sealed class SearchPublicTripsQueryHandler
    : IRequestHandler<SearchPublicTripsQuery, Result<IReadOnlyCollection<PublicTripSearchResponse>>>
{
    private readonly IPublicTripRepository _repository;
    private readonly PublicTripRankingOptions _rankingOptions;

    public SearchPublicTripsQueryHandler(
        IPublicTripRepository repository,
        IOptions<PublicTripRankingOptions>? rankingOptions = null)
    {
        _repository = repository;
        _rankingOptions = rankingOptions?.Value ?? new PublicTripRankingOptions();
    }

    public async Task<Result<IReadOnlyCollection<PublicTripSearchResponse>>> Handle(
        SearchPublicTripsQuery request,
        CancellationToken cancellationToken)
    {
        var versions = await _repository.SearchPublicTripsAsync(
            request.Destination,
            request.MinDuration,
            request.MaxDuration,
            request.MaxBudget,
            request.SortOrder,
            cancellationToken);

        var rawResponses = versions.Select(v =>
        {
            var trip = v.Snapshot.Trip;
            var user = trip.User;
            var durationDays = (trip.EndDate - trip.StartDate).Days;

            var isUserDeleted = !user.IsActive;
            var authorName = isUserDeleted ? "Deleted User" : user.FullName;
            var authorPhotoUrl = isUserDeleted ? string.Empty : (user.ProfilePhotoUrl ?? string.Empty);
            
            var isNewTrip = v.RatingsCount < 3;

            return new PublicTripSearchResponse(
                v.Id,
                trip.Id,
                trip.Name,
                trip.Destination,
                trip.StartDate,
                trip.EndDate,
                durationDays,
                v.CostAmount,
                authorName,
                authorPhotoUrl,
                v.ViewsCount,
                v.LikesCount,
                v.AverageRating,
                trip.PublishedAt ?? v.CreatedAt,
                isNewTrip,
                v.ItineraryJson ?? ""
            );
        }).ToList();

        var applyRanking = string.IsNullOrWhiteSpace(request.SortOrder) || request.SortOrder.Equals("relevance", StringComparison.OrdinalIgnoreCase);

        if (!applyRanking)
        {
            if (string.Equals(request.SortOrder, "newest", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(request.SortOrder))
            {
                return Result<IReadOnlyCollection<PublicTripSearchResponse>>.Success(
                    rawResponses.OrderByDescending(r => r.PublishedAt).ToList()
                );
            }
            return Result<IReadOnlyCollection<PublicTripSearchResponse>>.Success(rawResponses);
        }

        var scoredItems = rawResponses.Select(r =>
        {
            var version = versions.First(v => v.Id == r.VersionId);
            var user = version.Snapshot.Trip.User;
            var trip = version.Snapshot.Trip;
            
            var score = CalculateRankingScore(version, user, trip, request.Destination);
            return new ScoredItem(r, score, version.CostAmount);
        }).ToList();

        var newTrips = scoredItems.Where(x => x.Response.IsNewTrip)
                                  .OrderByDescending(x => x.Response.PublishedAt)
                                  .Select(x => x.Response)
                                  .ToList();

        var mainRanked = scoredItems.Where(x => !x.Response.IsNewTrip)
                                    .OrderByDescending(x => x.Score)
                                    .ThenByDescending(x => x.Response.PublishedAt)
                                    .ToList();

        var diversifiedMainRanked = ApplyBudgetDiversity(mainRanked);

        var finalResult = diversifiedMainRanked.Concat(newTrips).ToList();

        return Result<IReadOnlyCollection<PublicTripSearchResponse>>.Success(finalResult);
    }

    private double CalculateRankingScore(Domain.Entities.SnapshotVersion version, Domain.Entities.User user, Domain.Entities.Trip trip, string? searchedDestination)
    {
        var normalizedRating = version.AverageRating / 5.0;

        var isAgencyVerified = user.TravelAgency?.VerificationStatus == Domain.Enums.AgencyVerificationStatus.Verified;
        var normalizedVerification = isAgencyVerified ? 1.0 : (user.IsEmailVerified ? 1.0 : 0.0);

        var daysSincePublished = (DateTime.UtcNow - (trip.PublishedAt ?? version.CreatedAt)).TotalDays;
        var normalizedRecency = 1.0;
        if (daysSincePublished > _rankingOptions.RecencyThresholdDays)
        {
            normalizedRecency = Math.Exp(-_rankingOptions.RecencyDecayRate * (daysSincePublished - _rankingOptions.RecencyThresholdDays));
        }

        var engagementSum = version.ViewsCount + (version.LikesCount * 5) + (version.SavesCount * 10) + (version.CopiesCount * 20);
        var normalizedEngagement = Math.Min(1.0, Math.Log10(1 + engagementSum) / 5.0);

        var normalizedRelevance = 1.0;
        if (!string.IsNullOrWhiteSpace(searchedDestination))
        {
            if (trip.Destination.Equals(searchedDestination, StringComparison.OrdinalIgnoreCase))
                normalizedRelevance = 1.0;
            else if (trip.Destination.Contains(searchedDestination, StringComparison.OrdinalIgnoreCase))
                normalizedRelevance = 0.8;
            else
                normalizedRelevance = 0.5;
        }

        var normalizedAuthorCredibility = user.IsActive ? 1.0 : 0.0;

        return (normalizedRating * _rankingOptions.RatingWeight)
             + (normalizedVerification * _rankingOptions.VerificationWeight)
             + (normalizedRecency * _rankingOptions.RecencyWeight)
             + (normalizedEngagement * _rankingOptions.EngagementWeight)
             + (normalizedRelevance * _rankingOptions.RelevanceWeight)
             + (normalizedAuthorCredibility * _rankingOptions.AuthorCredibilityWeight);
    }

    private List<PublicTripSearchResponse> ApplyBudgetDiversity(List<ScoredItem> mainRanked)
    {
        if (mainRanked.Count <= 2)
            return mainRanked.Select(x => x.Response).ToList();

        var result = new List<PublicTripSearchResponse>();
        var remaining = mainRanked.ToList();

        string GetTier(decimal? cost)
        {
            if (!cost.HasValue) return "Unknown";
            if (cost < 500) return "Low";
            if (cost < 2000) return "Medium";
            return "High";
        }

        while (remaining.Any())
        {
            var candidateIndex = 0;
            var candidate = remaining[0];
            
            if (result.Count >= 2)
            {
                var prev1 = GetTier(result[^1].CostAmount);
                var prev2 = GetTier(result[^2].CostAmount);

                if (prev1 == prev2 && GetTier(candidate.CostAmount) == prev1)
                {
                    for (int i = 1; i < remaining.Count; i++)
                    {
                        var alt = remaining[i];
                        if (candidate.Score - alt.Score > _rankingOptions.DiversityScoreTolerance)
                            break;

                        if (GetTier(alt.CostAmount) != prev1)
                        {
                            candidateIndex = i;
                            candidate = alt;
                            break;
                        }
                    }
                }
            }

            result.Add(candidate.Response);
            remaining.RemoveAt(candidateIndex);
        }

        return result;
    }

    private sealed record ScoredItem(PublicTripSearchResponse Response, double Score, decimal? CostAmount);
}
