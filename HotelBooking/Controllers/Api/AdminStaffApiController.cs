using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/admin/staff")]
public class AdminStaffApiController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public AdminStaffApiController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var staff = await _customerService.GetStaffAsync();
        return Ok(staff.Select(u => new
        {
            u.UserId,
            u.FullName,
            u.Email,
            u.PhoneNumber,
            RoleName = u.Role.RoleName,
            u.IsActive
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create(StaffWriteRequest request)
    {
        var (success, error) = await _customerService.CreateStaffAsync(
            request.FullName, request.Email, request.PhoneNumber, request.RoleName, request.Password);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã thêm nhân viên." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, StaffUpdateRequest request)
    {
        var (success, error) = await _customerService.UpdateStaffAsync(
            id, ApiUser.Id(User), request.FullName, request.PhoneNumber, request.RoleName, request.IsActive);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã cập nhật nhân viên." });
    }

    public class StaffWriteRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string RoleName { get; set; } = "Receptionist";
        public string Password { get; set; } = string.Empty;
    }

    public class StaffUpdateRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string RoleName { get; set; } = "Receptionist";
        public bool IsActive { get; set; } = true;
    }
}
