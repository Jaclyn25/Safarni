using System.Security.Claims;

namespace Safarni.API.Extensions
{
    public  static  class ClaimsPrincipalExtensions
    {
        public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(ClaimTypes.Name)
                ?? principal.FindFirstValue("sub");

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("The authenticated user id claim is missing or invalid.");
            }

            return userId;
        }
    }
}
