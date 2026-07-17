using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class RoomManageService : IRoomManageService
    {
        private readonly HotelBookingDbContext _context;

        public RoomManageService(HotelBookingDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoomManageListItemDto>> GetAllRoomsAsync()
        {
            return await _context.Rooms
                .Include(r => r.RoomType)
                .OrderBy(r => r.RoomNumber)
                .Select(r => new RoomManageListItemDto
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    TypeName = r.RoomType.TypeName,
                    Status = r.Status,
                    Floor = r.Floor
                })
                .ToListAsync();
        }

        public async Task<RoomUpsertDto?> GetRoomForEditAsync(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return null;

            return new RoomUpsertDto
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                RoomTypeId = room.RoomTypeId,
                Floor = room.Floor,
                Status = room.Status,
                Description = room.Description,
                Address = room.Address
            };
        }

        public async Task<(bool Success, string? Error)> SaveRoomAsync(RoomUpsertDto dto)
        {
            // Kiểm tra trùng số phòng
            bool duplicate = await _context.Rooms
                .AnyAsync(r => r.RoomNumber == dto.RoomNumber && r.RoomId != dto.RoomId);
            if (duplicate)
                return (false, "Số phòng này đã tồn tại.");

            if (dto.RoomId.HasValue)
            {
                var room = await _context.Rooms.FindAsync(dto.RoomId.Value);
                if (room == null) return (false, "Phòng không tồn tại.");

                room.RoomNumber = dto.RoomNumber;
                room.RoomTypeId = dto.RoomTypeId;
                room.Floor = dto.Floor;
                room.Status = dto.Status;
                room.Description = dto.Description;
                room.Address = dto.Address ?? "123 Võ Nguyên Giáp, Quận Sơn Trà, Đà Nẵng";
                room.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var room = new Room
                {
                    RoomNumber = dto.RoomNumber,
                    RoomTypeId = dto.RoomTypeId,
                    Floor = dto.Floor,
                    Status = dto.Status,
                    Description = dto.Description,
                    Address = dto.Address ?? "123 Võ Nguyên Giáp, Quận Sơn Trà, Đà Nẵng",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Rooms.Add(room);
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateStatusAsync(int roomId, string status)
        {
            var validStatuses = new[] { "Available", "Booked", "Maintenance" };
            if (!validStatuses.Contains(status))
                return (false, "Trạng thái không hợp lệ.");

            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return (false, "Phòng không tồn tại.");

            room.Status = status;
            room.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteRoomAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.BookingRooms)
                .ThenInclude(br => br.Booking)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null) return (false, "Phòng không tồn tại.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            bool hasActiveBookings = room.BookingRooms
                .Any(br => (br.Booking.Status == "Pending" || br.Booking.Status == "Confirmed" || br.Booking.Status == "CheckedIn")
                           && br.Booking.CheckOutDate >= today);

            if (hasActiveBookings)
                return (false, "Không thể xóa phòng này vì đang có đơn đặt phòng hoạt động hoặc trong tương lai.");

            var roomImages = _context.RoomImages.Where(ri => ri.RoomId == roomId);
            _context.RoomImages.RemoveRange(roomImages);

            var bookingRooms = _context.BookingRooms.Where(br => br.RoomId == roomId);
            _context.BookingRooms.RemoveRange(bookingRooms);

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}
