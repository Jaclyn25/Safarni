using Google.Apis.Auth;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Auth.Login;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Auth.GoogleLogin;

public sealed class GoogleLoginCommandHandler
    : IRequestHandler<GoogleLoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly GoogleAuthOptions _googleAuthOptions;

    public GoogleLoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        GoogleAuthOptions googleAuthOptions)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _googleAuthOptions = googleAuthOptions;
    }

    public async Task<Result<LoginResponse>> Handle(
        GoogleLoginCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return InvalidGoogleToken(
                "Google did not provide a valid authentication token.");
        }

        if (string.IsNullOrWhiteSpace(_googleAuthOptions.ClientId))
        {
            return Result<LoginResponse>.Failure(
                new Error(
                    "Auth.GoogleConfigurationMissing",
                    "Google authentication is not configured correctly.",
                    ErrorType.Internal));
        }

        GoogleJsonWebSignature.Payload payload;

        try
        {
            var validationSettings =
                new GoogleJsonWebSignature.ValidationSettings
                {
                    /*
                     * The token must have been issued specifically
                     * for Safarni's Google OAuth client.
                     */
                    Audience =
                    [
                        _googleAuthOptions.ClientId
                    ]
                };

            payload =
                await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    validationSettings);
        }
        catch (InvalidJwtException)
        {
            return InvalidGoogleToken(
                "The Google authentication token is invalid or expired.");
        }

        /*
         * Google should confirm that it has verified
         * the email associated with the account.
         */
        if (payload.EmailVerified != true)
        {
            return InvalidGoogleToken(
                "The Google account email address has not been verified.");
        }

        var googleUserId = payload.Subject?.Trim();

        if (string.IsNullOrWhiteSpace(googleUserId))
        {
            return InvalidGoogleToken(
                "The Google token does not contain a valid user identifier.");
        }

        var email = payload.Email?
            .Trim()
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
        {
            return InvalidGoogleToken(
                "The Google token does not contain an email address.");
        }

        var now = DateTime.UtcNow;

        var user =
            await _userRepository.GetByEmailAsync(
                email,
                cancellationToken);

        if (user is null)
        {
            /*
             * First Google login:
             * create a Safarni user automatically.
             */
            user = new User
            {
                Id = Guid.NewGuid(),

                FullName = string.IsNullOrWhiteSpace(payload.Name)
                    ? "Google User"
                    : payload.Name.Trim(),

                Email = email,

                /*
                 * Google users do not authenticate with this password.
                 * A random value is still stored because PasswordHash
                 * is required by the current User entity.
                 */
                PasswordHash = _passwordHasher.Hash(
                    Guid.NewGuid().ToString("N")),

                CreatedAt = now,
                UpdatedAt = now,

                IsActive = true,
                IsEmailVerified = true,

                Provider = "Google",
                ProviderUserId = googleUserId
            };

            await _userRepository.AddAsync(
                user,
                cancellationToken);
        }
        else
        {
            /*
             * Do not let Google login bypass an account
             * that was disabled by the application.
             */
            if (!user.IsActive)
            {
                return Result<LoginResponse>.Failure(
                    new Error(
                        "Auth.InactiveAccount",
                        "This account is currently inactive.",
                        ErrorType.Forbidden));
            }

            /*
             * If this user was previously connected to Google,
             * the stable Google subject identifier must match.
             */
            if (
                string.Equals(
                    user.Provider,
                    "Google",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(user.ProviderUserId) &&
                !string.Equals(
                    user.ProviderUserId,
                    googleUserId,
                    StringComparison.Ordinal))
            {
                return InvalidGoogleToken(
                    "This Google account does not match the connected Safarni account.");
            }

            var userNeedsUpdate = false;

            /*
             * Connect an existing email/password account
             * to Google on its first successful Google login.
             *
             * The existing password hash is preserved, so the
             * user can continue using either login method.
             */
            if (string.IsNullOrWhiteSpace(user.Provider))
            {
                user.Provider = "Google";
                user.ProviderUserId = googleUserId;
                userNeedsUpdate = true;
            }
            else if (
                string.Equals(
                    user.Provider,
                    "Google",
                    StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(user.ProviderUserId))
            {
                user.ProviderUserId = googleUserId;
                userNeedsUpdate = true;
            }

            if (!user.IsEmailVerified)
            {
                user.IsEmailVerified = true;
                userNeedsUpdate = true;
            }

            if (userNeedsUpdate)
            {
                user.UpdatedAt = now;

                await _userRepository.UpdateAsync(
                    user,
                    cancellationToken);
            }
        }

        var accessToken =
            _tokenService.GenerateAccessToken(user);

        var refreshTokenValue =
            _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,

            ExpiryDate =
                _tokenService.GetRefreshTokenExpiryDate(),

            CreatedAt = now,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(
            refreshToken,
            cancellationToken);

        return Result<LoginResponse>.Success(
            new LoginResponse(
                accessToken.Token,
                refreshTokenValue,
                accessToken.ExpiresIn,
                user.Id,
                user.FullName,
                user.Email));
    }

    private static Result<LoginResponse> InvalidGoogleToken(
        string message)
    {
        return Result<LoginResponse>.Failure(
            new Error(
                "Auth.InvalidGoogleToken",
                message,
                ErrorType.Unauthorized));
    }
}