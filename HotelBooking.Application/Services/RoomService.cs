using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class RoomService : IRoomService
    {
        private readonly HotelBookingDbContext _context;

        public RoomService(HotelBookingDbContext context)
        {
            _context = context;
        }
        public async Task<List<RoomListItemDto>> SearchAvailableRoomsAsync(RoomSearchDto criteria)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (criteria.CheckInDate < today)
                throw new ArgumentException("Ngày nhận phòng không được là ngày trong quá khứ.");

            if (criteria.CheckOutDate <= criteria.CheckInDate)
                throw new ArgumentException("Ngày trả phòng phải sau ngày nhận phòng ít nhất 1 ngày.");


            var checkIn = criteria.CheckInDate;
            var checkOut = criteria.CheckOutDate;

            var query = _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Where(r => r.Status != "Maintenance");

            if (criteria.RoomTypeId.HasValue)
                query = query.Where(r => r.RoomTypeId == criteria.RoomTypeId.Value);

            if (criteria.Guests.HasValue)
                query = query.Where(r => r.RoomType.MaxGuests >= criteria.Guests.Value);

            // Loại các phòng đã bị đặt trùng khoảng ngày (overlap logic giống sp_SearchAvailableRooms)
            var bookedRoomIds = _context.BookingRooms
                .Where(br => br.Booking.Status == "Pending"
                          || br.Booking.Status == "Confirmed"
                          || br.Booking.Status == "CheckedIn")
                .Where(br => br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn)
                .Select(br => br.RoomId);

            query = query.Where(r => !bookedRoomIds.Contains(r.RoomId));

            var result = await query
                .Select(r => new RoomListItemDto
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    TypeName = r.RoomType.TypeName,
                    BasePrice = r.RoomType.BasePrice,
                    MaxGuests = r.RoomType.MaxGuests,
                    PrimaryImageUrl = r.RoomImages
                        .Where(i => i.IsPrimary)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),
                    Address = r.Address ?? "123 Võ Nguyên Giáp, Quận Sơn Trà, Đà Nẵng"
                })
                .ToListAsync();

            return result;
        }

        public async Task<RoomDetailDto?> GetRoomDetailAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Include(r => r.Amenities)          
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null) return null;

            return new RoomDetailDto
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                TypeName = room.RoomType.TypeName,
                Description = room.Description ?? room.RoomType.Description,
                BasePrice = room.RoomType.BasePrice,
                MaxGuests = room.RoomType.MaxGuests,
                ImageUrls = room.RoomImages.Select(i => i.ImageUrl).ToList(),
                Amenities = room.Amenities.Select(a => a.Name).ToList(),
                Address = room.Address ?? "123 Võ Nguyên Giáp, Quận Sơn Trà, Đà Nẵng"
            };
        }
        public async Task<List<(int RoomTypeId, string TypeName)>> GetAllRoomTypesAsync()
        {
            return await _context.RoomTypes
                .Where(rt => rt.IsActive)
                .Select(rt => new ValueTuple<int, string>(rt.RoomTypeId, rt.TypeName))
                .ToListAsync();
        }
    }
}
