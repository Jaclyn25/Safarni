namespace Safarni.Application.Common.Errors;

public static class UserErrors
{
    public static readonly Error EmailAlreadyExists = new(
        "User.EmailAlreadyExists",
        "A user with the same email already exists.",
        ErrorType.Conflict);

    public static readonly Error NotFound = new(
       "User.NotFound",
       "The user was not found.",
       ErrorType.NotFound);


    public static readonly Error InvalidProfilePhoto = new(
       "User.InvalidProfilePhoto",
       "The profile photo is invalid. Allowed types are JPG, PNG, and WEBP with a maximum size of 5 MB.",
       ErrorType.Validation);
}