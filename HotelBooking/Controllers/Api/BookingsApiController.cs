using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/bookings")]
public class BookingsApiController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsApiController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("promos")]
    public async Task<IActionResult> AvailablePromos()
    {
        var promos = await _bookingService.GetAvailablePromoCodesAsync(ApiUser.Id(User));
        return Ok(promos);
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] BookingCreateDto dto)
    {
        var preview = await _bookingService.GetBookingPreviewAsync(ApiUser.Id(User), dto);
        if (!preview.Success || preview.Preview == null)
            return BadRequest(new { message = preview.ErrorMessage ?? "Không tạo được xem trước đơn đặt phòng." });
        return Ok(preview.Preview);
    }

    [HttpPost("preview")]
    public async Task<IActionResult> PreviewPost([FromBody] BookingCreateDto dto)
    {
        var preview = await _bookingService.GetBookingPreviewAsync(ApiUser.Id(User), dto);
        if (!preview.Success || preview.Preview == null)
            return BadRequest(new { message = preview.ErrorMessage ?? "Không tạo được xem trước đơn đặt phòng." });
        return Ok(preview.Preview);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BookingCreateDto dto)
    {
        var result = await _bookingService.CreateBookingAsync(ApiUser.Id(User), dto);
        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new
        {
            message = result.RequiresPayment
                ? "Đơn đã được giữ. Mời bạn thanh toán ngay."
                : "Đã giữ phòng. Quý khách thanh toán tiền mặt khi nhận phòng.",
            bookingId = result.BookingId,
            requiresPayment = result.RequiresPayment
        });
    }

    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var bookings = await _bookingService.GetCustomerBookingsAsync(ApiUser.Id(User));
        return Ok(bookings);
    }

    [HttpPost("{bookingId:int}/cancel")]
    public async Task<IActionResult> Cancel(int bookingId, [FromBody] CancelBookingRequest? request)
    {
        var result = await _bookingService.CancelBookingAsync(ApiUser.Id(User), bookingId, request?.Reason);
        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = result.Message, refundAmount = result.RefundAmount });
    }

    public class CancelBookingRequest
    {
        public string? Reason { get; set; }
    }
}
