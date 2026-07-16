using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Manager,Admin")]
public class ReportController : Controller
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Revenue = await _reportService.GetMonthlyRevenueAsync();
        ViewBag.Occupancy = await _reportService.GetRoomOccupancyAsync();
        return View();
    }
}
