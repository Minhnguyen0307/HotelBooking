using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterDto dto);
        Task<AuthResult> LoginAsync(LoginDto dto);
        Task<ProfileDto?> GetProfileAsync(int userId);
        Task<AuthResult> UpdateProfileAsync(int userId, ProfileDto dto);
        Task<AuthResult> ChangePasswordAsync(int userId, ChangePasswordDto dto);
        Task<string?> GeneratePasswordResetTokenAsync(string email);
        Task<AuthResult> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
