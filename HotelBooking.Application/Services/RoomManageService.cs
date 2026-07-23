using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class RoomManageService :  IRoomManageService
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

        public async Task<(bool Success, string? Error)> SaveRoomAsync(RoomUpsertDto dto, string? imagePath)
        {
            bool duplicate = await _context.Rooms
                .AnyAsync(r => r.RoomNumber == dto.RoomNumber && r.RoomId != dto.RoomId);
            if (duplicate)
                return (false, "Số phòng này đã tồn tại.");

            Room room;

            if (dto.RoomId.HasValue)
            {
                var existingRoom = await _context.Rooms
                    .Include(r => r.RoomImages)
                    .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId.Value);
                if (existingRoom == null) return (false, "Phòng không tồn tại.");

                existingRoom.RoomNumber = dto.RoomNumber;
                existingRoom.RoomTypeId = dto.RoomTypeId;
                existingRoom.Floor = dto.Floor;
                existingRoom.Status = dto.Status;
                existingRoom.Description = dto.Description;
                existingRoom.Address = dto.Address ?? "123 Võ Nguyên Giáp, phường Sơn Trà, TP Đà Nẵng";
                existingRoom.UpdatedAt = DateTime.UtcNow;

                room = existingRoom;

                if (imagePath != null)
                {
                    foreach (var img in room.RoomImages) img.IsPrimary = false;
                    room.RoomImages.Add(new RoomImage { ImageUrl = imagePath, IsPrimary = true });
                }
            }
            else
            {
                var newRoom = new Room
                {
                    RoomNumber = dto.RoomNumber,
                    RoomTypeId = dto.RoomTypeId,
                    Floor = dto.Floor,
                    Status = dto.Status,
                    Description = dto.Description,
                    Address = dto.Address ?? "123 Võ Nguyên Giáp, phường Sơn Trà, TP Đà Nẵng",
                    CreatedAt = DateTime.UtcNow
                };

                if (imagePath != null)
                {
                    newRoom.RoomImages.Add(new RoomImage { ImageUrl = imagePath, IsPrimary = true });
                }

                _context.Rooms.Add(newRoom);
                room = newRoom;
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
