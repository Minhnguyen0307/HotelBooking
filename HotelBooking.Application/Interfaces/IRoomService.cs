using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IRoomService
    {
        Task<List<RoomListItemDto>> SearchAvailableRoomsAsync(RoomSearchDto criteria);
        Task<List<RoomListItemDto>> GetShowcaseRoomsAsync();
        Task<List<RoomListItemDto>> GetPropertyRoomsAsync(RoomSearchDto criteria);
        Task<RoomDetailDto?> GetRoomDetailAsync(int roomId);
        Task<List<(int RoomTypeId, string TypeName)>> GetAllRoomTypesAsync();
        Task<List<PropertyDto>> GetPropertiesAsync();
    }
}