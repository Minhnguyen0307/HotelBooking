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
    private readonly IDeskService _deskService;
    private readonly IReviewService _reviewService;

    public FrontDeskApiController(IFrontDeskService frontDeskService, IDeskService deskService, IReviewService reviewService)
    {
        _frontDeskService = frontDeskService;
        _deskService = deskService;
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
            reviews = await _reviewService.GetAllReviewsAsync(),
            rooms = await _deskService.GetRoomsAsync(),
            services = await _deskService.GetServicesAsync(),
            issues = await _deskService.OpenIssuesAsync()
        });
    }

    [HttpGet("availability")]
    public async Task<IActionResult> Availability([FromQuery] DateOnly checkIn, [FromQuery] DateOnly checkOut, [FromQuery] int? ignoreBookingId)
    {
        if (checkOut <= checkIn) return BadRequest(new { message = "Ngày trả phòng phải sau ngày nhận phòng." });
        return Ok(await _deskService.AvailableRoomsAsync(checkIn, checkOut, ignoreBookingId));
    }

    [HttpPost("bookings")]
    public async Task<IActionResult> WalkIn(WalkInDto dto)
    {
        var (success, error, bookingId) = await _deskService.CreateWalkInAsync(dto);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã tạo đặt phòng.", bookingId });
    }

    [HttpGet("bookings/{bookingId:int}/folio")]
    public async Task<IActionResult> Folio(int bookingId)
    {
        var folio = await _deskService.GetFolioAsync(bookingId);
        return folio == null ? NotFound(new { message = "Không tìm thấy đơn." }) : Ok(folio);
    }

    [HttpPut("bookings/{bookingId:int}")]
    public async Task<IActionResult> UpdateStay(int bookingId, StayUpdateDto dto)
    {
        var (success, error) = await _deskService.UpdateStayAsync(bookingId, dto);
        return Result(success, error, "Đã cập nhật đặt phòng.");
    }

    [HttpPost("bookings/{bookingId:int}/change-room")]
    public async Task<IActionResult> ChangeRoom(int bookingId, ChangeRoomDto dto)
    {
        var (success, error) = await _deskService.ChangeRoomAsync(bookingId, dto.RoomId);
        return Result(success, error, "Đã đổi phòng.");
    }

    [HttpPost("bookings/{bookingId:int}/charges")]
    public async Task<IActionResult> Charge(int bookingId, ChargeDto dto)
    {
        var (success, error) = await _deskService.AddChargeAsync(bookingId, dto);
        return Result(success, error, "Đã thêm dịch vụ vào hóa đơn.");
    }

    [HttpPost("bookings/{bookingId:int}/deposit")]
    public async Task<IActionResult> Deposit(int bookingId, DepositDto dto)
    {
        var (success, error) = await _deskService.AddDepositAsync(bookingId, dto);
        return Result(success, error, "Đã ghi nhận tiền cọc.");
    }

    [HttpGet("customers")]
    public async Task<IActionResult> Customers([FromQuery] string? q)
    {
        return Ok(await _deskService.SearchGuestsAsync(q));
    }

    [HttpPut("customers/{userId:int}")]
    public async Task<IActionResult> UpdateCustomer(int userId, GuestUpdateDto dto)
    {
        var (success, error) = await _deskService.UpdateGuestAsync(userId, dto.FullName, dto.PhoneNumber, dto.Email);
        return Result(success, error, "Đã cập nhật khách.");
    }

    [HttpPost("rooms/{roomId:int}/housekeeping")]
    public async Task<IActionResult> Housekeeping(int roomId, HousekeepingDto dto)
    {
        var (success, error) = await _deskService.SetHousekeepingAsync(roomId, dto.Status);
        return Result(success, error, "Đã cập nhật tình trạng phòng.");
    }

    [HttpPost("rooms/{roomId:int}/issues")]
    public async Task<IActionResult> ReportIssue(int roomId, IssueDto dto)
    {
        var (success, error) = await _deskService.ReportIssueAsync(roomId, dto.Description);
        return Result(success, error, "Đã báo sự cố phòng.");
    }

    [HttpPost("issues/{issueId:int}/resolve")]
    public async Task<IActionResult> ResolveIssue(int issueId)
    {
        var (success, error) = await _deskService.ResolveIssueAsync(issueId);
        return Result(success, error, "Đã xử lý sự cố.");
    }

    [HttpPost("check-in/{bookingId:int}")]
    public async Task<IActionResult> CheckIn(int bookingId, CheckInDto dto)
    {
        var (success, error) = await _deskService.CheckInConfirmAsync(bookingId, dto);
        return Result(success, error, "Đã nhận phòng.");
    }

    [HttpPost("check-out/{bookingId:int}")]
    public async Task<IActionResult> CheckOut(int bookingId, CheckoutDto dto)
    {
        var (success, error) = await _deskService.CheckOutWithBillAsync(bookingId, dto, ApiUser.Id(User));
        return Result(success, error, "Đã trả phòng và chốt hóa đơn.");
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

    private IActionResult Result(bool success, string? error, string okMessage)
    {
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = okMessage });
    }
}
