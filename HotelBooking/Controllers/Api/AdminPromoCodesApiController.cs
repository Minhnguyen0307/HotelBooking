using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/admin/promo-codes")]
public class AdminPromoCodesApiController : ControllerBase
{
    private readonly HotelBookingDbContext _context;

    public AdminPromoCodesApiController(HotelBookingDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var promos = await _context.PromoCodes
            .OrderByDescending(p => p.PromoCodeId)
            .Select(p => new
            {
                p.PromoCodeId,
                p.Code,
                p.DiscountAmount,
                p.DiscountPercent,
                p.IsActive
            })
            .ToListAsync();

        return Ok(promos);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromoDto dto)
    {
        var code = (dto.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code) || code.Length < 3 || code.Length > 20)
            return BadRequest(new { message = "Mã khuyến mãi phải từ 3 đến 20 ký tự." });

        if (await _context.PromoCodes.AnyAsync(p => p.Code == code))
            return BadRequest(new { message = $"Mã '{code}' đã tồn tại trong hệ thống." });

        if (dto.DiscountPercent <= 0 && dto.DiscountAmount <= 0)
            return BadRequest(new { message = "Vui lòng nhập phần trăm giảm (>0) hoặc số tiền giảm (>0)." });

        if (dto.DiscountPercent > 100)
            return BadRequest(new { message = "Phần trăm giảm tối đa là 100%." });
        if (dto.DiscountPercent > 0 && dto.DiscountAmount > 0)
            return BadRequest(new { message = "Chỉ nhập phần trăm hoặc số tiền, không nhập cả hai." });

        var promo = new PromoCode
        {
            Code = code,
            DiscountAmount = dto.DiscountPercent > 0 ? 0 : dto.DiscountAmount,
            DiscountPercent = dto.DiscountPercent > 0 ? dto.DiscountPercent : 0,
            IsActive = dto.IsActive
        };

        _context.PromoCodes.Add(promo);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã tạo mã khuyến mãi '{code}' thành công.", promoCodeId = promo.PromoCodeId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePromoDto dto)
    {
        var promo = await _context.PromoCodes.FindAsync(id);
        if (promo == null) return NotFound(new { message = "Không tìm thấy mã khuyến mãi." });
        if (dto.DiscountPercent <= 0 && dto.DiscountAmount <= 0)
            return BadRequest(new { message = "Vui lòng nhập phần trăm giảm hoặc số tiền giảm." });
        if (dto.DiscountPercent > 100)
            return BadRequest(new { message = "Phần trăm giảm tối đa là 100%." });
        if (dto.DiscountPercent > 0 && dto.DiscountAmount > 0)
            return BadRequest(new { message = "Chỉ nhập phần trăm hoặc số tiền, không nhập cả hai." });
        promo.DiscountAmount = dto.DiscountPercent > 0 ? 0 : dto.DiscountAmount;
        promo.DiscountPercent = dto.DiscountPercent > 0 ? dto.DiscountPercent : 0;
        promo.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật mã giảm giá." });
    }

    [HttpPost("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var promo = await _context.PromoCodes.FindAsync(id);
        if (promo == null) return NotFound(new { message = "Không tìm thấy mã khuyến mãi." });

        promo.IsActive = !promo.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { message = promo.IsActive ? "Đã kích hoạt mã khuyến mãi." : "Đã tạm dừng mã khuyến mãi.", isActive = promo.IsActive });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var promo = await _context.PromoCodes.FindAsync(id);
        if (promo == null) return NotFound(new { message = "Không tìm thấy mã khuyến mãi." });

        _context.PromoCodes.Remove(promo);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã xóa mã khuyến mãi thành công." });
    }

    public class CreatePromoDto
    {
        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public int DiscountPercent { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
