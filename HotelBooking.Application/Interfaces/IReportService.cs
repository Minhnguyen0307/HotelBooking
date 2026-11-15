using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IReportService
    {
        Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync();
        Task<List<RoomOccupancyDto>> GetRoomOccupancyAsync();
        Task<ReportChartDto> GetChartDataAsync();
        Task<ExecutiveSummaryDto> GetExecutiveSummaryAsync();
        Task<RevenuePeriodDto> GetRevenuePeriodAsync(string? period, DateOnly? anchor);
        Task<List<ConfirmedBillDto>> GetConfirmedBillsAsync();
    }
}
