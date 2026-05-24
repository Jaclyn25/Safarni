namespace TripMindAI.Domain.Entities;

public class DocumentAnalysisResult
{
    public Guid Id { get; set; }
    public Guid UploadedDocumentId { get; set; }
    public UploadedDocument UploadedDocument { get; set; } = null!;
    public string AiProvider { get; set; } = "Gemini";
    public string ModelName { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public bool IsReadable { get; set; }
    public bool HasGlare { get; set; }
    public bool IsBlurred { get; set; }
    public bool IsCropped { get; set; }
    public string ExtractedDataJson { get; set; } = "{}";
    public string QualityIssuesJson { get; set; } = "[]";
    public string RawAiResponseJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
