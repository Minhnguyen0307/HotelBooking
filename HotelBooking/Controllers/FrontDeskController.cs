using System.Security.Claims;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Receptionist,Manager,Admin")]
public class FrontDeskController : Controller
{
    private readonly IFrontDeskService _frontDeskService;
    private readonly IReviewService _reviewService;

    public FrontDeskController(IFrontDeskService frontDeskService, IReviewService reviewService)
    {
        _frontDeskService = frontDeskService;
        _reviewService = reviewService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Arrivals = await _frontDeskService.GetTodayArrivalsAsync();
        ViewBag.Departures = await _frontDeskService.GetTodayDeparturesAsync();
        ViewBag.CancelRequests = await _frontDeskService.GetCancelRequestsAsync();
        ViewBag.AllCheckedIn = await _frontDeskService.GetAllCheckedInAsync();
        ViewBag.Reviews = await _reviewService.GetAllReviewsAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(int bookingId)
    {
        var (success, error) = await _frontDeskService.CheckInAsync(bookingId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Check-in thành công." : error;
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> CheckOut(int bookingId)
    {
        var (success, error) = await _frontDeskService.CheckOutAsync(bookingId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Check-out thành công." : error;
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmCancel(int bookingId)
    {
        var (success, error) = await _frontDeskService.ConfirmCancelAsync(bookingId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Xác nhận hủy đơn đặt phòng thành công." : error;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyReview(int reviewId, string response)
    {
        var staffUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _reviewService.ReplyReviewAsync(staffUserId, reviewId, response);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Đã gửi phản hồi đánh giá của khách thành công." : error;
        return RedirectToAction("Index");
    }
}