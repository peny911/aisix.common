using System.Security.Claims;

namespace Aisix.Common
{
    public static class ClaimsPrincipalUtil
    {
        public static long GetUserId(this ClaimsPrincipal user)
        {
            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
                {
                    return userId;
                }
            }

            return 0;
        }
    }
}
