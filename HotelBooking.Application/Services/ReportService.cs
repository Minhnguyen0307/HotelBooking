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
    }
}