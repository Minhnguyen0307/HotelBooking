using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IReportService
    {
        Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync();
        Task<List<RoomOccupancyDto>> GetRoomOccupancyAsync();
    }
}
