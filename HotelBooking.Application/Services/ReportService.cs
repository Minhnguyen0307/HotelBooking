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

        public async Task<List<PropertyRevenueSummaryDto>> GetPropertyRevenueSummaryAsync(int? propertyId = null, int? year = null, int? month = null)
        {
            var propertiesQuery = _context.Properties
                .Include(p => p.Rooms)
                .AsQueryable();

            if (propertyId.HasValue && propertyId.Value > 0)
            {
                propertiesQuery = propertiesQuery.Where(p => p.PropertyId == propertyId.Value);
            }

            var properties = await propertiesQuery
                .OrderBy(p => p.PropertyId)
                .ToListAsync();

            var paymentsQuery = _context.Payments
                .Where(p => p.PaymentStatus == "Success" && p.PaymentDate.HasValue);

            if (year.HasValue && year.Value > 0)
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaymentDate!.Value.Year == year.Value);
            }

            if (month.HasValue && month.Value > 0)
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaymentDate!.Value.Month == month.Value);
            }

            var paymentData = await paymentsQuery
                .Select(p => new
                {
                    p.Amount,
                    p.BookingId,
                    CheckIn = p.Booking.CheckInDate,
                    CheckOut = p.Booking.CheckOutDate,
                    PropertyIds = p.Booking.BookingRooms.Select(br => br.Room.PropertyId).Distinct().ToList()
                })
                .ToListAsync();

            var result = new List<PropertyRevenueSummaryDto>();

            foreach (var prop in properties)
            {
                var propPayments = paymentData
                    .Where(p => p.PropertyIds.Contains(prop.PropertyId))
                    .ToList();

                var totalRev = propPayments.Sum(p => p.Amount);
                var totalBookings = propPayments.Select(p => p.BookingId).Distinct().Count();

                var distinctBookings = propPayments
                    .GroupBy(p => p.BookingId)
                    .Select(g => g.First())
                    .ToList();

                var totalNights = distinctBookings.Sum(b => Math.Max(1, b.CheckOut.DayNumber - b.CheckIn.DayNumber));

                result.Add(new PropertyRevenueSummaryDto
                {
                    PropertyId = prop.PropertyId,
                    PropertyName = prop.Name,
                    Area = prop.Area,
                    Address = prop.Address,
                    TotalRooms = prop.Rooms.Count,
                    TotalRevenue = totalRev,
                    TotalBookingsPaid = totalBookings,
                    TotalNightsBooked = totalNights
                });
            }

            return result.OrderByDescending(r => r.TotalRevenue).ThenBy(r => r.PropertyName).ToList();
        }

        public async Task<List<PropertyMonthlyRevenueDto>> GetPropertyMonthlyRevenueAsync(int? propertyId = null, int? year = null)
        {
            var paymentsQuery = _context.Payments
                .Where(p => p.PaymentStatus == "Success" && p.PaymentDate.HasValue);

            if (year.HasValue && year.Value > 0)
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaymentDate!.Value.Year == year.Value);
            }

            var paymentData = await paymentsQuery
                .Select(p => new
                {
                    p.Amount,
                    PaymentDate = p.PaymentDate!.Value,
                    p.BookingId,
                    CheckIn = p.Booking.CheckInDate,
                    CheckOut = p.Booking.CheckOutDate,
                    Rooms = p.Booking.BookingRooms.Select(br => new
                    {
                        br.Room.PropertyId,
                        PropertyName = br.Room.Property.Name,
                        Area = br.Room.Property.Area
                    }).ToList()
                })
                .ToListAsync();

            var flatList = paymentData
                .SelectMany(p => p.Rooms.Select(r => new
                {
                    r.PropertyId,
                    r.PropertyName,
                    r.Area,
                    Year = p.PaymentDate.Year,
                    Month = p.PaymentDate.Month,
                    p.Amount,
                    p.BookingId,
                    Nights = Math.Max(1, p.CheckOut.DayNumber - p.CheckIn.DayNumber)
                }))
                .ToList();

            if (propertyId.HasValue && propertyId.Value > 0)
            {
                flatList = flatList.Where(x => x.PropertyId == propertyId.Value).ToList();
            }

            var grouped = flatList
                .GroupBy(x => new { x.PropertyId, x.PropertyName, x.Area, x.Year, x.Month })
                .Select(g => new PropertyMonthlyRevenueDto
                {
                    PropertyId = g.Key.PropertyId,
                    PropertyName = g.Key.PropertyName,
                    Area = g.Key.Area,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalRevenue = g.Sum(x => x.Amount),
                    TotalBookingsPaid = g.Select(x => x.BookingId).Distinct().Count(),
                    TotalNightsBooked = g.GroupBy(x => x.BookingId).Sum(bg => bg.First().Nights)
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenBy(x => x.PropertyName)
                .ToList();

            return grouped;
        }

        public async Task<List<PropertyOptionDto>> GetPropertiesListAsync()
        {
            return await _context.Properties
                .OrderBy(p => p.Name)
                .Select(p => new PropertyOptionDto
                {
                    PropertyId = p.PropertyId,
                    Name = p.Name,
                    Area = p.Area
                })
                .ToListAsync();
        }
    }
}
