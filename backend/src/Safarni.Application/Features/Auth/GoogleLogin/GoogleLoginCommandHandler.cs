using Google.Apis.Auth;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Auth.Login;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Auth.GoogleLogin;

public sealed class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public GoogleLoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
        }
        catch
        {
            return Result<LoginResponse>.Failure(new Error("Auth.InvalidGoogleToken", "Invalid Google token.", ErrorType.Unauthorized));
        }

        var email = payload.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<LoginResponse>.Failure(new Error("Auth.InvalidGoogleToken", "Google token does not contain email.", ErrorType.Unauthorized));
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                FullName = payload.Name ?? "Google User",
                Email = email,
                PasswordHash = _passwordHasher.Hash(Guid.NewGuid().ToString("N")),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsEmailVerified = true,
                Provider = "Google",
                ProviderUserId = payload.Subject
            };

            await _userRepository.AddAsync(user, cancellationToken);
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiryDate = _tokenService.GetRefreshTokenExpiryDate(),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken.Token,
            refreshTokenValue,
            accessToken.ExpiresIn,
            user.Id,
            user.FullName,
            user.Email));
    }
}