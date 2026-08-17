using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class CommunityPost
{
    public Guid Id { get; set; }
    public Guid AuthorUserId { get; set; }
    public CommunityPostType Type { get; set; }
    public CommunityPostStatus Status { get; set; } = CommunityPostStatus.Published;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public Guid? LinkedTripId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int LikesCount { get; set; }
    public int SavesCount { get; set; }
    public int CommentsCount { get; set; }

    public User Author { get; set; } = null!;
    public Trip? LinkedTrip { get; set; }
    public ICollection<CommunityPostMedia> Media { get; set; } = new List<CommunityPostMedia>();
    public ICollection<CommunityPostComment> Comments { get; set; } = new List<CommunityPostComment>();
    public ICollection<CommunityPostInteraction> Interactions { get; set; } = new List<CommunityPostInteraction>();
}
