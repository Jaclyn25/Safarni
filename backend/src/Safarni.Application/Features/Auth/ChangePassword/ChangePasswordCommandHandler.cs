using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.ChangePassword;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<ChangePasswordResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<ChangePasswordResponse>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            return Result<ChangePasswordResponse>.Failure(new Error("Auth.PasswordsDoNotMatch", "Passwords do not match.", ErrorType.Validation));
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            return Result<ChangePasswordResponse>.Failure(new Error("Auth.UserNotFound", "User not found.", ErrorType.NotFound));
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result<ChangePasswordResponse>.Failure(AuthenticationErrors.InvalidCredentials);
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<ChangePasswordResponse>.Success(new ChangePasswordResponse("Password changed successfully."));
    }
}