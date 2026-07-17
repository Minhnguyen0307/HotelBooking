using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class RoomTypeService : IRoomTypeService
    {
        private readonly HotelBookingDbContext _context;

        public RoomTypeService(HotelBookingDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoomType>> GetAllRoomTypesAsync()
        {
            return await _context.RoomTypes
                .OrderBy(rt => rt.TypeName)
                .ToListAsync();
        }

        public async Task<RoomType?> GetRoomTypeByIdAsync(int id)
        {
            return await _context.RoomTypes.FindAsync(id);
        }

        public async Task<(bool Success, string? Error)> SaveRoomTypeAsync(RoomType roomType)
        {
            // BR-25: Room type name must be unique
            bool duplicate = await _context.RoomTypes
                .AnyAsync(rt => rt.TypeName == roomType.TypeName && rt.RoomTypeId != roomType.RoomTypeId);
            if (duplicate)
                return (false, "Tên loại phòng này đã tồn tại.");

            if (roomType.RoomTypeId == 0)
            {
                roomType.IsActive = true;
                _context.RoomTypes.Add(roomType);
            }
            else
            {
                var existing = await _context.RoomTypes.FindAsync(roomType.RoomTypeId);
                if (existing == null) return (false, "Loại phòng không tồn tại.");

                existing.TypeName = roomType.TypeName;
                existing.Description = roomType.Description;
                existing.BasePrice = roomType.BasePrice;
                existing.MaxGuests = roomType.MaxGuests;
                existing.IsActive = roomType.IsActive;
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteRoomTypeAsync(int id)
        {
            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.RoomTypeId == id);

            if (roomType == null) return (false, "Loại phòng không tồn tại.");

            // BR-15: A room type cannot be deleted if it is assigned to existing rooms
            if (roomType.Rooms.Any())
                return (false, "Không thể xóa loại phòng này vì đang được áp dụng cho một số phòng trong hệ thống.");

            _context.RoomTypes.Remove(roomType);
            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}
