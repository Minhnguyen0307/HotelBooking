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
    public async Task<IActionResult> Index([FromQuery] int? propertyId, [FromQuery] int? year, [FromQuery] int? month)
    {
        return Ok(new
        {
            revenue = await _reportService.GetMonthlyRevenueAsync(),
            occupancy = await _reportService.GetRoomOccupancyAsync(),
            propertyRevenue = await _reportService.GetPropertyRevenueSummaryAsync(propertyId, year, month),
            propertyMonthlyRevenue = await _reportService.GetPropertyMonthlyRevenueAsync(propertyId, year),
            properties = await _reportService.GetPropertiesListAsync()
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

    [HttpGet("property-revenue.csv")]
    public async Task<IActionResult> PropertyRevenueCsv([FromQuery] int? propertyId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var data = await _reportService.GetPropertyRevenueSummaryAsync(propertyId, year, month);
        var csv = new StringBuilder();
        csv.AppendLine("Mã Cơ Sở,Tên Cơ Sở,Khu Vực,Địa Chỉ,Số Phòng,Tổng Doanh Thu (VNĐ),Số Lượt Đặt Đã Thanh Toán,Tổng Số Đêm Thuê,Doanh Thu TB Mỗi Đơn (VNĐ)");
        foreach (var item in data)
        {
            var cleanAddress = (item.Address ?? string.Empty).Replace("\"", "\"\"");
            csv.AppendLine($"{item.PropertyId},\"{item.PropertyName}\",\"{item.Area}\",\"{cleanAddress}\",{item.TotalRooms},{item.TotalRevenue},{item.TotalBookingsPaid},{item.TotalNightsBooked},{item.AverageRevenuePerBooking}");
        }
        return Csv(csv.ToString(), "BaoCaoDoanhThuTheoCoSo.csv");
    }

    [HttpGet("property-monthly.csv")]
    public async Task<IActionResult> PropertyMonthlyCsv([FromQuery] int? propertyId, [FromQuery] int? year)
    {
        var data = await _reportService.GetPropertyMonthlyRevenueAsync(propertyId, year);
        var csv = new StringBuilder();
        csv.AppendLine("Năm,Tháng,Mã Cơ Sở,Tên Cơ Sở,Khu Vực,Tổng Doanh Thu (VNĐ),Số Lượt Đặt Đã Thanh Toán,Tổng Số Đêm Thuê");
        foreach (var item in data)
        {
            csv.AppendLine($"{item.Year},{item.Month},{item.PropertyId},\"{item.PropertyName}\",\"{item.Area}\",{item.TotalRevenue},{item.TotalBookingsPaid},{item.TotalNightsBooked}");
        }
        return Csv(csv.ToString(), "BaoCaoDoanhThuCoSoTheoThang.csv");
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
