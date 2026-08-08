using Safarni.Application.Common.Errors;

namespace Safarni.Application.Features.Admin.Common;

public static class AdminErrors
{
    public static readonly Error UserNotFound =
        new("Admin.UserNotFound", "The requested user does not exist.", ErrorType.NotFound);

    public static readonly Error SelfDeactivationNotAllowed =
        new("Admin.SelfDeactivationNotAllowed", "You cannot deactivate your own active account.", ErrorType.Conflict);

    public static readonly Error SelfRoleChangeNotAllowed =
        new("Admin.SelfRoleChangeNotAllowed", "You cannot change your own administrator role.", ErrorType.Conflict);

    public static readonly Error KnowledgeNotFound =
        new("Admin.KnowledgeNotFound", "The requested knowledge item does not exist.", ErrorType.NotFound);

    public static readonly Error KnowledgeValidationFailed =
        new("Admin.KnowledgeValidationFailed", "Knowledge items require a title and content.", ErrorType.Validation);

    public static readonly Error QdrantUnavailable =
        new("Admin.QdrantUnavailable", "Qdrant is not reachable, so a reindex cannot run.", ErrorType.Conflict);

    public static readonly Error ConversationNotFound =
        new("Admin.ConversationNotFound", "The requested conversation does not exist.", ErrorType.NotFound);

    public static readonly Error UnknownIntegration =
        new("Admin.UnknownIntegration", "The requested integration is not known.", ErrorType.NotFound);

    public static readonly Error InvalidRole =
        new("Admin.InvalidRole", "The requested role is not supported.", ErrorType.Validation);

    public static readonly Error AgencyRoleManagedByAgencyWorkflow =
        new(
            "Admin.AgencyRoleManagedByAgencyWorkflow",
            "Travel Agency roles are managed by the agency registration workflow.",
            ErrorType.Conflict);

    public static readonly Error InvalidDateRange =
        new("Admin.InvalidDateRange", "The from date must be earlier than the to date.", ErrorType.Validation);
}
