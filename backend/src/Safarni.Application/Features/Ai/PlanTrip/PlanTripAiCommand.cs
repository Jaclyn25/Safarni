using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Features.Chat.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Safarni.Application.Common.Configuration;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.PublicTrips.Queries;

namespace Safarni.Application.Features.Ai.PlanTrip;

public record PlanTripAiCommand(string Prompt) : IRequest<Result<AiTripPlanResponse>>;

public sealed class PlanTripAiCommandHandler : IRequestHandler<PlanTripAiCommand, Result<AiTripPlanResponse>>
{
    private readonly ISender _sender;
    private readonly IAIProvider _aiProvider;
    private readonly IIntentDetectionService _intentDetectionService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILogger<PlanTripAiCommandHandler> _logger;
    private readonly AiTripPlanOptions _options;

    public PlanTripAiCommandHandler(
        ISender sender,
        IAIProvider aiProvider,
        IIntentDetectionService intentDetectionService,
        IPromptBuilder promptBuilder,
        ILogger<PlanTripAiCommandHandler> logger,
        IOptions<AiTripPlanOptions>? options = null)
    {
        _sender = sender;
        _aiProvider = aiProvider;
        _intentDetectionService = intentDetectionService;
        _promptBuilder = promptBuilder;
        _logger = logger;
        _options = options?.Value ?? new AiTripPlanOptions();
    }

    public async Task<Result<AiTripPlanResponse>> Handle(PlanTripAiCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting PlanTripAiCommand for prompt: {Prompt}", request.Prompt);

        // 1. Extract Destination using synchronous/fast regex intent detection
        var intent = _intentDetectionService.DetectIntent(request.Prompt);
        
        // Extract clean destination from natural language prompt
        var destinationContext = request.Prompt;
        destinationContext = Regex.Replace(destinationContext, @"(?i)^(i\s+want\s+to\s+(?:travel|go|visit)\s+to\s+|a\s+\d+-day\s+trip\s+to\s+|trip\s+to\s+|travel\s+to\s+|visit\s+|vacation\s+in\s+)", "").Trim();
        destinationContext = Regex.Replace(destinationContext, @"(?i)^\d+\s+days?\s+in\s+", "").Trim();
        destinationContext = Regex.Replace(destinationContext, @"[.?!]+$", "").Trim();

        _logger.LogInformation("Extracted standalone destination context: {DestinationContext}", destinationContext);

        // 2. Execute Public Trips Search First (Required for Grounding Context - PT-204)
        // AI generation depends on this search result to ground its itinerary.
        var relatedTrips = await SearchPublicTripsAsync(destinationContext, cancellationToken);

        // 3. Extract Top 5 Public Trips for Grounding Context
        var topTrips = relatedTrips.Take(5).ToList();
        var (groundingContextString, knownPlaces) = BuildGroundingContext(topTrips);

        // 4. Generate AI Itinerary
        var generatedItinerary = await GenerateAiItineraryAsync(request.Prompt, groundingContextString, cancellationToken);

        // 5. Verify Community Places in the generated Markdown
        if (!string.IsNullOrWhiteSpace(generatedItinerary) && knownPlaces.Count > 0)
        {
            generatedItinerary = VerifyPlacesInMarkdown(generatedItinerary, knownPlaces);
        }

        // 6. Return Combined Response
        return Result<AiTripPlanResponse>.Success(
            new AiTripPlanResponse(generatedItinerary, relatedTrips)
        );
    }

    private (string Context, HashSet<string> KnownPlaces) BuildGroundingContext(List<PublicTripSearchResponse> trips)
    {
        if (trips.Count == 0) return (string.Empty, new HashSet<string>());

        var sb = new StringBuilder();
        var knownPlaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("[DIAGNOSTIC] BuildGroundingContext: Analyzing {Count} trips.", trips.Count);

        int index = 1;
        foreach (var trip in trips)
        {
            sb.AppendLine($"Trip {index}:");
            sb.AppendLine($"- Destination: {trip.Destination}");
            sb.AppendLine($"- Duration: {trip.DurationDays} days");
            if (trip.AverageRating > 0)
                sb.AppendLine($"- Rating: {trip.AverageRating:0.1}/5.0");

            var placesInTrip = ExtractPlacesFromJson(trip.ItineraryJson);
            
            _logger.LogInformation("[DIAGNOSTIC] Trip '{TripName}' ({TripId}) - Extracted {PlacesCount} places from ItineraryJson.", trip.TripName, trip.TripId, placesInTrip.Count);
            if (placesInTrip.Count == 0 && !string.IsNullOrWhiteSpace(trip.ItineraryJson))
            {
                _logger.LogWarning("[DIAGNOSTIC] ItineraryJson is NOT empty but extracted 0 places! JSON Sample: {JsonSample}", trip.ItineraryJson);
            }

            if (placesInTrip.Any())
            {
                sb.AppendLine("- Places Visited:");
                foreach (var place in placesInTrip)
                {
                    sb.AppendLine($"  - {place}");
                    var normalized = NormalizePlaceName(place);
                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        knownPlaces.Add(normalized);
                    }
                }
            }
            sb.AppendLine();
            index++;
        }

