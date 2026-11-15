using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly HotelBookingDbContext _context;
    private readonly IConfiguration _config;

    public AuthApiController(
        IAuthService authService,
        IEmailService emailService,
        HotelBookingDbContext context,
        IConfiguration config)
    {
        _authService = authService;
        _emailService = emailService;
        _context = context;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { message = "Đăng ký thành công. Vui lòng đăng nhập." });
    }

    [HttpGet("google-client")]
    public IActionResult GoogleClient()
    {
        var clientId = _config["Google:ClientId"];
        return Ok(new { clientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId });
    }

    [HttpPost("google")]
    public async Task<IActionResult> Google(GoogleLoginRequest request)
    {
        var clientId = _config["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            return BadRequest(new { message = "Chưa cấu hình Google Client ID trên server." });
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(new { message = "Thiếu mã đăng nhập Google." });

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            });
        }
        catch
        {
            return Unauthorized(new { message = "Không xác thực được tài khoản Google." });
        }

        if (string.IsNullOrWhiteSpace(payload.Email) || payload.EmailVerified != true)
            return Unauthorized(new { message = "Email Google chưa được xác minh." });

        var email = payload.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null)
        {
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            if (customerRole == null)
                return BadRequest(new { message = "Chưa cấu hình vai trò Customer." });

            user = new User
            {
                FullName = string.IsNullOrWhiteSpace(payload.Name) ? email : payload.Name,
                Email = email,
                RoleId = customerRole.RoleId,
                IsActive = true,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, Guid.NewGuid().ToString("N"));
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            user = await _context.Users.Include(u => u.Role).FirstAsync(u => u.UserId == user.UserId);
        }

        if (!user.IsActive)
            return Unauthorized(new { message = "Tài khoản đã bị vô hiệu hóa." });

        return Ok(new
        {
            token = CreateToken(user),
            user = ToPublicUser(user)
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (!result.Success)
            return Unauthorized(new { message = result.ErrorMessage });

        var user = await _context.Users.Include(u => u.Role)
            .FirstAsync(u => u.Email == dto.Email);

        return Ok(new
        {
            token = CreateToken(user),
            user = ToPublicUser(user)
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == ApiUser.Id(User));
        if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản." });
        return Ok(ToPublicUser(user));
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var profile = await _authService.GetProfileAsync(ApiUser.Id(User));
        if (profile == null) return NotFound(new { message = "Không tìm thấy hồ sơ." });
        return Ok(profile);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(ProfileDto dto)
    {
        var result = await _authService.UpdateProfileAsync(ApiUser.Id(User), dto);
        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "Cập nhật thông tin thành công." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var result = await _authService.ChangePasswordAsync(ApiUser.Id(User), dto);
        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var token = await _authService.GeneratePasswordResetTokenAsync(dto.Email);
        string? resetLink = null;
        var emailSent = false;

        if (token != null)
        {
            var frontend = _config["Frontend:BaseUrl"] ?? "http://localhost:3000";
            resetLink = $"{frontend}/reset?email={Uri.EscapeDataString(dto.Email)}&token={Uri.EscapeDataString(token)}";
            try
            {
                await _emailService.SendPasswordResetEmailAsync(dto.Email, resetLink);
                emailSent = true;
            }
            catch
            {
                emailSent = false;
            }
        }

        return Ok(new
        {
            message = "Nếu địa chỉ email chính xác, một liên kết khôi phục mật khẩu đã được gửi đến email của bạn.",
            emailSent,
            resetLink = emailSent ? null : resetLink
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var result = await _authService.ResetPasswordAsync(dto);
        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "Mật khẩu đã được đặt lại. Vui lòng đăng nhập." });
    }

    private string CreateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var hours = int.TryParse(_config["Jwt:ExpireHours"], out var parsed) ? parsed : 8;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.RoleName)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static object ToPublicUser(User user) => new
    {
        user.UserId,
        user.FullName,
        user.Email,
        user.PhoneNumber,
        role = user.Role.RoleName
    };

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }
}
