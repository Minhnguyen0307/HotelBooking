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

        public BookingService(
            HotelBookingDbContext context,
            IHubContext<RoomHub> hubContext,
            IOptions<HotelPolicies> policyOptions)
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

        private decimal CalculateDynamicDateMarkup(DateOnly checkIn, DateOnly checkOut, decimal basePrice, out string breakdown)
        {
            int nights = checkOut.DayNumber - checkIn.DayNumber;
            if (nights <= 0) { breakdown = string.Empty; return 0; }

            decimal totalMarkup = 0;
            var holidaySet = _policy.Holidays != null ? new HashSet<string>(_policy.Holidays) : new HashSet<string>();
            var lines = new List<string>();

            for (int i = 0; i < nights; i++)
            {
                var d = checkIn.AddDays(i);
                var ddMM = d.ToString("dd-MM");

                if (holidaySet.Contains(ddMM) && _policy.HolidayMarkupPercent > 0)
                {
                    decimal markup = basePrice * (decimal)(_policy.HolidayMarkupPercent / 100.0);
                    totalMarkup += markup;
                    lines.Add($"{d:dd/MM} (Lễ +{_policy.HolidayMarkupPercent}%: +{markup:N0}đ)");
                }
                else if ((d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday) && _policy.WeekendMarkupPercent > 0)
                {
                    decimal markup = basePrice * (decimal)(_policy.WeekendMarkupPercent / 100.0);
                    totalMarkup += markup;
                    lines.Add($"{d:dd/MM} (Cuối tuần +{_policy.WeekendMarkupPercent}%: +{markup:N0}đ)");
                }
            }

            breakdown = lines.Count > 0 ? string.Join("; ", lines) : string.Empty;
            return totalMarkup;
        }

        public async Task<BookingPreviewResult> GetBookingPreviewAsync(BookingCreateDto dto)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (dto.CheckInDate < today)
                return FailPreview("Ngày nhận phòng không được là ngày trong quá khứ.");

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.Amenities)
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

            var dateMarkup = CalculateDynamicDateMarkup(dto.CheckInDate, dto.CheckOutDate, room.RoomType.BasePrice, out string markupBreakdown);
            var totalPrice = quote.TotalPrice + dateMarkup;
            var surcharge = quote.Surcharge + dateMarkup;
            var summary = string.IsNullOrEmpty(markupBreakdown)
                ? quote.Summary
                : $"{quote.Summary} · Phụ thu cuối tuần / lễ: +{OccupancyPricing.Money(dateMarkup)} ({markupBreakdown})";
            var discount = await DiscountAmount(dto.PromoCode, totalPrice);
            if (discount.Error != null)
                return FailPreview(discount.Error);
            var services = await QuoteAddOns(room, dto, nights);
            var servicesTotal = services.Sum(line => line.Amount);

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
                    NumberOfGuests = dto.NumberOfGuests,
                    Nights = nights,
                    PricePerNight = quote.PricePerNight,
                    TotalPrice = totalPrice,
                    StayLabel = quote.Plan.Label,
                    IncludedGuests = quote.Plan.IncludedGuests,
                    ExtraGuests = quote.ExtraGuests,
                    ExtraGuestFee = quote.Plan.ExtraGuestFeePerNight,
                    RoomCharge = quote.RoomCharge,
                    Surcharge = surcharge,
                    Discount = discount.Amount,
                    ServicesTotal = servicesTotal,
                    Services = services,
                    BreakfastNote = services.FirstOrDefault(line => line.Note != null)?.Note,
                    Payable = Math.Max(0, totalPrice - discount.Amount + servicesTotal),
                    PromoCode = string.IsNullOrWhiteSpace(dto.PromoCode) ? null : dto.PromoCode.Trim().ToUpperInvariant(),
                    Summary = summary
                }
            };
        }

        private async Task<(decimal Amount, string? Error)> DiscountAmount(string? code, decimal roomTotal)
        {
            if (string.IsNullOrWhiteSpace(code)) return (0, null);
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Code == code.Trim().ToUpperInvariant() && p.IsActive);
            if (promo == null) return (0, "Mã không đúng.");
            var amount = promo.DiscountPercent > 0
                ? Math.Round(roomTotal * promo.DiscountPercent / 100m, 0)
                : promo.DiscountAmount;
            return (Math.Min(Math.Max(0, amount), roomTotal), null);
        }

        private async Task<List<BookingServiceLineDto>> QuoteAddOns(Room room, BookingCreateDto dto, int nights)
        {
            var catalog = await _context.HotelServices.ToListAsync();
            decimal PriceOf(string code, decimal fallback) => catalog.FirstOrDefault(s => s.ServiceCode == code)?.Price ?? fallback;
            string NameOf(string code, string fallback) => catalog.FirstOrDefault(s => s.ServiceCode == code)?.Name ?? fallback;
            var lines = new List<BookingServiceLineDto>();
            var guests = Math.Max(1, dto.NumberOfGuests);
            if (dto.Breakfast)
            {
                var included = room.Amenities.Any(amenity => amenity.Name == "Buffet sáng");
                var name = NameOf("breakfast", "Buffet sáng");
                lines.Add(included
                    ? new BookingServiceLineDto { Name = name, Amount = 0, Note = "Phòng đã có suất buffet sáng rồi." }
                    : new BookingServiceLineDto { Name = $"{name} · {guests} khách · {nights} đêm", Amount = PriceOf("breakfast", 180_000m) * guests * nights });
            }
            if (dto.Spa)
                lines.Add(new BookingServiceLineDto { Name = NameOf("spa", "Spa"), Amount = PriceOf("spa", 300_000m) });
            if (dto.ExtraBed)
            {
                var name = NameOf("extra-bed", "Giường phụ");
                lines.Add(new BookingServiceLineDto { Name = $"{name} · {nights} đêm", Amount = PriceOf("extra-bed", 250_000m) * nights });
            }
            return lines;
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
                .Include(r => r.Amenities)
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
            var services = await QuoteAddOns(room, dto, nights);
            var breakfastNote = services.FirstOrDefault(line => line.Note != null)?.Note;
            var guestNote = string.IsNullOrWhiteSpace(dto.GuestNote) ? null : dto.GuestNote.Trim();
            if (breakfastNote != null)
                guestNote = string.IsNullOrWhiteSpace(guestNote) ? breakfastNote : $"{guestNote} {breakfastNote}";
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

            var dateMarkup = CalculateDynamicDateMarkup(dto.CheckInDate, dto.CheckOutDate, room.RoomType.BasePrice, out _);
            decimal totalPrice = quote.TotalPrice + dateMarkup;
            var discount = await DiscountAmount(dto.PromoCode, totalPrice);
            if (discount.Error != null)
                return new BookingResult { Success = false, ErrorMessage = discount.Error };
            var servicesTotal = services.Sum(line => line.Amount);
            var payable = Math.Max(0, totalPrice - discount.Amount + servicesTotal);

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
                    Amount = payable,
                    PaymentMethod = "Cash",
                    PaymentStatus = "Pending",
                    TransactionId = null,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            if (discount.Amount > 0)
            {
                _context.FolioItems.Add(new FolioItem
                {
                    BookingId = booking.BookingId,
                    Category = "Discount",
                    Description = $"Mã {dto.PromoCode!.Trim().ToUpperInvariant()}",
                    Quantity = 1,
                    UnitPrice = discount.Amount,
                    CreatedAt = DateTime.UtcNow
                });
            }
            foreach (var line in services.Where(line => line.Amount > 0))
            {
                _context.FolioItems.Add(new FolioItem
                {
                    BookingId = booking.BookingId,
                    Category = "Service",
                    Description = line.Name,
                    Quantity = 1,
                    UnitPrice = line.Amount,
                    CreatedAt = DateTime.UtcNow
                });
            }
            if (discount.Amount > 0 || services.Any(line => line.Amount > 0))
                await _context.SaveChangesAsync();

            // Real-time SignalR notifications
            await _hubContext.Clients.All.SendAsync("RoomBooked", new
            {
                RoomId = dto.RoomId,
                CheckInDate = dto.CheckInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = dto.CheckOutDate.ToString("yyyy-MM-dd")
            });

            await _hubContext.Clients.All.SendAsync("NewBookingCreated", new
            {
                BookingId = booking.BookingId,
                GuestName = booking.GuestName,
                RoomNumber = room.RoomNumber,
                TotalPrice = booking.TotalPrice,
                CreatedAt = DateTime.UtcNow
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
