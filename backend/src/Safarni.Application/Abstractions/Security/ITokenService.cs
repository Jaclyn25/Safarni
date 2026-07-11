using Safarni.Application.Common.Models;
using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Security;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user);

    string GenerateRefreshToken();

    DateTime GetRefreshTokenExpiryDate();
}