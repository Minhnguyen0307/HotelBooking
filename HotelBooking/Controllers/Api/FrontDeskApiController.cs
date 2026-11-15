using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize(Roles = "Receptionist,Manager,Admin")]
[Route("api/frontdesk")]
public class FrontDeskApiController : ControllerBase
{
    private readonly IFrontDeskService _frontDeskService;
    private readonly IReviewService _reviewService;

    public FrontDeskApiController(IFrontDeskService frontDeskService, IReviewService reviewService)
    {
        _frontDeskService = frontDeskService;
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return Ok(new
        {
            arrivals = await _frontDeskService.GetTodayArrivalsAsync(),
            departures = await _frontDeskService.GetTodayDeparturesAsync(),
            cancelRequests = await _frontDeskService.GetCancelRequestsAsync(),
            checkedIn = await _frontDeskService.GetAllCheckedInAsync(),
            reviews = await _reviewService.GetAllReviewsAsync()
        });
    }

    [HttpPost("check-in/{bookingId:int}")]
    public async Task<IActionResult> CheckIn(int bookingId)
    {
        var (success, error) = await _frontDeskService.CheckInAsync(bookingId);
        return Result(success, error, "Check-in thành công.");
    }

    [HttpPost("check-out/{bookingId:int}")]
    public async Task<IActionResult> CheckOut(int bookingId)
    {
        var (success, error) = await _frontDeskService.CheckOutAsync(bookingId);
        return Result(success, error, "Check-out thành công.");
    }

    [HttpPost("confirm-cancel/{bookingId:int}")]
    public async Task<IActionResult> ConfirmCancel(int bookingId)
    {
        var (success, error) = await _frontDeskService.ConfirmCancelAsync(bookingId);
        return Result(success, error, "Đã xác nhận hủy đơn.");
    }

    [HttpPost("reviews/{reviewId:int}/reply")]
    public async Task<IActionResult> Reply(int reviewId, ReplyReviewDto dto)
    {
        var (success, error) = await _reviewService.ReplyReviewAsync(ApiUser.Id(User), reviewId, dto.Response);
        return Result(success, error, "Đã gửi phản hồi đánh giá.");
    }

    // ── New Endpoints ──────────────────────────────────────────────────────────

    [HttpGet("room-matrix")]
    public async Task<IActionResult> GetRoomMatrix()
    {
        var matrix = await _frontDeskService.GetRoomMatrixAsync();
        return Ok(matrix);
    }

    [HttpPost("rooms/{roomId:int}/clean")]
    public async Task<IActionResult> MarkRoomClean(int roomId)
    {
        var (success, error) = await _frontDeskService.MarkRoomCleanAsync(roomId);
        return Result(success, error, "Phòng đã được đánh dấu sạch.");
    }

    [HttpPost("walk-in")]
    public async Task<IActionResult> CreateWalkIn([FromBody] WalkInBookingDto dto)
    {
        var (success, error, bookingId) = await _frontDeskService.CreateWalkInBookingAsync(dto);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Walk-in check-in thành công.", bookingId });
    }

    [HttpPost("bookings/{id:int}/transfer-room")]
    public async Task<IActionResult> TransferRoom(int id, [FromBody] TransferRoomDto dto)
    {
        var (success, error) = await _frontDeskService.TransferRoomAsync(id, dto.NewRoomId);
        return Result(success, error, "Chuyển phòng thành công.");
    }

    [HttpPost("bookings/{id:int}/extend-stay")]
    public async Task<IActionResult> ExtendStay(int id, [FromBody] ExtendStayDto dto)
    {
        var (success, error) = await _frontDeskService.ExtendStayAsync(id, dto.NewCheckOutDate);
        return Result(success, error, "Gia hạn lưu trú thành công.");
    }

    [HttpGet("bookings/{id:int}/invoice")]
    public async Task<IActionResult> GetInvoice(int id)
    {
        var invoice = await _frontDeskService.GetInvoiceAsync(id);
        if (invoice == null) return NotFound(new { message = "Không tìm thấy đơn đặt phòng." });
        return Ok(invoice);
    }

    private IActionResult Result(bool success, string? error, string okMessage)
    {
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = okMessage });
    }
}
