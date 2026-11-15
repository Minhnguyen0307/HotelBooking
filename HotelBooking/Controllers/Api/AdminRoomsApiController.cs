using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/admin/rooms")]
public class AdminRoomsApiController : ControllerBase
{
    private readonly IRoomManageService _roomManageService;
    private readonly IRoomService _roomService;
    private readonly IWebHostEnvironment _env;

    public AdminRoomsApiController(
        IRoomManageService roomManageService,
        IRoomService roomService,
        IWebHostEnvironment env)
    {
        _roomManageService = roomManageService;
        _roomService = roomService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return Ok(await _roomManageService.GetAllRoomsAsync());
    }

    [HttpGet("types")]
    public async Task<IActionResult> Types()
    {
        var types = await _roomService.GetAllRoomTypesAsync();
        return Ok(types.Select(t => new { roomTypeId = t.RoomTypeId, typeName = t.TypeName }));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var room = await _roomManageService.GetRoomForEditAsync(id);
        if (room == null) return NotFound(new { message = "Không tìm thấy phòng." });
        return Ok(new
        {
            room.RoomId,
            room.RoomNumber,
            room.RoomTypeId,
            room.Floor,
            room.Status,
            room.Description,
            room.Address,
            room.PropertyId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] RoomUpsertDto dto)
    {
        dto.RoomId = null;
        return await Save(dto, "Đã thêm phòng mới.");
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromForm] RoomUpsertDto dto)
    {
        dto.RoomId = id;
        return await Save(dto, "Đã cập nhật phòng.");
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateStatusRequest request)
    {
        var (success, error) = await _roomManageService.UpdateStatusAsync(id, request.Status);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã cập nhật trạng thái." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _roomManageService.DeleteRoomAsync(id);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã xóa phòng." });
    }

    private async Task<IActionResult> Save(RoomUpsertDto dto, string okMessage)
    {
        string? imagePath = null;
        if (dto.ImageFile is { Length: > 0 })
            imagePath = await SaveRoomImageAsync(dto.ImageFile);

        var (success, error) = await _roomManageService.SaveRoomAsync(dto, imagePath);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = okMessage });
    }

    private async Task<string> SaveRoomImageAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "rooms");
        Directory.CreateDirectory(uploadsFolder);
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/images/rooms/{uniqueFileName}";
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
