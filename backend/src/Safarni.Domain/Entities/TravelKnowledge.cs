using System;

namespace ChatBotApi.Domain.Entities;

public class TravelKnowledge
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public string? Country { get; set; }
    public string? City { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string MetadataJson { get; set; } = "{}";
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
