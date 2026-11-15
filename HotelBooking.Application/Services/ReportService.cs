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

        public async Task<ExecutiveSummaryDto> GetExecutiveSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            int totalRooms = await _context.Rooms.CountAsync();
            int occupiedRooms = await _context.Bookings.Where(b => b.Status == "CheckedIn").CountAsync();
            int availableRooms = await _context.Rooms
                .Where(r => r.Status == "Available" && r.HousekeepingStatus != "Maintenance")
                .CountAsync();

            int totalBookings = await _context.Bookings.CountAsync();
            int pendingCancels = await _context.Bookings.Where(b => b.Status == "CancelRequested").CountAsync();

            decimal todayRevenue = await _context.Payments
                .Where(p => p.PaymentStatus == "Success" && ((p.PaymentDate.HasValue && p.PaymentDate.Value.Date == today) || (p.CreatedAt.Date == today)))
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            decimal monthRevenue = await _context.Payments
                .Where(p => p.PaymentStatus == "Success" && (p.PaymentDate ?? p.CreatedAt) >= startOfMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            decimal totalRevenue = await _context.Payments
                .Where(p => p.PaymentStatus == "Success")
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            double occupancyPercent = totalRooms > 0
                ? Math.Round((double)occupiedRooms / totalRooms * 100, 1)
                : 0;

            var propertyRevenues = await _context.BookingRooms
                .Where(br => br.Booking.Status != "Cancelled")
                .GroupBy(br => new { br.Room.PropertyId, br.Room.Property.Name })
                .Select(g => new PropertyRevenueDto
                {
                    PropertyId = g.Key.PropertyId,
                    PropertyName = g.Key.Name,
                    Revenue = g.Sum(x => x.Booking.TotalPrice),
                    BookingCount = g.Select(x => x.BookingId).Distinct().Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            var paymentMethods = await _context.Payments
                .Where(p => p.PaymentStatus == "Success")
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new PaymentMethodStatDto
                {
                    Method = string.IsNullOrEmpty(g.Key) ? "Khác" : g.Key,
                    TotalAmount = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            return new ExecutiveSummaryDto
            {
                TotalRooms = totalRooms,
                AvailableRooms = availableRooms,
                OccupiedRooms = occupiedRooms,
                TotalBookings = totalBookings,
                ActiveStays = occupiedRooms,
                PendingCancels = pendingCancels,
                TodayRevenue = todayRevenue,
                MonthRevenue = monthRevenue,
                TotalRevenue = totalRevenue,
                OccupancyPercent = occupancyPercent,
                PropertyRevenues = propertyRevenues,
                PaymentMethodStats = paymentMethods
            };
        }
    }
}
