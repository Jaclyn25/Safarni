namespace Safarni.Domain.Entities;

public sealed class CommunityCommentMedia
{
    public Guid Id { get; set; }
    public Guid CommentId { get; set; }
    public string MediaType { get; set; } = "image";
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public int SortOrder { get; set; }

    public CommunityPostComment Comment { get; set; } = null!;
}
