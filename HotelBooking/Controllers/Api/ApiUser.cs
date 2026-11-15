using System.Security.Claims;

namespace HotelBooking.Controllers.Api;

internal static class ApiUser
{
    public static int Id(ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
