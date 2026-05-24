namespace TripMindAI.Domain.Enums;

public enum VisaApplicationStatus
{
    Draft,
    RequirementsLoaded,
    DocumentsUploaded,
    Analyzing,
    Analyzed,
    NeedsFixes,
    AlmostReady,
    Ready,
    HighRisk,
    Cancelled
}
