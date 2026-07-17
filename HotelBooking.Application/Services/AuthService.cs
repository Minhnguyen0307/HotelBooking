using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly HotelBookingDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public AuthService(HotelBookingDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<AuthResult> RegisterAsync(RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return new AuthResult { Success = false, ErrorMessage = "Mật khẩu xác nhận không khớp." };

            // BR-01: Email phải duy nhất
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailExists)
                return new AuthResult { Success = false, ErrorMessage = "Email đã được sử dụng." };

            // Mặc định gán role Customer (RoleId = 1 theo seed data)
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            if (customerRole == null)
                return new AuthResult { Success = false, ErrorMessage = "Chưa cấu hình Role Customer trong hệ thống." };

            var newUser = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                RoleId = customerRole.RoleId,
                IsActive = true,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, dto.Password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return new AuthResult { Success = true };
        }

        public async Task<AuthResult> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return new AuthResult { Success = false, ErrorMessage = "Email hoặc mật khẩu không đúng." };

            if (!user.IsActive)
                return new AuthResult { Success = false, ErrorMessage = "Tài khoản đã bị vô hiệu hóa." };

            // BR-02: Kiểm tra khóa tài khoản
            if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = $"Tài khoản tạm khóa đến {user.LockoutUntil:HH:mm dd/MM/yyyy}."
                };

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= MaxFailedAttempts)
                {
                    user.LockoutUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    user.FailedLoginCount = 0;
                }
                await _context.SaveChangesAsync();
                return new AuthResult { Success = false, ErrorMessage = "Email hoặc mật khẩu không đúng." };
            }

            // Đăng nhập thành công -> reset counter
            user.FailedLoginCount = 0;
            user.LockoutUntil = null;
            await _context.SaveChangesAsync();

            return new AuthResult { Success = true };
        }

        public async Task<ProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            return new ProfileDto
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email
            };
        }

        public async Task<AuthResult> UpdateProfileAsync(int userId, ProfileDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new AuthResult { Success = false, ErrorMessage = "Tài khoản không tồn tại." };

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return new AuthResult { Success = true };
        }

        public async Task<AuthResult> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return new AuthResult { Success = false, ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp." };

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new AuthResult { Success = false, ErrorMessage = "Tài khoản không tồn tại." };

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.OldPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
                return new AuthResult { Success = false, ErrorMessage = "Mật khẩu cũ không chính xác." };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return new AuthResult { Success = true };
        }

        public async Task<string?> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;

            var token = Guid.NewGuid().ToString("N");
            var prt = new PasswordResetToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.PasswordResetTokens.Add(prt);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<AuthResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return new AuthResult { Success = false, ErrorMessage = "Mật khẩu xác nhận không khớp." };

            var tokenRecord = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == dto.Token && t.User.Email == dto.Email);

            if (tokenRecord == null || tokenRecord.IsUsed || tokenRecord.ExpiresAt < DateTime.UtcNow)
                return new AuthResult { Success = false, ErrorMessage = "Mã xác thực không hợp lệ hoặc đã hết hạn." };

            var user = tokenRecord.User;
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            tokenRecord.IsUsed = true;

            await _context.SaveChangesAsync();
            return new AuthResult { Success = true };
        }
    }
}