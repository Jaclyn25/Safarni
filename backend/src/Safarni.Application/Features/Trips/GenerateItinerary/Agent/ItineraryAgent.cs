using ChatBotApi.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.AI;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary.Agent;

/// <summary>
/// Bounded end-to-end agentic itinerary loop.
/// Large state is retained by the backend and represented to Gemini as a compact
/// snapshot, so prompt size stays relatively stable instead of growing with the
/// complete conversation history.
/// </summary>
internal static class ItineraryAgent
{
    private const int MaximumModelAttemptsPerStep = 2;
    private const int MaximumTotalToolExecutions = 14;
    private const int MaximumToolExecutionsPerDecision = 3;

    public static async Task<ItineraryAgentResult> RunAsync(
        IItineraryAgentModel model,
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences,
        decimal? budgetAmount,
        string currencyCode,
        int travelerCount,
        IReadOnlySet<string> previouslyUsedPlaceIds,
        Itinerary? previousItinerary,
        IItineraryTravelDataService? travelDataService,
        IItineraryRouteService? routeService,
        IFlightAirportService? flightService,
        Func<Guid?, int, string, CancellationToken, Task>? reportProgress,
        Guid? generationJobId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var context = new ItineraryAgentContext();
        var requiresFlightEvidence =
            flightService?.IsConfigured == true &&
            cityPlan.Count > 0 &&
            TripRouteSerializer.NormalizeAirportCode(
                trip.OriginAirportCode) is not null &&
            TripRouteSerializer.NormalizeAirportCode(
                cityPlan[0].AirportCode) is not null;

        var tools = BuildTools(
            trip,
            cityPlan,
            preferences,
            budgetAmount,
            currencyCode,
            travelerCount,
            previouslyUsedPlaceIds,
            previousItinerary,
            travelDataService,
            routeService,
            flightService,
            requiresFlightEvidence);
        var toolsByName = tools.ToDictionary(
            tool => tool.Definition.Name,
            StringComparer.OrdinalIgnoreCase);
        var definitions = tools
            .Select(tool => tool.Definition)
            .ToList();

        var previousPlaceNames = previousItinerary?.Days
            .SelectMany(day => day.Items)
            .Where(item =>
                item.Category is ItineraryItemCategory.Activity or
                    ItineraryItemCategory.Meal or
                    ItineraryItemCategory.Accommodation)
            .Select(item => item.Location ?? item.Title)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToList() ?? [];

        var goalMessage = new ItineraryAgentMessage(
            ItineraryAgentMessageRole.User,
            ItineraryAgentPromptBuilder.BuildGoal(
                trip,
                cityPlan,
                preferences,
                budgetAmount,
                currencyCode,
                travelerCount,
                previousItinerary is not null,
                previousPlaceNames));

        ItineraryAgentMessage? lastModelToolCall = null;
        ItineraryAgentMessage? lastToolResponse = null;
        string? recoveryInstruction = null;

        var maxSteps = Math.Clamp(model.MaxAgentSteps, 4, 10);
        var totalToolExecutions = 0;
        var modelCallCount = 0;
        long promptTokens = 0;
        long outputTokens = 0;
        long thinkingTokens = 0;
        long toolUsePromptTokens = 0;
        long cachedTokens = 0;
        long totalTokens = 0;

        try
        {
            for (var step = 1; step <= maxSteps; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ReportAsync(
                    reportProgress,
                    generationJobId,
                    Math.Min(70, 18 + (step * 5)),
                    $"Agent planning itinerary step {step}/{maxSteps}",
                    cancellationToken);

                // Token optimization: do not replay the entire agent transcript.
                // Keep only the stable goal, the immediately preceding function
                // call/response pair required by Gemini 3, and a compact snapshot
                // of authoritative server-side state.
                var messages = BuildCompactMessages(
                    goalMessage,
                    context,
                    lastModelToolCall,
                    lastToolResponse,
                    recoveryInstruction);

                var decision = await GetDecisionWithRecoveryAsync(
                    model,
                    messages,
                    definitions,
                    cancellationToken);

                modelCallCount++;
                if (decision.Usage is { } usage)
                {
                    promptTokens += usage.PromptTokenCount;
                    outputTokens += usage.CandidatesTokenCount;
                    thinkingTokens += usage.ThoughtsTokenCount;
                    toolUsePromptTokens += usage.ToolUsePromptTokenCount;
                    cachedTokens += usage.CachedContentTokenCount;
                    totalTokens += usage.TotalTokenCount;

                    logger.LogInformation(
                        "Gemini itinerary token usage for JobId={GenerationJobId}, TripId={TripId}, AgentStep={AgentStep}, ModelCall={ModelCall}. Prompt={PromptTokens}, Output={OutputTokens}, Thinking={ThinkingTokens}, ToolUsePrompt={ToolUsePromptTokens}, Cached={CachedTokens}, Total={TotalTokens}",
                        generationJobId,
                        trip.Id,
                        step,
                        modelCallCount,
                        usage.PromptTokenCount,
                        usage.CandidatesTokenCount,
                        usage.ThoughtsTokenCount,
                        usage.ToolUsePromptTokenCount,
                        usage.CachedContentTokenCount,
                        usage.TotalTokenCount);
                }

                if (decision.FunctionCalls.Count == 0)
                {
                    // A prose-only response is not useful in the bounded agent.
                    // Drop it instead of carrying it into future prompts.
                    lastModelToolCall = null;
                    lastToolResponse = null;
                    recoveryInstruction = context.ValidatedDays is not null
                        ? "The server-side draft is valid. Call submit_itinerary now; do not answer with prose."
                        : context.DraftDays is null
                            ? "Continue with tools. Gather missing evidence, create the complete itinerary once, and call set_itinerary_draft."
                            : "Continue with tools. The draft is server-side; use check_budget/validate_itinerary, revise only affected days when rejected, then submit after validation.";
                    continue;
                }

                var modelToolCallMessage = new ItineraryAgentMessage(
                    ItineraryAgentMessageRole.Model,
                    decision.Text,
                    decision.FunctionCalls,
                    RawContentJson: decision.RawContentJson);
                var results = new List<ItineraryAgentFunctionResult>();

                for (var callIndex = 0;
                     callIndex < decision.FunctionCalls.Count;
                     callIndex++)
                {
                    var functionCall = decision.FunctionCalls[callIndex];

                    if (callIndex >= MaximumToolExecutionsPerDecision ||
                        totalToolExecutions >= MaximumTotalToolExecutions)
                    {
                        results.Add(new ItineraryAgentFunctionResult(
                            functionCall.Id,
                            functionCall.Name,
                            "{\"error\":\"Safarni's bounded agent tool budget was reached. Use collected evidence, validate the best draft, and submit only if valid.\"}"));
                        continue;
                    }

                    if (!toolsByName.TryGetValue(
                            functionCall.Name,
                            out var tool))
                    {
                        results.Add(new ItineraryAgentFunctionResult(
                            functionCall.Id,
                            functionCall.Name,
                            "{\"error\":\"Unknown tool. Choose one of the declared Safarni itinerary tools.\"}"));
                        continue;
                    }

                    totalToolExecutions++;
                    ItineraryAgentToolExecution execution;

                    try
                    {
                        execution = await tool.ExecuteAsync(
                            context,
                            functionCall.ArgumentsJson,
                            cancellationToken);
                    }
                    catch (Exception exception)
                        when (exception is not OperationCanceledException &&
                              exception is not ItineraryGenerationCancelledException)
                    {
                        logger.LogWarning(
                            exception,
                            "Itinerary agent tool {ToolName} failed.",
                            functionCall.Name);

                        execution = new ItineraryAgentToolExecution(
                            "{\"error\":\"The tool failed. Choose another safe action or revise using already collected evidence.\"}",
                            $"Agent tool {functionCall.Name} failed");
                    }

                    results.Add(new ItineraryAgentFunctionResult(
                        functionCall.Id,
                        functionCall.Name,
                        execution.ResultJson));

                    await ReportAsync(
                        reportProgress,
                        generationJobId,
                        Math.Min(74, 20 + (step * 5)),
                        execution.Stage,
                        cancellationToken);
                }

                // Keep only this pair for the next request. Older tool exchanges
                // are represented by context.BuildCompactStateSummary().
                lastModelToolCall = modelToolCallMessage;
                lastToolResponse = new ItineraryAgentMessage(
                    ItineraryAgentMessageRole.Tool,
                    FunctionResults: results);
                recoveryInstruction = null;

                if (context.Submitted &&
                    context.ValidatedDays is not null)
                {
                    break;
                }
            }

            // If the bounded loop ended immediately after successful validation,
            // accept that exact validated server-side candidate. Never auto-accept
            // an unvalidated model draft.
            if (!context.Submitted && context.ValidatedDays is not null)
            {
                context.RecordAction(
                    "bounded_loop_auto_accept_after_validation");
            }

            if (context.ValidatedDays is null)
            {
                throw new ItineraryOutputException(
                    context.LastValidationError is null
                        ? "The itinerary agent did not produce a deterministically valid itinerary within its bounded loop."
                        : "The itinerary agent could not repair its candidate within the bounded loop. Last validation error: " +
                          context.LastValidationError);
            }

            if (context.Places.Count == 0)
            {
                throw new ItineraryOutputException(
                    "The itinerary agent produced no verified place catalog.");
            }

            logger.LogInformation(
                "Itinerary agent completed with {PlaceCount} verified candidates, {RouteChecks} route checks and actions: {Actions}.",
                context.Places.Count,
                context.RouteChecksPerformed,
                context.Actions.Count == 0
                    ? "none"
                    : string.Join(", ", context.Actions));

            return new ItineraryAgentResult(
                context.FlightPlan,
                context.Places,
                context.ValidatedDays,
                context.Actions);
        }
        finally
        {
            logger.LogInformation(
                "Gemini itinerary FINAL actual token summary. JobId={GenerationJobId}, TripId={TripId}, Model={ModelName}, SuccessfulModelCalls={ModelCallCount}, Prompt={PromptTokens}, Output={OutputTokens}, Thinking={ThinkingTokens}, ToolUsePrompt={ToolUsePromptTokens}, Cached={CachedTokens}, Total={TotalTokens}",
                generationJobId,
                trip.Id,
                model.ModelName,
                modelCallCount,
                promptTokens,
                outputTokens,
                thinkingTokens,
                toolUsePromptTokens,
                cachedTokens,
                totalTokens);
        }
    }

