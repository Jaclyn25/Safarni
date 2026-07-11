namespace Safarni.Application.Common.Errors;

public static class UserErrors
{
    public static readonly Error EmailAlreadyExists = new(
        "User.EmailAlreadyExists",
        "A user with the same email already exists.",
        ErrorType.Conflict);
}