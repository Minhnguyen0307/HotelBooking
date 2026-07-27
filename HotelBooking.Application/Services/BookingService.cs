using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly HotelBookingDbContext _context;

        public BookingService(HotelBookingDbContext context)
        {
            _context = context;
        }

        // Kiểm tra phòng còn trống trong khoảng ngày (dùng lại logic giống RoomService)
        private async Task<bool> IsRoomAvailableAsync(int roomId, DateOnly checkIn, DateOnly checkOut)
        {
            bool isOverlapping = await _context.BookingRooms
                .Where(br => br.RoomId == roomId)
                .Where(br => br.Booking.Status == "Pending"
                          || br.Booking.Status == "Confirmed"
                          || br.Booking.Status == "CheckedIn"
                          || br.Booking.Status == "CancelRequested")
                .AnyAsync(br => br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn);

            return !isOverlapping;
        }

        public async Task<BookingConfirmDto?> GetBookingPreviewAsync(BookingCreateDto dto)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (dto.CheckInDate < today) return null;
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);

            if (room == null) return null;

            int nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
            if (nights <= 0) return null;

            return new BookingConfirmDto
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                TypeName = room.RoomType.TypeName,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfGuests = dto.NumberOfGuests,
                Nights = nights,
                PricePerNight = room.RoomType.BasePrice,
                TotalPrice = room.RoomType.BasePrice * nights
            };
        }

        public async Task<BookingResult> CreateBookingAsync(int customerId, BookingCreateDto dto)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (dto.CheckInDate < today)
                return new BookingResult { Success = false, ErrorMessage = "Ngày nhận phòng không được là ngày trong quá khứ." };

            if (dto.CheckOutDate <= dto.CheckInDate)
                return new BookingResult { Success = false, ErrorMessage = "Ngày trả phòng phải sau ngày nhận phòng." };

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);

            if (room == null)
                return new BookingResult { Success = false, ErrorMessage = "Phòng không tồn tại." };

            if (dto.NumberOfGuests > room.RoomType.MaxGuests)
                return new BookingResult { Success = false, ErrorMessage = $"Phòng này chỉ chứa tối đa {room.RoomType.MaxGuests} khách." };

  
            bool available = await IsRoomAvailableAsync(dto.RoomId, dto.CheckInDate, dto.CheckOutDate);
            if (!available)
                return new BookingResult { Success = false, ErrorMessage = "Rất tiếc, phòng vừa được người khác đặt trước. Vui lòng chọn phòng khác." };

            int nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
            decimal totalPrice = room.RoomType.BasePrice * nights;

            var booking = new Booking
            {
                CustomerId = customerId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfGuests = dto.NumberOfGuests,
                Status = "Pending",
                TotalPrice = totalPrice,
                CreatedAt = DateTime.UtcNow
            };

            booking.BookingRooms.Add(new BookingRoom
            {
                RoomId = dto.RoomId,
                PricePerNight = room.RoomType.BasePrice
            });

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return new BookingResult { Success = true, BookingId = booking.BookingId };
        }

        public async Task<List<BookingListItemDto>> GetCustomerBookingsAsync(int customerId)
        {
            return await _context.Bookings
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedAt)
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

        public async Task<BookingResult> CancelBookingAsync(int customerId, int bookingId, string? reason)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customerId);

            if (booking == null)
                return new BookingResult { Success = false, ErrorMessage = "Không tìm thấy đơn đặt phòng." };

            if (booking.Status is "CheckedIn" or "CheckedOut" or "Cancelled" or "CancelRequested")
                return new BookingResult { Success = false, ErrorMessage = $"Không thể yêu cầu hủy đơn ở trạng thái '{booking.Status}'." };

            booking.Status = "CancelRequested";
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancelReason = reason;

            await _context.SaveChangesAsync();
            return new BookingResult { Success = true };
        }
    }
}