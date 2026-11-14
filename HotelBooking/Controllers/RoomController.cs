using System.Security.Claims;
using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

public class RoomController : Controller
{
    private readonly IRoomService _roomService;
    private readonly IReviewService _reviewService;
    private readonly IWebHostEnvironment _env;

    public RoomController(IRoomService roomService, IReviewService reviewService, IWebHostEnvironment env)
    {
        _roomService = roomService;
        _reviewService = reviewService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Search()
    {
        ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();
        return View(new RoomSearchDto());

    }

    [HttpPost]
    public async Task<IActionResult> Search(RoomSearchDto criteria)
    {
        ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();

        if (!ModelState.IsValid)
            return View(criteria);

        try
        {
            var rooms = await _roomService.SearchAvailableRoomsAsync(criteria);
            ViewBag.SearchCriteria = criteria;
            return View("SearchResults", rooms);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(criteria);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var room = await _roomService.GetRoomDetailAsync(id);
        if (room == null) return NotFound();

        room.ReviewsSummary = await _reviewService.GetRoomReviewsSummaryAsync(id);

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            ViewBag.CanReview = true;
            ViewBag.HasStayed = await _reviewService.CanUserReviewAsync(userId, id);
        }
        else
        {
            ViewBag.CanReview = false;
            ViewBag.HasStayed = false;
        }

        return View(room);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(CreateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn số sao từ 1 đến 5.";
            return RedirectToAction("Details", new { id = dto.RoomId });
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _reviewService.AddReviewAsync(userId, dto, _env.WebRootPath);

        if (success)
        {
            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = error ?? "Đã có lỗi xảy ra khi gửi đánh giá.";
        }

        return RedirectToAction("Details", new { id = dto.RoomId });
    }

    [HttpPost]
    [Authorize(Roles = "Receptionist,Manager,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyReview(int reviewId, int roomId, string response)
    {
        var staffUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _reviewService.ReplyReviewAsync(staffUserId, reviewId, response);

        if (success)
        {
            TempData["SuccessMessage"] = "Đã gửi phản hồi đến đánh giá của khách hàng thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = error ?? "Không thể gửi phản hồi.";
        }

        return RedirectToAction("Details", new { id = roomId });
    }
}