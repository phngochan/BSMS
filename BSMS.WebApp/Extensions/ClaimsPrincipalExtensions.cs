using System.Security.Claims;

namespace BSMS.WebApp.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                     ?? user.FindFirst("sub")
                     ?? user.FindFirst("userId");

            if (claim != null && int.TryParse(claim.Value, out int id))
                return id;

            throw new InvalidOperationException("User ID not found in claims.");
        }
    }
}
