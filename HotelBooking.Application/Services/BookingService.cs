using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Application.Hubs;
namespace HotelBooking.Application.Services

{
    public class BookingService : IBookingService
    {
        private readonly HotelBookingDbContext _context;
        private readonly IHubContext<RoomHub> _hubContext;
        public BookingService(HotelBookingDbContext context , IHubContext<RoomHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
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

        public async Task<BookingPreviewResult> GetBookingPreviewAsync(BookingCreateDto dto)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (dto.CheckInDate < today)
                return FailPreview("Ngày nhận phòng không được là ngày trong quá khứ.");

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);

            if (room == null)
                return FailPreview("Phòng không tồn tại.");
            if (room.Status == "Maintenance" || room.HousekeepingStatus == "Maintenance")
                return FailPreview("Phòng đang bảo trì, chưa nhận đặt.");

            int nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
            if (nights <= 0)
                return FailPreview("Ngày trả phòng phải sau ngày nhận phòng.");

            var quote = OccupancyPricing.Price(room.RoomType.TypeName, room.RoomType.MaxGuests, room.RoomType.BasePrice, dto.NumberOfGuests, nights);
            if (!quote.Success)
                return FailPreview(quote.ErrorMessage ?? quote.Plan.Rule);

            return new BookingPreviewResult
            {
                Success = true,
                Preview = ToConfirm(room, dto, nights, quote)
            };
        }

        private static BookingPreviewResult FailPreview(string message) =>
            new() { Success = false, ErrorMessage = message };

        private static BookingConfirmDto ToConfirm(Room room, BookingCreateDto dto, int nights, OccupancyPricing.Quote quote) =>
            new()
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                TypeName = room.RoomType.TypeName,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfGuests = dto.NumberOfGuests,
                Nights = nights,
                PricePerNight = quote.PricePerNight,
                TotalPrice = quote.TotalPrice,
                StayLabel = quote.Plan.Label,
                IncludedGuests = quote.Plan.IncludedGuests,
                ExtraGuests = quote.ExtraGuests,
                ExtraGuestFee = quote.Plan.ExtraGuestFeePerNight,
                RoomCharge = quote.RoomCharge,
                Surcharge = quote.Surcharge,
                Summary = quote.Summary
            };

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
            if (room.Status == "Maintenance" || room.HousekeepingStatus == "Maintenance")
                return new BookingResult { Success = false, ErrorMessage = "Phòng đang bảo trì, chưa nhận đặt." };

            int nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
            var quote = OccupancyPricing.Price(room.RoomType.TypeName, room.RoomType.MaxGuests, room.RoomType.BasePrice, dto.NumberOfGuests, nights);
            if (!quote.Success)
                return new BookingResult { Success = false, ErrorMessage = quote.ErrorMessage };

            var guestName = (dto.GuestName ?? string.Empty).Trim();
            var guestPhone = new string((dto.GuestPhone ?? string.Empty).Where(char.IsDigit).ToArray());
            var guestNote = string.IsNullOrWhiteSpace(dto.GuestNote) ? null : dto.GuestNote.Trim();
            var payNow = !string.Equals(dto.PaymentChoice, "Cash", StringComparison.OrdinalIgnoreCase);

            if (guestName.Length < 2 || guestName.Length > 150)
                return new BookingResult { Success = false, ErrorMessage = "Vui lòng nhập tên người nhận phòng." };
            if (guestPhone.Length != 10 || !guestPhone.StartsWith('0'))
                return new BookingResult { Success = false, ErrorMessage = "Số điện thoại cần 10 số và bắt đầu bằng 0." };
            if (guestNote?.Length > 500)
                return new BookingResult { Success = false, ErrorMessage = "Lời nhắn tối đa 500 ký tự." };

            bool available = await IsRoomAvailableAsync(dto.RoomId, dto.CheckInDate, dto.CheckOutDate);
            if (!available)
                return new BookingResult { Success = false, ErrorMessage = "Rất tiếc, phòng vừa được người khác đặt trước. Vui lòng chọn phòng khác." };

            decimal totalPrice = quote.TotalPrice;

            var booking = new Booking
            {
                CustomerId = customerId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfGuests = dto.NumberOfGuests,
                GuestName = guestName,
                GuestPhone = guestPhone,
                GuestNote = guestNote,
                PaymentChoice = payNow ? "Now" : "Cash",
                Status = payNow ? "Pending" : "Confirmed",
                TotalPrice = totalPrice,
                CreatedAt = DateTime.UtcNow
            };

            booking.BookingRooms.Add(new BookingRoom
            {
                RoomId = dto.RoomId,
                PricePerNight = quote.PricePerNight
            });

            if (!payNow)
            {
                booking.Payments.Add(new Payment
                {
                    Amount = totalPrice,
                    PaymentMethod = "Cash",
                    PaymentStatus = "Pending",
                    TransactionId = null,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("RoomBooked", new
            {
                RoomId = dto.RoomId,
                CheckInDate = dto.CheckInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = dto.CheckOutDate.ToString("yyyy-MM-dd")
            });
            return new BookingResult { Success = true, BookingId = booking.BookingId, RequiresPayment = payNow };
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
                    PropertyName = b.BookingRooms.Select(br => br.Room.Property.Name).FirstOrDefault() ?? "",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice,
                    CreatedAt = b.CreatedAt,
                    GuestName = b.GuestName,
                    GuestPhone = b.GuestPhone,
                    GuestNote = b.GuestNote,
                    PaymentChoice = b.PaymentChoice
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