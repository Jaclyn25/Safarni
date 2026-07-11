using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Auth.RefreshTokens;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshToken is null)
        {
            return Result<RefreshTokenResponse>.Failure(AuthenticationErrors.InvalidRefreshToken);
        }

        if (refreshToken.IsRevoked)
        {
            await _refreshTokenRepository.RevokeAllByUserIdAsync(refreshToken.UserId, cancellationToken);
            return Result<RefreshTokenResponse>.Failure(AuthenticationErrors.InvalidRefreshToken);
        }

        if (refreshToken.ExpiryDate < DateTime.UtcNow)
        {
            return Result<RefreshTokenResponse>.Failure(AuthenticationErrors.InvalidRefreshToken);
        }

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result<RefreshTokenResponse>.Failure(AuthenticationErrors.InvalidRefreshToken);
        }

        refreshToken.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiryDate = _tokenService.GetRefreshTokenExpiryDate(),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(
            accessToken.Token,
            newRefreshTokenValue,
            accessToken.ExpiresIn));
    }
}
