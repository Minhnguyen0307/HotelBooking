using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Create(int roomId, DateOnly? checkIn, DateOnly? checkOut)
    {
        var dto = new BookingCreateDto
        {
            RoomId = roomId,
            CheckInDate = checkIn ?? DateOnly.FromDateTime(DateTime.Today),
            CheckOutDate = checkOut ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };

        var preview = await _bookingService.GetBookingPreviewAsync(dto);
        if (preview == null) return NotFound();

        return View(preview);
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(BookingCreateDto dto)
    {
        var result = await _bookingService.CreateBookingAsync(CurrentUserId, dto);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction("Create", new { roomId = dto.RoomId, checkIn = dto.CheckInDate, checkOut = dto.CheckOutDate });
        }

        TempData["SuccessMessage"] = "Đặt phòng thành công! Vui lòng chờ xác nhận.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Index()
    {
        var bookings = await _bookingService.GetCustomerBookingsAsync(CurrentUserId);
        return View(bookings);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int bookingId, string? reason)
    {
        var result = await _bookingService.CancelBookingAsync(CurrentUserId, bookingId, reason);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] =
            result.Success ? "Đã hủy đặt phòng." : result.ErrorMessage;

        return RedirectToAction("Index");
    }
}