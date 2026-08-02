namespace Safarni.Application.Features.Admin.Common;

public static class AdminAuditActions
{
    public const string UserDeactivated = "User.Deactivated";
    public const string UserActivated = "User.Activated";
    public const string UserEmailVerifiedByAdmin = "User.EmailVerifiedByAdmin";
    public const string UserPasswordResetForced = "User.PasswordResetForced";
    public const string UserRoleChanged = "User.RoleChanged";
    public const string KnowledgeCreated = "Knowledge.Created";
    public const string KnowledgeUpdated = "Knowledge.Updated";
    public const string KnowledgeArchived = "Knowledge.Archived";
    public const string KnowledgeRestored = "Knowledge.Restored";
    public const string KnowledgeReindexed = "Knowledge.Reindexed";
    public const string IntegrationTested = "Integration.Tested";
    public const string AIConversationViewedByAdmin = "AIConversation.ViewedByAdmin";
}
