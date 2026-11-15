using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/admin/customers")]
public class AdminCustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public AdminCustomersApiController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customers = await _customerService.GetAllCustomersAsync();
        return Ok(customers.Select(u => new
        {
            u.UserId,
            u.FullName,
            u.Email,
            u.PhoneNumber,
            u.IsActive,
            u.CreatedAt
        }));
    }

    [HttpPost("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var (success, error) = await _customerService.ToggleCustomerActiveStatusAsync(id);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã cập nhật trạng thái khách hàng." });
    }
}
