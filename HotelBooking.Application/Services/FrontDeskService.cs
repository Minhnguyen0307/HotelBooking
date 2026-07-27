using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class FrontDeskService : IFrontDeskService
    {
        private readonly HotelBookingDbContext _context;

        public FrontDeskService(HotelBookingDbContext context)
        {
            _context = context;
        }

        public async Task<List<BookingListItemDto>> GetTodayArrivalsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            return await _context.Bookings
                .Where(b => b.CheckInDate == today && b.Status == "Confirmed")
                .Select(b => new BookingListItemDto
                {
                    BookingId = b.BookingId,
                    RoomNumber = b.BookingRooms.Select(br => br.Room.RoomNumber).FirstOrDefault() ?? "",
                    TypeName = b.BookingRooms.Select(br => br.Room.RoomType.TypeName).FirstOrDefault() ?? "",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<List<BookingListItemDto>> GetTodayDeparturesAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            return await _context.Bookings
                .Where(b => b.CheckOutDate == today && b.Status == "CheckedIn")
                .Select(b => new BookingListItemDto
                {
                    BookingId = b.BookingId,
                    RoomNumber = b.BookingRooms.Select(br => br.Room.RoomNumber).FirstOrDefault() ?? "",
                    TypeName = b.BookingRooms.Select(br => br.Room.RoomType.TypeName).FirstOrDefault() ?? "",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string? Error)> CheckInAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return (false, "Không tìm thấy đơn đặt phòng.");
            if (booking.Status != "Confirmed")
                return (false, $"Đơn đang ở trạng thái '{booking.Status}', không thể check-in.");

            booking.Status = "CheckedIn";
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> CheckOutAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return (false, "Không tìm thấy đơn đặt phòng.");
            if (booking.Status != "CheckedIn")
                return (false, $"Đơn đang ở trạng thái '{booking.Status}', không thể check-out.");

            booking.Status = "CheckedOut";
            foreach (var br in booking.BookingRooms)
            {
                var room = await _context.Rooms.FindAsync(br.RoomId);
                if (room != null) room.Status = "Available";
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<BookingListItemDto>> GetCancelRequestsAsync()
        {
            return await _context.Bookings
                .Where(b => b.Status == "CancelRequested")
                .Select(b => new BookingListItemDto
                {
                    BookingId = b.BookingId,
                    RoomNumber = b.BookingRooms.Select(br => br.Room.RoomNumber).FirstOrDefault() ?? "",
                    TypeName = b.BookingRooms.Select(br => br.Room.RoomType.TypeName).FirstOrDefault() ?? "",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice,
                    CreatedAt = b.CreatedAt,
                    CustomerName = b.Customer.FullName,
                    CancelReason = b.CancelReason
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string? Error)> ConfirmCancelAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return (false, "Không tìm thấy đơn đặt phòng.");
            if (booking.Status != "CancelRequested")
                return (false, $"Đơn đang ở trạng thái '{booking.Status}', không thể xác nhận hủy.");

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.UtcNow;

            // Set room status back to Available
            foreach (var br in booking.BookingRooms)
            {
                var room = await _context.Rooms.FindAsync(br.RoomId);
                if (room != null) room.Status = "Available";
            }

            // Check if booking is paid and initiate a refund
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.PaymentStatus == "Success");
            if (payment != null)
            {
                var refund = new Refund
                {
                    PaymentId = payment.PaymentId,
                    Amount = payment.Amount,
                    Reason = booking.CancelReason ?? "Lễ tân xác nhận hủy đơn",
                    Status = "Processed",
                    RequestedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow
                };
                _context.Refunds.Add(refund);
                payment.PaymentStatus = "Refunded";
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}