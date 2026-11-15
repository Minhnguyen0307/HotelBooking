using HotelBooking.Application.DTOs;
using HotelBooking.Application.Hubs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Settings;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly HotelBookingDbContext _context;
        private readonly IHubContext<RoomHub> _hubContext;
        private readonly HotelPolicies _policy;

        public BookingService(HotelBookingDbContext context, IHubContext<RoomHub> hubContext, IOptions<HotelPolicies> policyOptions)
        {
            _context = context;
            _hubContext = hubContext;
            _policy = policyOptions.Value;
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

        private (decimal totalPrice, decimal weekendSurcharge, decimal extraGuestSurcharge, string breakdown)
            CalculatePriceDetails(DateOnly checkIn, DateOnly checkOut, int adults, int children, decimal basePrice)
        {
            int nights = checkOut.DayNumber - checkIn.DayNumber;
            decimal baseRoomTotal = 0;
            decimal weekendSurcharge = 0;
            var breakdownLines = new List<string>();

            for (int i = 0; i < nights; i++)
            {
                var date = checkIn.AddDays(i);
                string ddMM = date.ToString("dd-MM");
                decimal nightPrice = basePrice;

                if (_policy.Holidays.Contains(ddMM))
                {
                    decimal markup = basePrice * (decimal)(_policy.HolidayMarkupPercent / 100.0);
                    nightPrice += markup;
                    weekendSurcharge += markup;
                    breakdownLines.Add($"{date:dd/MM}: Holiday +{_policy.HolidayMarkupPercent}% = +{markup:N0}đ");
                }
                else if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday)
                {
                    decimal markup = basePrice * (decimal)(_policy.WeekendMarkupPercent / 100.0);
                    nightPrice += markup;
                    weekendSurcharge += markup;
                    breakdownLines.Add($"{date:dd/MM}: Weekend +{_policy.WeekendMarkupPercent}% = +{markup:N0}đ");
                }

                baseRoomTotal += nightPrice;
            }

            decimal extraGuestSurcharge =
                Math.Max(0, adults - _policy.StandardGuestsPerRoom) * _policy.ExtraAdultFeePerNight * nights
                + children * _policy.ExtraChildFeePerNight * nights;

            if (extraGuestSurcharge > 0)
                breakdownLines.Add($"Extra guests: +{extraGuestSurcharge:N0}đ");

            decimal totalPrice = baseRoomTotal + extraGuestSurcharge;
            string breakdown = string.Join("; ", breakdownLines);

            return (totalPrice, weekendSurcharge, extraGuestSurcharge, breakdown);
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

            int adults = dto.Adults > 0 ? dto.Adults : (dto.NumberOfGuests > 0 ? dto.NumberOfGuests : 1);
            int children = dto.Children;
            int totalGuests = adults + children;

            var quote = OccupancyPricing.Price(room.RoomType.TypeName, room.RoomType.MaxGuests, room.RoomType.BasePrice, totalGuests, nights);
            if (!quote.Success)
                return FailPreview(quote.ErrorMessage ?? quote.Plan.Rule);

            var (_, weekendSurcharge, extraGuestSurcharge, breakdown) =
                CalculatePriceDetails(dto.CheckInDate, dto.CheckOutDate, adults, children, room.RoomType.BasePrice);

            return new BookingPreviewResult
            {
                Success = true,
                Preview = new BookingConfirmDto
                {
                    RoomId = room.RoomId,
                    RoomNumber = room.RoomNumber,
                    TypeName = room.RoomType.TypeName,
                    CheckInDate = dto.CheckInDate,
                    CheckOutDate = dto.CheckOutDate,
                    NumberOfGuests = totalGuests,
                    Nights = nights,
                    PricePerNight = quote.PricePerNight,
                    TotalPrice = quote.TotalPrice + weekendSurcharge,
                    Adults = adults,
                    Children = children,
                    ExtraGuestSurcharge = quote.Surcharge > 0 ? quote.Surcharge : extraGuestSurcharge,
                    WeekendSurcharge = weekendSurcharge,
                    PriceBreakdown = string.IsNullOrEmpty(breakdown) ? quote.Summary : $"{breakdown}; {quote.Summary}",
                    StayLabel = quote.Plan.Label,
                    IncludedGuests = quote.Plan.IncludedGuests,
                    ExtraGuests = quote.ExtraGuests,
                    ExtraGuestFee = quote.Plan.ExtraGuestFeePerNight,
                    RoomCharge = quote.RoomCharge,
                    Surcharge = quote.Surcharge + weekendSurcharge,
                    Summary = quote.Summary
                }
            };
        }

        private static BookingPreviewResult FailPreview(string message) =>
            new() { Success = false, ErrorMessage = message };

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

            int adults = dto.Adults > 0 ? dto.Adults : (dto.NumberOfGuests > 0 ? dto.NumberOfGuests : 1);
            int children = dto.Children;
            int totalGuests = adults + children;
            int nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;

            var quote = OccupancyPricing.Price(room.RoomType.TypeName, room.RoomType.MaxGuests, room.RoomType.BasePrice, totalGuests, nights);
            if (!quote.Success)
                return new BookingResult { Success = false, ErrorMessage = quote.ErrorMessage };

            var guestName = (dto.GuestName ?? string.Empty).Trim();
            var guestPhone = new string((dto.GuestPhone ?? string.Empty).Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(guestName) || string.IsNullOrWhiteSpace(guestPhone))
            {
                var cust = await _context.Users.FindAsync(customerId);
                if (string.IsNullOrWhiteSpace(guestName)) guestName = cust?.FullName ?? "Khách đặt";
                if (string.IsNullOrWhiteSpace(guestPhone)) guestPhone = cust?.PhoneNumber ?? "0900000000";
            }

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

            var (_, weekendSurcharge, _, _) = CalculatePriceDetails(dto.CheckInDate, dto.CheckOutDate, adults, children, room.RoomType.BasePrice);
            decimal totalPrice = quote.TotalPrice + weekendSurcharge;

            var booking = new Booking
            {
                CustomerId = customerId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfGuests = totalGuests,
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
                    CustomerName = b.Customer.FullName,
                    CustomerPhone = b.Customer.PhoneNumber,
                    NumberOfGuests = b.NumberOfGuests,
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
