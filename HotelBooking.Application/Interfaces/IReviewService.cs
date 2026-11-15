using System.Threading.Tasks;
using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IReviewService
    {
        Task<RoomReviewsSummaryDto> GetRoomReviewsSummaryAsync(int roomId);
        Task<(bool Success, string? Error)> AddReviewAsync(int userId, CreateReviewDto dto, string webRootPath);
        Task<bool> CanUserReviewAsync(int userId, int roomId);
        Task<(bool Success, string? Error)> ReplyReviewAsync(int staffUserId, int reviewId, string response);
        Task<List<ReviewItemDto>> GetAllReviewsAsync();
        Task<(bool Success, string? Error)> DeleteReviewAsync(int reviewId);
    }
}

