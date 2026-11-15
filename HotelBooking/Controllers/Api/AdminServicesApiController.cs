using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Route("api/admin/services")]
public class AdminServicesApiController : ControllerBase
{
    private readonly HotelBookingDbContext _context;

    public AdminServicesApiController(HotelBookingDbContext context)
    {
        _context = context;
    }

    [HttpGet("/api/services")]
    [AllowAnonymous]
    public async Task<IActionResult> PublicCatalog()
    {
        var rows = await _context.HotelServices.Where(s => s.ForBooking).OrderBy(s => s.HotelServiceId).ToListAsync();
        return Ok(rows.Select(Map));
    }

    [HttpGet]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Index()
    {
        var rows = await _context.HotelServices.OrderBy(s => s.HotelServiceId).ToListAsync();
        return Ok(rows.Select(Map));
    }

    [HttpPut("{code}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Update(string code, ServicePriceRequest request)
    {
        var row = await _context.HotelServices.FirstOrDefaultAsync(s => s.ServiceCode == code);
        if (row == null) return NotFound(new { message = "Không tìm thấy dịch vụ." });
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length < 2 || name.Length > 80) return BadRequest(new { message = "Tên dịch vụ từ 2 đến 80 ký tự." });
        if (request.Price < 0) return BadRequest(new { message = "Giá không được âm." });
        if (await _context.HotelServices.AnyAsync(s => s.Name == name && s.ServiceCode != code))
            return BadRequest(new { message = "Tên dịch vụ này đã được dùng." });
        row.Name = name;
        row.Price = request.Price;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật giá dịch vụ." });
    }

    private static object Map(HotelService row) => new
    {
        row.ServiceCode,
        row.Name,
        row.Price,
        row.UnitLabel,
        row.ForBooking,
        row.ForDesk
    };

    public class ServicePriceRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
