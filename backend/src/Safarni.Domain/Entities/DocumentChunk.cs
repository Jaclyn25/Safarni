using System;

namespace ChatBotApi.Domain.Entities;

public class DocumentChunk
{
    public string Id { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public float Score { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