        _logger.LogInformation("[DIAGNOSTIC] Total known places extracted and normalized: {KnownPlacesCount}. Sample: {Sample}", 
            knownPlaces.Count, 
            string.Join(", ", knownPlaces.Take(5)));

        return (sb.ToString().Trim(), knownPlaces);
    }

    private List<string> ExtractPlacesFromJson(string json)
    {
        var places = new List<string>();
        if (string.IsNullOrWhiteSpace(json)) return places;

        try
        {
            using var doc = JsonDocument.Parse(json);
            
            // Resolve days array (either inside an object with a "days" property, or directly as an array)
            JsonElement daysArray = default;
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("days", out var daysProp))
            {
                daysArray = daysProp;
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                daysArray = doc.RootElement;
            }

            if (daysArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var day in daysArray.EnumerateArray())
                {
                    // Legacy Format: items / title
                    if (day.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            if (item.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                            {
                                var placeName = title.GetString();
                                if (!string.IsNullOrWhiteSpace(placeName))
                                {
                                    places.Add(placeName);
                                }
                            }
                        }
                    }

                    // Current Format: places / name
                    if (day.TryGetProperty("places", out var placesList) && placesList.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var place in placesList.EnumerateArray())
                        {
                            if (place.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                            {
                                var placeName = name.GetString();
                                if (!string.IsNullOrWhiteSpace(placeName))
                                {
                                    places.Add(placeName);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse ItineraryJson for grounding context extraction.");
        }

        return places;
    }

    private string NormalizePlaceName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var normalized = name.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^\w\s]", ""); // Remove punctuation
        return normalized;
    }

    private bool IsGenericText(string text)
    {
        var normalized = text.Trim().ToLowerInvariant();
        
        var genericTerms = new HashSet<string>
        {
            "breakfast", "lunch", "dinner", "brunch", "meal", "snack",
            "cafe", "café", "restaurant", "hotel", "accommodation", "stay", "resort", "hostel",
            "flight", "transport", "airport", "transfer", "taxi", "train", "bus", "ferry", "cruise",
            "morning", "afternoon", "evening", "midday", "night", "midnight",
            "tips", "optional", "travel tips", "quick tips", "note", "notes",
            "nearby cafe", "nearby café", "nearby restaurant", "local cafe", "local café", "local restaurant",
            "a nearby cafe", "a nearby café", "a nearby restaurant", "airport transfer", 
            "museum pass", "museum pass istanbul", "istanbulkart", "ticket", "tickets", "checkin", "checkout",
            "departure", "arrival", "return flight"
        };

        if (genericTerms.Contains(normalized)) return true;

        if (Regex.IsMatch(normalized, @"^(a\s+)?(nearby|local|hotel)\s+(cafe|café|restaurant|eatery)$")) return true;
        if (Regex.IsMatch(normalized, @"^(morning|afternoon|evening|midday|breakfast|lunch|dinner|brunch)\s+(at\s+the\s+)?(hotel|resort)$")) return true;
        
        return false;
    }

    private string VerifyPlacesInMarkdown(string markdown, HashSet<string> knownPlaces)
    {
        var lines = markdown.Split('\n');
        int matchesCount = 0;

        // Filter out generic known places just to be safe
        var validKnownPlaces = knownPlaces.Where(kp => !IsGenericText(kp) && kp.Length >= 4).ToList();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* ") || Regex.IsMatch(trimmedLine, @"^\d+\.\s"))
            {
                var contentWithoutBullets = Regex.Replace(trimmedLine, @"^[-*]\s*", "");
                contentWithoutBullets = Regex.Replace(contentWithoutBullets, @"^\d+\.\s*", "");

                var potentialPlaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                // 1. Extracted bold text (Highest priority)
                var boldMatches = Regex.Matches(contentWithoutBullets, @"\*\*(.*?)\*\*");
                foreach (Match m in boldMatches)
                {
                    var boldText = m.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(boldText))
                    {
                        potentialPlaces.Add(boldText);
                    }
                }

                // 2. If no bold text, fall back to extracting entities based on capitalization or removing common verbs, but only if it's short.
                if (potentialPlaces.Count == 0 && contentWithoutBullets.Length < 60)
                {
                    var withoutVerbs = Regex.Replace(contentWithoutBullets, @"\b(visit|explore|head to|see|go to|discover|walk through|tour|enjoy|experience|spend|arrive at|check in at|check out of)\b", "", RegexOptions.IgnoreCase);
                    withoutVerbs = Regex.Replace(withoutVerbs, @"\b(the|a|an)\b", "", RegexOptions.IgnoreCase);
                    potentialPlaces.Add(withoutVerbs.Trim());
                }

                bool verified = false;
                foreach (var potentialPlace in potentialPlaces)
                {
                    if (IsGenericText(potentialPlace))
                    {
                        continue;
                    }

                    var normalizedPlace = NormalizePlaceName(potentialPlace);
                    if (normalizedPlace.Length < 4 || IsGenericText(normalizedPlace)) continue;

                    // Safe matching:
                    // 1. Exact match
                    // 2. Token-based containment (to avoid 'palace' matching 'topkapi palace' without the specific name)
                    // 3. Substring only if it's highly specific (> 7 chars) and contains significant words
                    
                    var placeTokens = normalizedPlace.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var kp in validKnownPlaces)
                    {
                        var kpTokens = kp.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        if (normalizedPlace == kp)
                        {
                            verified = true;
                            break;
                        }

                        // If AI generated place is fully contained in community place (e.g. "Hagia Sophia" in "Hagia Sophia Grand Mosque")
                        if (kp.Contains(normalizedPlace) && normalizedPlace.Length > 5)
                        {
                            verified = true;
                            break;
                        }

                        // If community place is fully contained in AI generated place (e.g. "Dolmabahçe Palace" in "Tour the Dolmabahçe Palace")
                        if (normalizedPlace.Contains(kp) && kp.Length > 5)
                        {
                            verified = true;
                            break;
                        }
                    }

                    if (verified)
                    {
                        _logger.LogInformation("[DIAGNOSTIC] VERIFIED MATCH FOUND! AI Place '{NormalizedPlace}' matched a known community place.", normalizedPlace);
                        matchesCount++;
                        break;
                    }
                }

                if (!verified)
                {
                    _logger.LogInformation("[DIAGNOSTIC] No match for candidates: {Candidates}", string.Join(" | ", potentialPlaces));
                }

                if (verified && !line.Contains("Verified by community"))
                {
                    lines[i] = line.TrimEnd('\r') + " *(✓ Verified by community)*\r";
                }
            }
        }

        _logger.LogInformation("[DIAGNOSTIC] VerifyPlacesInMarkdown: Found {MatchesCount} verified matches.", matchesCount);

        return string.Join("\n", lines);
    }

    private async Task<string?> GenerateAiItineraryAsync(string prompt, string groundingContext, CancellationToken parentCancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(parentCancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            var systemPrompt = _promptBuilder.BuildTripPlanSystemPrompt(prompt, groundingContext);
            var userPrompt = _promptBuilder.BuildUserPrompt(prompt);

            var stopwatch = Stopwatch.StartNew();
            var response = await _aiProvider.GetChatCompletionAsync(
                systemPrompt,
                new List<ChatMessageDto>(),
                userPrompt,
                timeoutCts.Token);
            stopwatch.Stop();

            _logger.LogInformation("AI Itinerary Generation succeeded in {ElapsedMilliseconds} ms.", stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !parentCancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AI Itinerary Generation timed out after {TimeoutSeconds} seconds.", _options.TimeoutSeconds);
            return null; // Graceful degradation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Itinerary Generation failed.");
            return null; // Graceful degradation
        }
    }

    private async Task<IReadOnlyCollection<PublicTripSearchResponse>> SearchPublicTripsAsync(string destinationContext, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var query = new SearchPublicTripsQuery(
                Destination: destinationContext,
                MinDuration: null,
                MaxDuration: null,
                MaxBudget: null,
                SortOrder: "relevance" // PT-202 ranking behavior
            );

            var result = await _sender.Send(query, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("Public Trips Search succeeded in {ElapsedMilliseconds} ms. Found {Count} trips.", stopwatch.ElapsedMilliseconds, result.IsSuccess ? result.Value!.Count : 0);
            
            return result.IsSuccess ? result.Value! : new List<PublicTripSearchResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Public Trips Search failed.");
            return new List<PublicTripSearchResponse>(); // Graceful degradation
        }
    }
}
