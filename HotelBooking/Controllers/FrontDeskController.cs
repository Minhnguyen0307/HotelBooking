using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Receptionist,Manager,Admin")]
public class FrontDeskController : Controller
{
    private readonly IFrontDeskService _frontDeskService;
    public FrontDeskController(IFrontDeskService frontDeskService)
    {
        _frontDeskService = frontDeskService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Arrivals = await _frontDeskService.GetTodayArrivalsAsync();
        ViewBag.Departures = await _frontDeskService.GetTodayDeparturesAsync();
        ViewBag.AllCheckedIn = await _frontDeskService.GetAllCheckedInAsync(); // thêm mới
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
}