    private static IReadOnlyList<ItineraryAgentMessage> BuildCompactMessages(
        ItineraryAgentMessage goalMessage,
        ItineraryAgentContext context,
        ItineraryAgentMessage? lastModelToolCall,
        ItineraryAgentMessage? lastToolResponse,
        string? recoveryInstruction)
    {
        var messages = new List<ItineraryAgentMessage>
        {
            goalMessage
        };

        // Preserve exactly one recent Gemini function-call exchange. This keeps
        // function IDs/thought signatures valid without accumulating the full log.
        if (lastModelToolCall is not null && lastToolResponse is not null)
        {
            messages.Add(lastModelToolCall);
            messages.Add(lastToolResponse);
        }

        messages.Add(new ItineraryAgentMessage(
            ItineraryAgentMessageRole.User,
            context.BuildCompactStateSummary()));

        if (!string.IsNullOrWhiteSpace(recoveryInstruction))
        {
            messages.Add(new ItineraryAgentMessage(
                ItineraryAgentMessageRole.User,
                recoveryInstruction));
        }

        return messages;
    }

    private static IReadOnlyList<IItineraryAgentTool> BuildTools(
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences,
        decimal? budgetAmount,
        string currencyCode,
        int travelerCount,
        IReadOnlySet<string> previouslyUsedPlaceIds,
        Itinerary? previousItinerary,
        IItineraryTravelDataService? travelDataService,
        IItineraryRouteService? routeService,
        IFlightAirportService? flightService,
        bool requiresFlightEvidence)
    {
        return
        [
            new SearchFlightsAgentTool(
                trip,
                cityPlan,
                currencyCode,
                travelerCount,
                preferences.PreferredLanguage,
                flightService),
            new SearchVerifiedCatalogAgentTool(
                travelDataService,
                trip,
                cityPlan,
                preferences,
                currencyCode,
                travelerCount,
                previouslyUsedPlaceIds),
            new SearchHotelsAgentTool(
                travelDataService,
                trip,
                cityPlan,
                preferences,
                currencyCode,
                travelerCount),
            new CalculateRouteAgentTool(
                routeService,
                preferences),
            new SetItineraryDraftAgentTool(),
            new ReviseItineraryDayAgentTool(),
            new CheckBudgetAgentTool(
                budgetAmount,
                currencyCode),
            new ValidateItineraryAgentTool(
                trip,
                cityPlan,
                preferences,
                budgetAmount,
                previousItinerary,
                requiresFlightEvidence),
            new SubmitItineraryAgentTool()
        ];
    }

