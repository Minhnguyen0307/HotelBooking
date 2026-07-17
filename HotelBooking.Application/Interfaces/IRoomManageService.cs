using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IRoomManageService
    {
        Task<List<RoomManageListItemDto>> GetAllRoomsAsync();
        Task<RoomUpsertDto?> GetRoomForEditAsync(int roomId);
        Task<(bool Success, string? Error)> SaveRoomAsync(RoomUpsertDto dto);
        Task<(bool Success, string? Error)> UpdateStatusAsync(int roomId, string status);
        Task<(bool Success, string? Error)> DeleteRoomAsync(int roomId);
    }
}
