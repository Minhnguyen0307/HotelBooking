using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IReportService
    {
        Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync();
        Task<List<RoomOccupancyDto>> GetRoomOccupancyAsync();
        Task<ReportChartDto> GetChartDataAsync();
        Task<ExecutiveSummaryDto> GetExecutiveSummaryAsync();
        Task<List<PropertyRevenueSummaryDto>> GetPropertyRevenueSummaryAsync(int? propertyId = null, int? year = null, int? month = null);
        Task<List<PropertyMonthlyRevenueDto>> GetPropertyMonthlyRevenueAsync(int? propertyId = null, int? year = null);
        Task<List<PropertyOptionDto>> GetPropertiesListAsync();
    }
}
