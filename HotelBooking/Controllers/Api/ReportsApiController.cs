using System.Text;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/reports")]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsApiController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return Ok(new
        {
            revenue = await _reportService.GetMonthlyRevenueAsync(),
            occupancy = await _reportService.GetRoomOccupancyAsync()
        });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        return Ok(await _reportService.GetExecutiveSummaryAsync());
    }

    [HttpGet("charts")]
    public async Task<IActionResult> GetCharts()
    {
        var data = await _reportService.GetChartDataAsync();
        return Ok(data);
    }

    [HttpGet("revenue.csv")]
    public async Task<IActionResult> RevenueCsv()
    {
        var data = await _reportService.GetMonthlyRevenueAsync();
        var csv = new StringBuilder();
        csv.AppendLine("Năm,Tháng,Tổng Doanh Thu (VNĐ),Số Lượt Đặt Đã Thanh Toán");
        foreach (var item in data)
            csv.AppendLine($"{item.Year},{item.Month},{item.TotalRevenue},{item.TotalBookingsPaid}");
        return Csv(csv.ToString(), "BaoCaoDoanhThu.csv");
    }

    [HttpGet("occupancy.csv")]
    public async Task<IActionResult> OccupancyCsv()
    {
        var data = await _reportService.GetRoomOccupancyAsync();
        var csv = new StringBuilder();
        csv.AppendLine("Số Phòng,Tổng Lượt Đặt,Tổng Số Đêm Được Thuê");
        foreach (var item in data)
            csv.AppendLine($"{item.RoomNumber},{item.TotalBookings},{item.TotalNightsBooked}");
        return Csv(csv.ToString(), "BaoCaoTanSuatPhong.csv");
    }

    private FileContentResult Csv(string content, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var result = new byte[bytes.Length + 3];
        result[0] = 0xEF;
        result[1] = 0xBB;
        result[2] = 0xBF;
        Buffer.BlockCopy(bytes, 0, result, 3, bytes.Length);
        return File(result, "text/csv", fileName);
    }
}
