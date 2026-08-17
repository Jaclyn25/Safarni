namespace Safarni.Domain.Entities;

public sealed class CommunityPostComment
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid AuthorUserId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsAcceptedAnswer { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public CommunityPost Post { get; set; } = null!;
    public User Author { get; set; } = null!;
    public CommunityPostComment? ParentComment { get; set; }
    public ICollection<CommunityPostComment> Replies { get; set; } = new List<CommunityPostComment>();
}
