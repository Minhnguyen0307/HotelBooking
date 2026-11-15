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
                    CreatedAt = b.CreatedAt,
                    CustomerName = b.Customer.FullName,
                    CustomerPhone = b.Customer.PhoneNumber,
                    NumberOfGuests = b.NumberOfGuests
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
                    CreatedAt = b.CreatedAt,
                    CustomerName = b.Customer.FullName,
                    CustomerPhone = b.Customer.PhoneNumber,
                    NumberOfGuests = b.NumberOfGuests
                })
                .ToListAsync();
        }

        public async Task<List<BookingListItemDto>> GetAllCheckedInAsync()
        {
            return await _context.Bookings
                .Where(b => b.Status == "CheckedIn")
                .OrderBy(b => b.CheckOutDate)
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
                    CustomerPhone = b.Customer.PhoneNumber,
                    NumberOfGuests = b.NumberOfGuests
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
                if (room != null) room.Status = "Maintenance";
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
                    CustomerPhone = b.Customer.PhoneNumber,
                    CancelReason = b.CancelReason,
                    NumberOfGuests = b.NumberOfGuests
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

            foreach (var br in booking.BookingRooms)
            {
                var room = await _context.Rooms.FindAsync(br.RoomId);
                if (room != null) room.Status = "Available";
            }

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

        // ── New Methods ──────────────────────────────────────────────────────────

        public async Task<List<RoomMatrixFloorDto>> GetRoomMatrixAsync()
        {
            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.BookingRooms)
                    .ThenInclude(br => br.Booking)
                        .ThenInclude(b => b.Customer)
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();

            var floorGroups = rooms
                .GroupBy(r => r.Floor ?? 0)
                .OrderBy(g => g.Key);

            var result = new List<RoomMatrixFloorDto>();
            foreach (var floorGroup in floorGroups)
            {
                var floorDto = new RoomMatrixFloorDto { Floor = floorGroup.Key };
                foreach (var room in floorGroup)
                {
                    var activeBooking = room.BookingRooms
                        .Select(br => br.Booking)
                        .FirstOrDefault(b => b.Status == "CheckedIn");

                    floorDto.Rooms.Add(new RoomMatrixItemDto
                    {
                        RoomId = room.RoomId,
                        RoomNumber = room.RoomNumber,
                        Floor = room.Floor,
                        TypeName = room.RoomType.TypeName,
                        BasePrice = room.RoomType.BasePrice,
                        Status = room.Status,
                        ActiveBookingId = activeBooking?.BookingId,
                        GuestName = activeBooking?.Customer?.FullName,
                        GuestPhone = activeBooking?.Customer?.PhoneNumber,
                        CheckInDate = activeBooking?.CheckInDate,
                        CheckOutDate = activeBooking?.CheckOutDate
                    });
                }
                result.Add(floorDto);
            }
            return result;
        }

        public async Task<(bool Success, string? Error)> MarkRoomCleanAsync(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return (false, "Không tìm thấy phòng.");
            if (room.Status != "Maintenance")
                return (false, $"Phòng đang ở trạng thái '{room.Status}', không cần dọn dẹp.");
            room.Status = "Available";
            room.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error, int? BookingId)> CreateWalkInBookingAsync(WalkInBookingDto dto)
        {
            // Find or create guest user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == dto.GuestPhone);

            if (user == null)
            {
                user = new User
                {
                    RoleId = 1,
                    FullName = dto.GuestName,
                    Email = dto.GuestEmail ?? $"walkin_{Guid.NewGuid():N}@hbs.local",
                    PhoneNumber = dto.GuestPhone,
                    PasswordHash = "WALKIN_NO_LOGIN",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Verify room exists
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);
            if (room == null) return (false, "Không tìm thấy phòng.", null);

            int nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
            if (nights <= 0) return (false, "Ngày trả phòng phải sau ngày nhận phòng.", null);

            decimal totalPrice = room.RoomType.BasePrice * nights;

            var booking = new Booking
            {
                CustomerId = user.UserId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfGuests = dto.Adults + dto.Children,
                Status = "CheckedIn",
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

            // Add payment record
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                Amount = totalPrice,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = "Success",
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);

            // Mark room as Booked
            room.Status = "Booked";
            room.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, null, booking.BookingId);
        }

        public async Task<(bool Success, string? Error)> TransferRoomAsync(int bookingId, int newRoomId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                    .ThenInclude(br => br.Room)
                        .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return (false, "Không tìm thấy đơn đặt phòng.");
            if (booking.Status != "CheckedIn")
                return (false, $"Chỉ có thể chuyển phòng khi đang check-in.");

            var newRoom = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == newRoomId);
            if (newRoom == null) return (false, "Không tìm thấy phòng mới.");
            if (newRoom.Status != "Available")
                return (false, $"Phòng mới đang ở trạng thái '{newRoom.Status}', không thể chuyển.");

            var bookingRoom = booking.BookingRooms.First();
            var oldRoom = bookingRoom.Room;

            // Transfer
            bookingRoom.RoomId = newRoomId;
            bookingRoom.PricePerNight = newRoom.RoomType.BasePrice;

            // Update room statuses
            oldRoom.Status = "Maintenance";
            oldRoom.UpdatedAt = DateTime.UtcNow;
            newRoom.Status = "Booked";
            newRoom.UpdatedAt = DateTime.UtcNow;

            // Recalculate total price
            int nights = booking.CheckOutDate.DayNumber - booking.CheckInDate.DayNumber;
            booking.TotalPrice = newRoom.RoomType.BasePrice * nights;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ExtendStayAsync(int bookingId, DateOnly newCheckOutDate)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                    .ThenInclude(br => br.Room)
                        .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return (false, "Không tìm thấy đơn đặt phòng.");
            if (booking.Status != "CheckedIn")
                return (false, "Chỉ có thể gia hạn khi đang check-in.");
            if (newCheckOutDate <= booking.CheckOutDate)
                return (false, "Ngày trả phòng mới phải sau ngày trả phòng hiện tại.");

            var bookingRoom = booking.BookingRooms.First();
            int roomId = bookingRoom.RoomId;

            // Check no conflicting bookings in the extended period
            bool conflict = await _context.BookingRooms
                .Where(br => br.RoomId == roomId && br.BookingId != bookingId)
                .Where(br => br.Booking.Status == "Pending"
                          || br.Booking.Status == "Confirmed"
                          || br.Booking.Status == "CheckedIn"
                          || br.Booking.Status == "CancelRequested")
                .AnyAsync(br => br.Booking.CheckInDate < newCheckOutDate && br.Booking.CheckOutDate > booking.CheckOutDate);

            if (conflict)
                return (false, "Phòng đã có booking khác trong khoảng thời gian gia hạn.");

            int extraNights = newCheckOutDate.DayNumber - booking.CheckOutDate.DayNumber;
            decimal extraPrice = extraNights * bookingRoom.Room.RoomType.BasePrice;

            booking.TotalPrice += extraPrice;
            booking.CheckOutDate = newCheckOutDate;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<InvoiceDto?> GetInvoiceAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingRooms)
                    .ThenInclude(br => br.Room)
                        .ThenInclude(r => r.RoomType)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return null;

            var bookingRoom = booking.BookingRooms.First();
            int nights = booking.CheckOutDate.DayNumber - booking.CheckInDate.DayNumber;
            decimal baseRoomTotal = bookingRoom.PricePerNight * nights;
            decimal subTotal = booking.TotalPrice;
            decimal vatAmount = Math.Round(subTotal * 0.10m, 0);
            decimal totalAmount = subTotal + vatAmount;

            var payment = booking.Payments.FirstOrDefault();

            return new InvoiceDto
            {
                BookingId = booking.BookingId,
                GuestName = booking.Customer.FullName,
                GuestPhone = booking.Customer.PhoneNumber,
                GuestEmail = booking.Customer.Email,
                RoomNumber = bookingRoom.Room.RoomNumber,
                RoomTypeName = bookingRoom.Room.RoomType.TypeName,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Nights = nights,
                NumberOfGuests = booking.NumberOfGuests,
                BaseRoomTotal = baseRoomTotal,
                ExtraGuestSurcharge = 0,
                WeekendSurcharge = 0,
                SubTotal = subTotal,
                VatAmount = vatAmount,
                TotalAmount = totalAmount,
                PaymentMethod = payment?.PaymentMethod ?? "",
                PaymentStatus = payment?.PaymentStatus ?? "",
                TransactionId = payment?.TransactionId,
                PaymentDate = payment?.PaymentDate,
                InvoiceGeneratedAt = DateTime.UtcNow
            };
        }
    }
}
