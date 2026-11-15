using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IDeskService
    {
        Task<List<ServiceOfferDto>> GetServicesAsync();
        Task<List<DeskRoomDto>> GetRoomsAsync();
        Task<List<DeskRoomDto>> AvailableRoomsAsync(DateOnly checkIn, DateOnly checkOut, int? ignoreBookingId);
        Task<List<DeskIssueDto>> OpenIssuesAsync();
        Task<(bool Success, string? Error)> SetHousekeepingAsync(int roomId, string status);
        Task<(bool Success, string? Error)> ReportIssueAsync(int roomId, string description);
        Task<(bool Success, string? Error)> ResolveIssueAsync(int issueId);
        Task<List<DeskGuestDto>> SearchGuestsAsync(string? query);
        Task<(bool Success, string? Error)> UpdateGuestAsync(int userId, string fullName, string? phone, string? email);
        Task<(bool Success, string? Error, int? BookingId)> CreateWalkInAsync(WalkInDto dto);
        Task<FolioDto?> GetFolioAsync(int bookingId);
        Task<(bool Success, string? Error)> UpdateStayAsync(int bookingId, StayUpdateDto dto);
        Task<(bool Success, string? Error)> ChangeRoomAsync(int bookingId, int roomId);
        Task<(bool Success, string? Error)> AddChargeAsync(int bookingId, ChargeDto dto);
        Task<(bool Success, string? Error)> AddDepositAsync(int bookingId, DepositDto dto);
        Task<(bool Success, string? Error)> CheckInConfirmAsync(int bookingId, CheckInDto dto);
        Task<(bool Success, string? Error)> CheckOutWithBillAsync(int bookingId, CheckoutDto dto, int staffUserId);
    }
}