    private static async Task<ItineraryAgentModelResponse>
        GetDecisionWithRecoveryAsync(
            IItineraryAgentModel model,
            IReadOnlyList<ItineraryAgentMessage> messages,
            IReadOnlyList<ItineraryAgentToolDefinition> tools,
            CancellationToken cancellationToken)
    {
        AIProviderRateLimitException? lastRateLimit = null;

        for (var attempt = 1;
             attempt <= MaximumModelAttemptsPerStep;
             attempt++)
        {
            try
            {
                return await model.GetNextDecisionAsync(
                    ItineraryAgentPromptBuilder.SystemPrompt,
                    messages,
                    tools,
                    cancellationToken);
            }
            catch (AIProviderRateLimitException exception)
            {
                lastRateLimit = exception;
                if (attempt >= MaximumModelAttemptsPerStep)
                {
                    break;
                }

                var delay = exception.RetryAfter is { } retryAfter &&
                            retryAfter > TimeSpan.Zero
                    ? retryAfter + TimeSpan.FromSeconds(2)
                    : TimeSpan.FromSeconds(15);

                if (delay > TimeSpan.FromSeconds(60))
                {
                    delay = TimeSpan.FromSeconds(60);
                }

                await Task.Delay(delay, cancellationToken);
            }
        }

        if (lastRateLimit is not null)
        {
            throw lastRateLimit;
        }

        throw new InvalidOperationException(
            "The itinerary agent model did not return a decision.");
    }

    private static Task ReportAsync(
        Func<Guid?, int, string, CancellationToken, Task>? reporter,
        Guid? generationJobId,
        int progress,
        string stage,
        CancellationToken cancellationToken)
    {
        return reporter is null
            ? Task.CompletedTask
            : reporter(
                generationJobId,
                progress,
                stage,
                cancellationToken);
    }
}
