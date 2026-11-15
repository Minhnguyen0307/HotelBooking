using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Route("api/rooms")]
public class RoomsApiController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly IReviewService _reviewService;
    private readonly IWebHostEnvironment _env;

    public RoomsApiController(IRoomService roomService, IReviewService reviewService, IWebHostEnvironment env)
    {
        _roomService = roomService;
        _reviewService = reviewService;
        _env = env;
    }

    [HttpGet("types")]
    public async Task<IActionResult> Types()
    {
        var types = await _roomService.GetAllRoomTypesAsync();
        return Ok(types.Select(t => new { roomTypeId = t.RoomTypeId, typeName = t.TypeName }));
    }

    [HttpGet("featured")]
    public async Task<IActionResult> Featured()
    {
        return Ok(await _roomService.GetShowcaseRoomsAsync());
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog([FromQuery] RoomSearchDto criteria)
    {
        if (criteria.CheckInDate == default)
            criteria.CheckInDate = DateOnly.FromDateTime(DateTime.Today);
        if (criteria.CheckOutDate == default)
            criteria.CheckOutDate = criteria.CheckInDate.AddDays(1);

        try
        {
            return Ok(await _roomService.GetPropertyRoomsAsync(criteria));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("properties")]
    public async Task<IActionResult> Properties()
    {
        return Ok(await _roomService.GetPropertiesAsync());
    }

    [HttpGet("add-ons")]
    public IActionResult AddOns()
    {
        return Ok(BookingAddOnPricing.GetOptions());
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] RoomSearchDto criteria)
    {
        if (criteria.CheckInDate == default)
            criteria.CheckInDate = DateOnly.FromDateTime(DateTime.Today);
        if (criteria.CheckOutDate == default)
            criteria.CheckOutDate = criteria.CheckInDate.AddDays(1);

        try
        {
            var rooms = await _roomService.SearchAvailableRoomsAsync(criteria);
            return Ok(rooms);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var room = await _roomService.GetRoomDetailAsync(id);
        if (room == null) return NotFound(new { message = "Không tìm thấy phòng." });

        room.ReviewsSummary = await _reviewService.GetRoomReviewsSummaryAsync(id);
        var authenticated = User.Identity?.IsAuthenticated == true;
        var canReview = false;
        if (authenticated)
            canReview = await _reviewService.CanUserReviewAsync(ApiUser.Id(User), id);

        return Ok(new
        {
            room,
            canReview,
            hasStayed = authenticated && canReview
        });
    }

    [Authorize]
    [HttpPost("{id:int}/reviews")]
    public async Task<IActionResult> AddReview(int id, [FromForm] CreateReviewDto dto)
    {
        dto.RoomId = id;
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest(new { message = "Vui lòng chọn số sao từ 1 đến 5." });

        var (success, error) = await _reviewService.AddReviewAsync(ApiUser.Id(User), dto, _env.WebRootPath);
        if (!success)
            return BadRequest(new { message = error ?? "Không thể gửi đánh giá." });

        return Ok(new { message = "Cảm ơn bạn đã gửi đánh giá." });
    }

    [Authorize(Roles = "Receptionist,Manager,Admin")]
    [HttpPost("reviews/{reviewId:int}/reply")]
    public async Task<IActionResult> ReplyReview(int reviewId, ReplyReviewDto dto)
    {
        var (success, error) = await _reviewService.ReplyReviewAsync(ApiUser.Id(User), reviewId, dto.Response);
        if (!success)
            return BadRequest(new { message = error ?? "Không thể gửi phản hồi." });
        return Ok(new { message = "Đã gửi phản hồi đánh giá." });
    }
}
