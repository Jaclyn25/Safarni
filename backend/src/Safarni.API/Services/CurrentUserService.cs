using System.Security.Claims;
using Safarni.Application.Abstractions.Authentication;

namespace Safarni.API.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        }
    }

    public string SessionId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return string.Empty;

            var cookieName = "SafarniViewerId";
            if (context.Request.Cookies.TryGetValue(cookieName, out var sessionId) && !string.IsNullOrWhiteSpace(sessionId))
            {
                return sessionId;
            }

            // Generate and set if not exists
            sessionId = Guid.NewGuid().ToString("N");
            context.Response.Cookies.Append(cookieName, sessionId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });

            return sessionId;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
