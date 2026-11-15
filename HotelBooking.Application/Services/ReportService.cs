using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly HotelBookingDbContext _context;

        public ReportService(HotelBookingDbContext context)
        {
            _context = context;
        }

        public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync()
        {
            return await _context.VwMonthlyRevenues
                .OrderByDescending(v => v.RevenueYear)
                .ThenByDescending(v => v.RevenueMonth)
                .Select(v => new MonthlyRevenueDto
                {
                    Year = v.RevenueYear ?? 0,
                    Month = v.RevenueMonth ?? 0,
                    TotalRevenue = v.TotalRevenue ?? 0,
                    TotalBookingsPaid = v.TotalBookingsPaid ?? 0
                })
                .ToListAsync();
        }

        public async Task<List<RoomOccupancyDto>> GetRoomOccupancyAsync()
        {
            return await _context.VwRoomOccupancies
                .OrderByDescending(v => v.TotalNightsBooked)
                .Select(v => new RoomOccupancyDto
                {
                    RoomNumber = v.RoomNumber,
                    TotalBookings = v.TotalBookings ?? 0,
                    TotalNightsBooked = v.TotalNightsBooked
                })
                .ToListAsync();
        }

        public async Task<ReportChartDto> GetChartDataAsync()
        {
            // Revenue data (last 12 months from view)
            var revenue = await GetMonthlyRevenueAsync();

            // RoomType stats: group BookingRooms by RoomType
            var roomTypeStats = await _context.BookingRooms
                .GroupBy(br => br.Room.RoomType.TypeName)
                .Select(g => new RoomTypeStatsDto
                {
                    TypeName = g.Key,
                    TotalBookings = g.Count()
                })
                .OrderByDescending(x => x.TotalBookings)
                .ToListAsync();

            // Occupancy rate: total nights booked / (total rooms * 30) * 100
            int totalRooms = await _context.Rooms.CountAsync();
            int totalNightsBooked = await _context.BookingRooms
                .Where(br => br.Booking.Status == "CheckedOut" || br.Booking.Status == "CheckedIn")
                .SumAsync(br => (int?)(br.Booking.CheckOutDate.DayNumber - br.Booking.CheckInDate.DayNumber)) ?? 0;

            double occupancyRate = totalRooms > 0
                ? Math.Round((double)totalNightsBooked / (totalRooms * 30) * 100, 2)
                : 0;

            return new ReportChartDto
            {
                Revenue = revenue,
                RoomTypeStats = roomTypeStats,
                OccupancyRatePercent = occupancyRate
            };
        }
    }
}
