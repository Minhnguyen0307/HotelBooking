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

            if (criteria.PropertyId.HasValue)
                query = query.Where(r => r.PropertyId == criteria.PropertyId.Value);

            var bookedRoomIds = _context.BookingRooms
                .Where(br => br.Booking.Status == "Pending"
                          || br.Booking.Status == "Confirmed"
                          || br.Booking.Status == "CheckedIn"
                          || br.Booking.Status == "CancelRequested")
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
                    Address = r.Property.Address,
                    PropertyId = r.PropertyId,
                    PropertyName = r.Property.Name,
                    Area = r.Property.Area,
                    IsAvailable = true
                })
                .ToListAsync();

            foreach (var room in result)
                ApplyStay(room);

            if (criteria.Guests.HasValue)
                result = result.Where(r => r.MaxGuests >= criteria.Guests.Value).ToList();

            return result;
        }

        public async Task<List<RoomListItemDto>> GetShowcaseRoomsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var rooms = await ShowcaseQuery().ToListAsync();
            var booked = await BookedRoomIdsAsync(today, today.AddDays(1));

            return rooms
                .GroupBy(r => r.PropertyId)
                .Select(group => group
                    .OrderByDescending(r => ShowcaseRank(r.RoomType.TypeName))
                    .ThenByDescending(r => r.RoomType.BasePrice)
                    .First())
                .OrderBy(r => r.PropertyId)
                .Select(r => MapRoom(r, !booked.Contains(r.RoomId)))
                .ToList();
        }

        public async Task<List<RoomListItemDto>> GetPropertyRoomsAsync(RoomSearchDto criteria)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (!criteria.PropertyId.HasValue)
                throw new ArgumentException("Hãy chọn một cơ sở.");
            if (criteria.CheckInDate < today)
                throw new ArgumentException("Ngày nhận phòng không được là ngày trong quá khứ.");
            if (criteria.CheckOutDate <= criteria.CheckInDate)
                throw new ArgumentException("Ngày trả phòng phải sau ngày nhận phòng ít nhất 1 ngày.");

            var rooms = await ShowcaseQuery()
                .Where(r => r.PropertyId == criteria.PropertyId.Value)
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();
            var booked = await BookedRoomIdsAsync(criteria.CheckInDate, criteria.CheckOutDate);

            return rooms
                .Select(r => MapRoom(r, !booked.Contains(r.RoomId)))
                .ToList();
        }

        private IQueryable<Room> ShowcaseQuery()
        {
            return _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.Property)
                .Include(r => r.RoomImages)
                .Where(r => r.Status != "Maintenance");
        }

        private async Task<HashSet<int>> BookedRoomIdsAsync(DateOnly checkIn, DateOnly checkOut)
        {
            var ids = await _context.BookingRooms
                .Where(br => br.Booking.Status == "Pending"
                          || br.Booking.Status == "Confirmed"
                          || br.Booking.Status == "CheckedIn"
                          || br.Booking.Status == "CancelRequested")
                .Where(br => br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn)
                .Select(br => br.RoomId)
                .ToListAsync();
            return ids.ToHashSet();
        }

        private static int ShowcaseRank(string typeName)
        {
            var name = typeName.ToLowerInvariant();
            if (name.Contains("vip") || name.Contains("suite")) return 3;
            if (name.Contains("deluxe")) return 2;
            return 1;
        }

        private static RoomListItemDto MapRoom(Room room, bool isAvailable)
        {
            var dto = new RoomListItemDto
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                TypeName = room.RoomType.TypeName,
                BasePrice = room.RoomType.BasePrice,
                MaxGuests = room.RoomType.MaxGuests,
                PrimaryImageUrl = room.RoomImages
                    .Where(i => i.IsPrimary)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
                    ?? room.RoomImages.Select(i => i.ImageUrl).FirstOrDefault(),
                Address = room.Property.Address,
                PropertyId = room.PropertyId,
                PropertyName = room.Property.Name,
                Area = room.Property.Area,
                IsAvailable = isAvailable
            };
            ApplyStay(dto);
            return dto;
        }

        private static void ApplyStay(RoomListItemDto room)
        {
            var plan = OccupancyPricing.For(room.TypeName, room.MaxGuests);
            room.StayLabel = plan.Label;
            room.IncludedGuests = plan.IncludedGuests;
            room.ExtraGuestFee = plan.ExtraGuestFeePerNight;
            room.MaxGuests = plan.Capacity;
            room.OccupancyRule = plan.Rule;
        }

        public async Task<RoomDetailDto?> GetRoomDetailAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.Property)
                .Include(r => r.RoomImages)
                .Include(r => r.Amenities)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null) return null;

            var plan = OccupancyPricing.For(room.RoomType.TypeName, room.RoomType.MaxGuests);
            return new RoomDetailDto
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                TypeName = room.RoomType.TypeName,
                Description = room.Description ?? room.RoomType.Description,
                BasePrice = room.RoomType.BasePrice,
                MaxGuests = plan.Capacity,
                StayLabel = plan.Label,
                IncludedGuests = plan.IncludedGuests,
                ExtraGuestFee = plan.ExtraGuestFeePerNight,
                OccupancyRule = plan.Rule,
                ImageUrls = room.RoomImages.Select(i => i.ImageUrl).ToList(),
                Amenities = room.Amenities.Select(a => a.Name).ToList(),
                Address = room.Property.Address,
                PropertyId = room.PropertyId,
                PropertyName = room.Property.Name,
                Area = room.Property.Area
            };
        }

        public async Task<List<PropertyDto>> GetPropertiesAsync()
        {
            return await _context.Properties
                .OrderBy(p => p.PropertyId)
                .Select(p => new PropertyDto
                {
                    PropertyId = p.PropertyId,
                    Name = p.Name,
                    Area = p.Area,
                    Address = p.Address,
                    Description = p.Description,
                    RoomCount = p.Rooms.Count()
                })
                .ToListAsync();
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
