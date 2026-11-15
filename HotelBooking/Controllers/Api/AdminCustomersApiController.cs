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
    public async Task<IActionResult> Index([FromQuery] string? role)
    {
        var users = await _customerService.GetAllUsersAsync();

        if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, "all", StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(u => string.Equals(u.Role?.RoleName, role, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Ok(users.Select(u => new
        {
            u.UserId,
            u.FullName,
            u.Email,
            u.PhoneNumber,
            u.RoleId,
            RoleName = u.Role?.RoleName ?? "Customer",
            u.IsActive,
            u.CreatedAt
        }));
    }

    [HttpGet("roles")]
    public async Task<IActionResult> Roles()
    {
        var roles = await _customerService.GetAllRolesAsync();
        return Ok(roles.Select(r => new { r.RoleId, r.RoleName }));
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest request)
    {
        var (success, error) = await _customerService.ChangeUserRoleAsync(id, request.RoleId);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã cập nhật vai trò người dùng thành công." });
    }

    [HttpPost("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var (success, error) = await _customerService.ToggleCustomerActiveStatusAsync(id);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã cập nhật trạng thái người dùng." });
    }

    public class ChangeRoleRequest
    {
        public int RoleId { get; set; }
    }
}
