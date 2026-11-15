using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/admin/room-types")]
public class AdminRoomTypesApiController : ControllerBase
{
    private readonly IRoomTypeService _roomTypeService;

    public AdminRoomTypesApiController(IRoomTypeService roomTypeService)
    {
        _roomTypeService = roomTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var types = await _roomTypeService.GetAllRoomTypesAsync();
        return Ok(types.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var roomType = await _roomTypeService.GetRoomTypeByIdAsync(id);
        if (roomType == null) return NotFound(new { message = "Không tìm thấy loại phòng." });
        return Ok(Map(roomType));
    }

    [HttpPost]
    public async Task<IActionResult> Create(RoomType roomType)
    {
        roomType.RoomTypeId = 0;
        var (success, error) = await _roomTypeService.SaveRoomTypeAsync(roomType);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã thêm loại phòng." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RoomType roomType)
    {
        roomType.RoomTypeId = id;
        var (success, error) = await _roomTypeService.SaveRoomTypeAsync(roomType);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã cập nhật loại phòng." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _roomTypeService.DeleteRoomTypeAsync(id);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã xóa loại phòng." });
    }

    private static object Map(RoomType roomType) => new
    {
        roomType.RoomTypeId,
        roomType.TypeName,
        roomType.Description,
        roomType.BasePrice,
        roomType.MaxGuests,
        roomType.IsActive
    };
}
