using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly HotelBookingDbContext _context;

    public HealthController(HotelBookingDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var connected = await _context.Database.CanConnectAsync();
        if (!connected)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { database = "failed" });

        return Ok(new
        {
            database = "connected",
            databaseName = _context.Database.GetDbConnection().Database,
            users = await _context.Users.CountAsync(),
            rooms = await _context.Rooms.CountAsync(),
            roomTypes = await _context.RoomTypes.CountAsync(),
            bookings = await _context.Bookings.CountAsync()
        });
    }
}
