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

    [HttpGet]
    public async Task<IActionResult> ExportRevenueCsv()
    {
        var data = await _reportService.GetMonthlyRevenueAsync();
        var csvBuilder = new System.Text.StringBuilder();
        csvBuilder.AppendLine("Năm,Tháng,Tổng Doanh Thu (VNĐ),Số Lượt Đặt Đã Thanh Toán");
        foreach (var item in data)
        {
            csvBuilder.AppendLine($"{item.Year},{item.Month},{item.TotalRevenue},{item.TotalBookingsPaid}");
        }
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
        var result = new byte[bytes.Length + 3];
        result[0] = 0xEF;
        result[1] = 0xBB;
        result[2] = 0xBF;
        Buffer.BlockCopy(bytes, 0, result, 3, bytes.Length);

        return File(result, "text/csv", "BaoCaoDoanhThu.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportOccupancyCsv()
    {
        var data = await _reportService.GetRoomOccupancyAsync();
        var csvBuilder = new System.Text.StringBuilder();
        csvBuilder.AppendLine("Số Phòng,Tổng Lượt Đặt,Tổng Số Đêm Được Thuê");
        foreach (var item in data)
        {
            csvBuilder.AppendLine($"{item.RoomNumber},{item.TotalBookings},{item.TotalNightsBooked}");
        }
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
        var result = new byte[bytes.Length + 3];
        result[0] = 0xEF;
        result[1] = 0xBB;
        result[2] = 0xBF;
        Buffer.BlockCopy(bytes, 0, result, 3, bytes.Length);

        return File(result, "text/csv", "BaoCaoTanSuatPhong.csv");
    }
}
