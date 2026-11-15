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

        public async Task<RevenuePeriodDto> GetRevenuePeriodAsync(string? period, DateOnly? anchor)
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));
            var day = anchor ?? today;
            var kind = period?.ToLowerInvariant() switch
            {
                "week" => "week",
                "month" => "month",
                _ => "day"
            };
            DateOnly start = day;
            DateOnly endExclusive = day.AddDays(1);
            if (kind == "week")
            {
                var offset = ((int)day.DayOfWeek + 6) % 7;
                start = day.AddDays(-offset);
                endExclusive = start.AddDays(7);
            }
            else if (kind == "month")
            {
                start = new DateOnly(day.Year, day.Month, 1);
                endExclusive = start.AddMonths(1);
            }

            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(start.ToDateTime(TimeOnly.MinValue), zone);
            var toUtc = TimeZoneInfo.ConvertTimeToUtc(endExclusive.ToDateTime(TimeOnly.MinValue), zone);
            var payments = await _context.Payments
                .Where(p => p.PaymentStatus == "Success" && (p.PaymentDate ?? p.CreatedAt) >= fromUtc && (p.PaymentDate ?? p.CreatedAt) < toUtc)
                .Select(p => new { p.Amount, When = p.PaymentDate ?? p.CreatedAt, p.PaymentMethod, p.BookingId })
                .ToListAsync();

            List<RevenueBucketDto> buckets;
            if (kind == "day")
            {
                buckets = payments
                    .OrderBy(p => p.When)
                    .Select(p => new RevenueBucketDto
                    {
                        Label = $"{TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(p.When, DateTimeKind.Utc), zone):HH:mm} · đơn #{p.BookingId} · {p.PaymentMethod}",
                        Amount = p.Amount,
                        Count = 1
                    })
                    .ToList();
            }
            else
            {
                var byDay = payments
                    .GroupBy(p => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(p.When, DateTimeKind.Utc), zone)))
                    .ToDictionary(g => g.Key, g => (Amount: g.Sum(x => x.Amount), Count: g.Count()));
                buckets = new List<RevenueBucketDto>();
                for (var cursor = start; cursor < endExclusive; cursor = cursor.AddDays(1))
                {
                    byDay.TryGetValue(cursor, out var item);
                    buckets.Add(new RevenueBucketDto
                    {
                        Label = cursor.ToString("dd/MM"),
                        Amount = item.Amount,
                        Count = item.Count
                    });
                }
            }

            return new RevenuePeriodDto
            {
                Period = kind,
                From = start,
                To = endExclusive.AddDays(-1),
                Total = payments.Sum(p => p.Amount),
                PaymentCount = payments.Count,
                Buckets = buckets
            };
        }

        public async Task<List<ConfirmedBillDto>> GetConfirmedBillsAsync()
        {
            var bills = await _context.Bookings
                .Where(b => b.Status == "CheckedOut")
                .OrderByDescending(b => b.BillConfirmedAt ?? b.CreatedAt)
                .Take(80)
                .Select(b => new
                {
                    b.BookingId,
                    GuestName = b.GuestName ?? b.Customer.FullName,
                    RoomNumber = b.BookingRooms.Select(br => br.Room.RoomNumber).FirstOrDefault() ?? "",
                    Collected = b.Payments.Where(p => p.PaymentStatus == "Success").Sum(p => p.Amount),
                    b.BillConfirmedByUserId,
                    b.BillConfirmedAt
                })
                .ToListAsync();

            var staffIds = bills.Where(b => b.BillConfirmedByUserId != null).Select(b => b.BillConfirmedByUserId!.Value).Distinct().ToList();
            var names = await _context.Users
                .Where(u => staffIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            return bills.Select(b => new ConfirmedBillDto
            {
                BookingId = b.BookingId,
                GuestName = b.GuestName,
                RoomNumber = b.RoomNumber,
                Collected = b.Collected,
                ConfirmedBy = b.BillConfirmedByUserId is int id && names.TryGetValue(id, out var name) ? name : null,
                ConfirmedAt = b.BillConfirmedAt
            }).ToList();
        }
    }
}
