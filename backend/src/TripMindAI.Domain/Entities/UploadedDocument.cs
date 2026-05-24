using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class UploadedDocument
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? VisaApplicationId { get; set; }
    public VisaApplication? VisaApplication { get; set; }
    public DocumentType DocumentType { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public UploadStatus UploadStatus { get; set; }
    public DocumentAnalysisStatus AnalysisStatus { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DocumentAnalysisResult? AnalysisResult { get; set; }
}
