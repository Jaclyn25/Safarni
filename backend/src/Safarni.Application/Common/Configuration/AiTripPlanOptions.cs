namespace Safarni.Application.Common.Configuration;

public class AiTripPlanOptions
{
    public const string SectionName = "AiTripPlan";

    /// <summary>
    /// Timeout in seconds for the AI generation portion of the plan-trip endpoint.
    /// This prevents indefinite blocking if the LLM provider hangs.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 4;
}
