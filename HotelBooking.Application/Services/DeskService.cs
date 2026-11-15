using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class DeskService : IDeskService
    {
        private static readonly string[] Housekeeping = ["Clean", "Cleaning", "Dirty", "Maintenance"];
        private static readonly string[] PayMethods = ["Cash", "Card", "Transfer", "Wallet"];
        private readonly HotelBookingDbContext _context;

        public DeskService(HotelBookingDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<ServiceOfferDto> Services { get; } =
        [
            new() { Name = "Mini-bar nước", Price = 25_000 },
            new() { Name = "Mini-bar snack", Price = 45_000 },
            new() { Name = "Giặt ủi", Price = 80_000 },
            new() { Name = "Nhà hàng", Price = 150_000 },
            new() { Name = "Spa", Price = 300_000 }
        ];

        public async Task<List<DeskRoomDto>> GetRoomsAsync()
        {
            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.Property)
                .OrderBy(r => r.Property.Name)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();
            return rooms.Select(MapRoom).ToList();
        }

        public async Task<List<DeskRoomDto>> AvailableRoomsAsync(DateOnly checkIn, DateOnly checkOut, int? ignoreBookingId)
        {
            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.Property)
                .Where(r => r.Status != "Maintenance" && r.HousekeepingStatus != "Maintenance")
                .ToListAsync();
            var busy = await BusyRoomIds(checkIn, checkOut, ignoreBookingId);
            return rooms.Where(r => !busy.Contains(r.RoomId)).Select(MapRoom).ToList();
        }

        public async Task<List<DeskIssueDto>> OpenIssuesAsync()
        {
            return await _context.RoomIssues
                .Where(i => i.Status == "Open")
                .OrderByDescending(i => i.ReportedAt)
                .Select(i => new DeskIssueDto
                {
                    IssueId = i.IssueId,
                    RoomId = i.RoomId,
                    RoomNumber = i.Room.RoomNumber,
                    Description = i.Description,
                    Status = i.Status
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string? Error)> SetHousekeepingAsync(int roomId, string status)
        {
            if (!Housekeeping.Contains(status))
                return (false, "Trạng thái phòng không hợp lệ.");
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return (false, "Không tìm thấy phòng.");
            room.HousekeepingStatus = status;
            room.Status = status == "Maintenance" ? "Maintenance" : room.Status == "Maintenance" ? "Available" : room.Status;
            room.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ReportIssueAsync(int roomId, string description)
        {
            var note = (description ?? string.Empty).Trim();
            if (note.Length < 3) return (false, "Mô tả sự cố còn quá ngắn.");
            if (!await _context.Rooms.AnyAsync(r => r.RoomId == roomId))
                return (false, "Không tìm thấy phòng.");
            _context.RoomIssues.Add(new RoomIssue
            {
                RoomId = roomId,
                Description = note,
                Status = "Open",
                ReportedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ResolveIssueAsync(int issueId)
        {
            var issue = await _context.RoomIssues.FindAsync(issueId);
            if (issue == null) return (false, "Không tìm thấy sự cố.");
            issue.Status = "Resolved";
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<DeskGuestDto>> SearchGuestsAsync(string? query)
        {
            var term = (query ?? string.Empty).Trim();
            var roleId = await CustomerRoleId();
            var users = _context.Users.Where(u => u.RoleId == roleId);
            if (term.Length > 0)
            {
                users = users.Where(u =>
                    u.FullName.Contains(term) ||
                    u.Email.Contains(term) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            var list = await users.OrderBy(u => u.FullName).Take(12).ToListAsync();
            var ids = list.Select(u => u.UserId).ToList();
            var history = await _context.Bookings
                .Where(b => ids.Contains(b.CustomerId))
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    b.CustomerId,
                    Item = new BookingListItemDto
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
                    }
                })
                .ToListAsync();

            return list.Select(u => new DeskGuestDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                StayCount = history.Count(h => h.CustomerId == u.UserId),
                History = history.Where(h => h.CustomerId == u.UserId).Take(5).Select(h => h.Item).ToList()
            }).ToList();
        }

        public async Task<(bool Success, string? Error)> UpdateGuestAsync(int userId, string fullName, string? phone, string? email)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "Không tìm thấy khách.");
            var name = (fullName ?? string.Empty).Trim();
            if (name.Length < 2) return (false, "Tên khách chưa hợp lệ.");
            var digits = Digits(phone);
            if (digits.Length > 0 && (digits.Length != 10 || !digits.StartsWith('0')))
                return (false, "Số điện thoại cần 10 số và bắt đầu bằng 0.");
            var mail = string.IsNullOrWhiteSpace(email) ? user.Email : email.Trim();
            if (await _context.Users.AnyAsync(u => u.Email == mail && u.UserId != userId))
                return (false, "Email này đã thuộc khách khác.");
            user.FullName = name;
            user.PhoneNumber = digits.Length == 0 ? user.PhoneNumber : digits;
            user.Email = mail;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error, int? BookingId)> CreateWalkInAsync(WalkInDto dto)
        {
            var name = (dto.GuestName ?? string.Empty).Trim();
            var phone = Digits(dto.GuestPhone);
            if (name.Length < 2) return (false, "Nhập tên người nhận phòng.", null);
            if (phone.Length != 10 || !phone.StartsWith('0')) return (false, "Số điện thoại cần 10 số và bắt đầu bằng 0.", null);
            if (dto.CheckOutDate <= dto.CheckInDate) return (false, "Ngày trả phòng phải sau ngày nhận phòng.", null);
            if (dto.Deposit < 0) return (false, "Tiền cọc không hợp lệ.", null);

            var room = await _context.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);
            if (room == null) return (false, "Không tìm thấy phòng.", null);
            if (await BusyRoomIds(dto.CheckInDate, dto.CheckOutDate, null) is var busy && busy.Contains(room.RoomId))
                return (false, "Phòng không trống trong khoảng ngày này.", null);

            var nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
            var quote = OccupancyPricing.Price(room.RoomType.TypeName, room.RoomType.MaxGuests, room.RoomType.BasePrice, dto.NumberOfGuests, nights);
            if (!quote.Success) return (false, quote.ErrorMessage, null);

            var discount = await DiscountAmount(dto.PromoCode, quote.TotalPrice);
            if (discount.Error != null) return (false, discount.Error, null);
            if (dto.Deposit > quote.TotalPrice - discount.Amount)
                return (false, "Tiền cọc lớn hơn số tiền còn lại.", null);

            var customer = await FindOrCreateGuest(name, phone, dto.Email);
            var booking = new Booking
            {
                CustomerId = customer.UserId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfGuests = dto.NumberOfGuests,
                GuestName = name,
                GuestPhone = phone,
                GuestNote = CleanNote(dto.GuestNote),
                PaymentChoice = "Cash",
                Status = "Confirmed",
                TotalPrice = quote.TotalPrice,
                CreatedAt = DateTime.UtcNow
            };
            booking.BookingRooms.Add(new BookingRoom { RoomId = room.RoomId, PricePerNight = quote.PricePerNight });
            if (dto.Deposit > 0)
            {
                booking.Payments.Add(new Payment
                {
                    Amount = dto.Deposit,
                    PaymentMethod = "Cash",
                    PaymentStatus = "Success",
                    TransactionId = "DEP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                    PaymentDate = DateTime.UtcNow,
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
                await _context.SaveChangesAsync();
            }
            return (true, null, booking.BookingId);
        }

        public async Task<FolioDto?> GetFolioAsync(int bookingId)
        {
            var booking = await LoadStay(bookingId);
            if (booking == null) return null;
            return await BuildFolio(booking);
        }

        public async Task<(bool Success, string? Error)> UpdateStayAsync(int bookingId, StayUpdateDto dto)
        {
            var booking = await LoadStay(bookingId);
            if (booking == null) return (false, "Không tìm thấy đơn.");
            if (booking.Status is "CheckedOut" or "Cancelled")
                return (false, "Đơn này không còn sửa được.");
            var name = (dto.GuestName ?? string.Empty).Trim();
            var phone = Digits(dto.GuestPhone);
            if (name.Length < 2) return (false, "Nhập tên người nhận phòng.");
            if (phone.Length != 10 || !phone.StartsWith('0')) return (false, "Số điện thoại cần 10 số và bắt đầu bằng 0.");
            if (dto.CheckOutDate <= dto.CheckInDate) return (false, "Ngày trả phòng phải sau ngày nhận phòng.");

            var roomId = booking.BookingRooms.Select(br => br.RoomId).FirstOrDefault();
            if (await BusyRoomIds(dto.CheckInDate, dto.CheckOutDate, bookingId) is var busy && busy.Contains(roomId))
                return (false, "Phòng không trống cho ngày mới.");

            booking.GuestName = name;
            booking.GuestPhone = phone;
            booking.GuestNote = CleanNote(dto.GuestNote);
            booking.CheckInDate = dto.CheckInDate;
            booking.CheckOutDate = dto.CheckOutDate;
            var link = booking.BookingRooms.First();
            var nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
            var quote = OccupancyPricing.Price(link.Room.RoomType.TypeName, link.Room.RoomType.MaxGuests, link.Room.RoomType.BasePrice, booking.NumberOfGuests, nights);
            if (!quote.Success) return (false, quote.ErrorMessage);
            link.PricePerNight = quote.PricePerNight;
            booking.TotalPrice = quote.TotalPrice;
            var promoError = await ApplyPromo(booking, dto.PromoCode);
            if (promoError != null) return (false, promoError);
            await SyncPending(booking);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ChangeRoomAsync(int bookingId, int roomId)
        {
            var booking = await LoadStay(bookingId);
            if (booking == null) return (false, "Không tìm thấy đơn.");
            if (booking.Status is "CheckedOut" or "Cancelled")
                return (false, "Đơn này không còn đổi phòng.");
            var current = booking.BookingRooms.FirstOrDefault();
            if (current == null) return (false, "Đơn chưa gắn phòng.");
            if (current.RoomId == roomId) return (false, "Khách đang ở phòng này.");
            var room = await _context.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null) return (false, "Không tìm thấy phòng mới.");
            if (await BusyRoomIds(booking.CheckInDate, booking.CheckOutDate, bookingId) is var busy && busy.Contains(roomId))
                return (false, "Phòng mới không trống trong kỳ ở này.");

            var nights = booking.CheckOutDate.DayNumber - booking.CheckInDate.DayNumber;
            var quote = OccupancyPricing.Price(room.RoomType.TypeName, room.RoomType.MaxGuests, room.RoomType.BasePrice, booking.NumberOfGuests, nights);
            if (!quote.Success) return (false, quote.ErrorMessage);

            var from = current.Room.RoomNumber;
            current.RoomId = room.RoomId;
            current.PricePerNight = quote.PricePerNight;
            booking.TotalPrice = quote.TotalPrice;
            booking.GuestNote = JoinNote(booking.GuestNote, $"Đổi phòng {from} sang {room.RoomNumber}.");
            await SyncPending(booking);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> AddChargeAsync(int bookingId, ChargeDto dto)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return (false, "Không tìm thấy đơn.");
            if (booking.Status != "CheckedIn") return (false, "Chỉ cộng dịch vụ khi khách đang ở.");
            var name = (dto.Description ?? string.Empty).Trim();
            if (name.Length < 2 || dto.Quantity < 1 || dto.UnitPrice < 0)
                return (false, "Dịch vụ chưa đủ thông tin.");
            var category = dto.Category is "Minibar" or "Damage" ? dto.Category : "Service";
            _context.FolioItems.Add(new FolioItem
            {
                BookingId = bookingId,
                Category = category,
                Description = name,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> AddDepositAsync(int bookingId, DepositDto dto)
        {
            var booking = await LoadStay(bookingId);
            if (booking == null) return (false, "Không tìm thấy đơn.");
            if (booking.Status is "CheckedOut" or "Cancelled") return (false, "Đơn này không nhận cọc.");
            if (dto.Amount <= 0) return (false, "Số tiền cọc phải lớn hơn 0.");
            if (!PayMethods.Contains(dto.Method)) return (false, "Hình thức thu cọc không hợp lệ.");
            var folio = await BuildFolio(booking);
            if (dto.Amount > folio.Due) return (false, "Tiền cọc lớn hơn số còn phải thu.");
            await SyncPending(booking);
            _context.Payments.Add(new Payment
            {
                BookingId = bookingId,
                Amount = dto.Amount,
                PaymentMethod = dto.Method,
                PaymentStatus = "Success",
                TransactionId = "DEP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> CheckInConfirmAsync(int bookingId, CheckInDto dto)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return (false, "Không tìm thấy đơn đặt phòng.");
            if (booking.Status != "Confirmed")
                return (false, "Chỉ nhận phòng khi đơn đã được xác nhận.");
            var name = (dto.GuestName ?? string.Empty).Trim();
            var phone = Digits(dto.GuestPhone);
            if (name.Length < 2) return (false, "Xác nhận tên người nhận phòng.");
            if (phone.Length != 10 || !phone.StartsWith('0'))
                return (false, "Xác nhận số điện thoại 10 số, bắt đầu bằng 0.");

            booking.GuestName = name;
            booking.GuestPhone = phone;
            await SyncPending(booking);
            var cash = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.PaymentMethod == "Cash" && p.PaymentStatus == "Pending");
            if (cash != null && cash.Amount > 0)
            {
                cash.PaymentStatus = "Success";
                cash.PaymentDate = DateTime.UtcNow;
                cash.TransactionId ??= "CASH-" + bookingId;
            }
            booking.Status = "CheckedIn";
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> CheckOutWithBillAsync(int bookingId, CheckoutDto dto)
        {
            var booking = await LoadStay(bookingId);
            if (booking == null) return (false, "Không tìm thấy đơn.");
            if (booking.Status != "CheckedIn") return (false, "Chỉ trả phòng khi khách đang ở.");
            if (!PayMethods.Contains(dto.PaymentMethod)) return (false, "Hình thức thanh toán không hợp lệ.");
            if (dto.Condition is not ("Dirty" or "Clean" or "Maintenance"))
                return (false, "Chọn tình trạng phòng khi trả.");

            foreach (var line in dto.Minibar.Where(l => l.Quantity > 0))
            {
                _context.FolioItems.Add(new FolioItem
                {
                    BookingId = bookingId,
                    Category = "Minibar",
                    Description = line.Name,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            booking = (await LoadStay(bookingId))!;
            var folio = await BuildFolio(booking);
            if (folio.Due > 0)
            {
                _context.Payments.Add(new Payment
                {
                    BookingId = bookingId,
                    Amount = folio.Due,
                    PaymentMethod = dto.PaymentMethod,
                    PaymentStatus = "Success",
                    TransactionId = "OUT-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                    PaymentDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var damage = (dto.DamageNote ?? string.Empty).Trim();
            if (damage.Length > 0)
            {
                _context.RoomIssues.Add(new RoomIssue
                {
                    RoomId = booking.BookingRooms.First().RoomId,
                    Description = damage,
                    Status = "Open",
                    ReportedAt = DateTime.UtcNow
                });
            }

            foreach (var link in booking.BookingRooms)
            {
                link.Room.HousekeepingStatus = dto.Condition == "Maintenance" || damage.Length > 0 ? "Maintenance" : dto.Condition;
                link.Room.Status = link.Room.HousekeepingStatus == "Maintenance" ? "Maintenance" : "Available";
                link.Room.UpdatedAt = DateTime.UtcNow;
            }
            booking.Status = "CheckedOut";
            await _context.SaveChangesAsync();
            return (true, null);
        }

        private async Task SyncPending(Booking booking)
        {
            var discounts = await _context.FolioItems
                .Where(f => f.BookingId == booking.BookingId && f.Category == "Discount")
                .SumAsync(f => f.UnitPrice * f.Quantity);
            var bookedServices = await _context.FolioItems
                .Where(f => f.BookingId == booking.BookingId && f.Category == "Service")
                .SumAsync(f => f.UnitPrice * f.Quantity);
            var paid = await _context.Payments
                .Where(p => p.BookingId == booking.BookingId && p.PaymentStatus == "Success")
                .SumAsync(p => p.Amount);
            var pending = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == booking.BookingId && p.PaymentMethod == "Cash" && p.PaymentStatus == "Pending");
            if (pending == null) return;
            var due = booking.TotalPrice + bookedServices - discounts - paid;
            if (due <= 0) pending.PaymentStatus = "Failed";
            else pending.Amount = due;
        }

        private async Task<string?> ApplyPromo(Booking booking, string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            if (await _context.FolioItems.AnyAsync(f => f.BookingId == booking.BookingId && f.Category == "Discount"))
                return null;
            var discount = await DiscountAmount(code, booking.TotalPrice);
            if (discount.Error != null) return discount.Error;
            if (discount.Amount <= 0) return null;
            _context.FolioItems.Add(new FolioItem
            {
                BookingId = booking.BookingId,
                Category = "Discount",
                Description = $"Mã {code.Trim().ToUpperInvariant()}",
                Quantity = 1,
                UnitPrice = discount.Amount,
                CreatedAt = DateTime.UtcNow
            });
            return null;
        }

        private async Task<(decimal Amount, string? Error)> DiscountAmount(string? code, decimal roomTotal)
        {
            if (string.IsNullOrWhiteSpace(code)) return (0, null);
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Code == code.Trim().ToUpperInvariant() && p.IsActive);
            if (promo == null) return (0, "Mã giảm giá không dùng được.");
            var amount = promo.DiscountPercent > 0
                ? Math.Round(roomTotal * promo.DiscountPercent / 100m, 0)
                : promo.DiscountAmount;
            return (Math.Min(amount, roomTotal), null);
        }

        private async Task<FolioDto> BuildFolio(Booking booking)
        {
            var lines = await _context.FolioItems.Where(f => f.BookingId == booking.BookingId).ToListAsync();
            var payments = await _context.Payments.Where(p => p.BookingId == booking.BookingId && p.PaymentStatus == "Success").ToListAsync();
            decimal Sum(string category) => lines.Where(l => l.Category == category).Sum(l => l.UnitPrice * l.Quantity);
            var extras = Sum("Service") + Sum("Minibar") + Sum("Damage");
            var discounts = Sum("Discount");
            var paid = payments.Sum(p => p.Amount);
            var roomNet = Math.Max(0, booking.TotalPrice - discounts);
            var coveredBeforeCheckout = payments
                .Where(p => !(p.TransactionId ?? string.Empty).StartsWith("OUT-"))
                .Sum(p => p.Amount);
            var paidAtBooking = payments
                .Where(p => !IsDeskCollection(p.TransactionId))
                .Sum(p => p.Amount);
            var roomPaid = coveredBeforeCheckout >= roomNet;
            var due = booking.TotalPrice + extras - discounts - paid;
            var roomLink = booking.BookingRooms.FirstOrDefault();
            var note = roomPaid && paidAtBooking >= roomNet
                ? "Khách đã thanh toán tiền phòng khi đặt. Dịch vụ phát sinh ghi thêm vào bill. Chỉ thu thêm số tiền bên dưới."
                : roomPaid
                    ? "Tiền phòng đã được thu. Dịch vụ phát sinh ghi thêm vào bill. Chỉ thu thêm số tiền bên dưới."
                    : "Khách chưa thanh toán đủ tiền phòng. Bill gồm tiền phòng còn lại và dịch vụ.";
            return new FolioDto
            {
                BookingId = booking.BookingId,
                Status = booking.Status,
                GuestName = booking.GuestName ?? booking.Customer.FullName,
                GuestPhone = booking.GuestPhone ?? booking.Customer.PhoneNumber ?? "",
                GuestNote = booking.GuestNote,
                RoomId = roomLink?.RoomId ?? 0,
                RoomNumber = roomLink?.Room.RoomNumber ?? "",
                PropertyName = roomLink?.Room.Property.Name ?? "",
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfGuests = booking.NumberOfGuests,
                RoomCharge = booking.TotalPrice,
                Extras = extras,
                Discounts = discounts,
                Paid = paid,
                Due = due,
                RoomPaid = roomPaid,
                PaymentNote = note,
                Lines = lines.Select(l => new FolioLineDto
                {
                    Category = l.Category,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    Amount = l.UnitPrice * l.Quantity
                }).Concat(payments.Select(p => new FolioLineDto
                {
                    Category = "Paid",
                    Description = PaymentDescription(p),
                    Quantity = 1,
                    Amount = p.Amount
                })).ToList()
            };
        }

        private async Task<Booking?> LoadStay(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room).ThenInclude(r => r.Property)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room).ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        private async Task<HashSet<int>> BusyRoomIds(DateOnly checkIn, DateOnly checkOut, int? ignoreBookingId)
        {
            var ids = await _context.BookingRooms
                .Where(br => br.BookingId != ignoreBookingId)
                .Where(br => br.Booking.Status == "Pending"
                          || br.Booking.Status == "Confirmed"
                          || br.Booking.Status == "CheckedIn"
                          || br.Booking.Status == "CancelRequested")
                .Where(br => br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn)
                .Select(br => br.RoomId)
                .ToListAsync();
            return ids.ToHashSet();
        }

        private async Task<User> FindOrCreateGuest(string name, string phone, string? email)
        {
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (existing != null) return existing;
            var mail = string.IsNullOrWhiteSpace(email) ? $"walkin.{phone}@guest.hbs" : email.Trim();
            var taken = await _context.Users.AnyAsync(u => u.Email == mail);
            if (taken) mail = $"walkin.{phone}.{DateTime.UtcNow:HHmmss}@guest.hbs";
            var user = new User
            {
                FullName = name,
                Email = mail,
                PhoneNumber = phone,
                RoleId = await CustomerRoleId(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, Guid.NewGuid().ToString("N"));
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task<int> CustomerRoleId()
        {
            return await _context.Roles.Where(r => r.RoleName == "Customer").Select(r => r.RoleId).FirstAsync();
        }

        private static DeskRoomDto MapRoom(Room room)
        {
            var plan = OccupancyPricing.For(room.RoomType.TypeName, room.RoomType.MaxGuests);
            return new DeskRoomDto
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                TypeName = room.RoomType.TypeName,
                PropertyName = room.Property.Name,
                StayLabel = plan.Label,
                BasePrice = room.RoomType.BasePrice,
                Status = room.Status,
                HousekeepingStatus = string.IsNullOrWhiteSpace(room.HousekeepingStatus) ? "Clean" : room.HousekeepingStatus
            };
        }

        private static string Digits(string? value) => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

        private static string? CleanNote(string? note) => string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        private static string JoinNote(string? current, string extra)
        {
            var text = string.IsNullOrWhiteSpace(current) ? extra : current.Trim() + " " + extra;
            return text.Length <= 500 ? text : text[..500];
        }

        private static bool IsDeskCollection(string? transactionId)
        {
            var id = transactionId ?? string.Empty;
            return id.StartsWith("DEP-") || id.StartsWith("OUT-") || id.StartsWith("CASH-");
        }

        private static string PaymentDescription(Payment payment)
        {
            var id = payment.TransactionId ?? string.Empty;
            var method = PayLabel(payment.PaymentMethod);
            if (id.StartsWith("DEP-")) return "Tiền cọc · " + method;
            if (id.StartsWith("OUT-")) return "Thu khi trả phòng · " + method;
            if (id.StartsWith("CASH-")) return "Tiền phòng khi nhận phòng · " + method;
            return "Thanh toán khi đặt phòng · " + method;
        }

        private static string PayLabel(string method) => method switch
        {
            "Cash" => "Tiền mặt",
            "Card" => "Thẻ",
            "Transfer" => "Chuyển khoản",
            "Wallet" => "Ví điện tử",
            _ => method
        };
    }
}
