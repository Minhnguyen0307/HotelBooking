using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Infrastructure;

namespace HotelBooking.Application.Interfaces
{
    public interface IRoomTypeService
    {
        Task<List<RoomType>> GetAllRoomTypesAsync();
        Task<RoomType?> GetRoomTypeByIdAsync(int id);
        Task<(bool Success, string? Error)> SaveRoomTypeAsync(RoomType roomType);
        Task<(bool Success, string? Error)> DeleteRoomTypeAsync(int id);
    }
}
