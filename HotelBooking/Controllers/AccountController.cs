using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Services;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace HotelBooking.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly HotelBookingDbContext _context;
    private readonly IEmailService _emailService;
    public AccountController(IAuthService authService, HotelBookingDbContext context, IEmailService emailService)
    {
        _authService = authService;
        _context = context;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _authService.RegisterAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.ErrorMessage!);
            return View(dto);
        }

        TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _authService.LoginAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.ErrorMessage!);
            return View(dto);
        }

        var user = await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user!.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.RoleName)
    };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = dto.RememberMe });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    private int? CurrentUserId =>
        User.Identity != null && User.Identity.IsAuthenticated
        ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        : null;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = CurrentUserId;
        if (!userId.HasValue) return RedirectToAction("Login");
        var profile = await _authService.GetProfileAsync(userId.Value);
        if (profile == null) return NotFound();
        return View(profile);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Profile(ProfileDto dto)
    {
        var userId = CurrentUserId;
        if (!userId.HasValue) return RedirectToAction("Login");
        if (!ModelState.IsValid) return View(dto);

        var result = await _authService.UpdateProfileAsync(userId.Value, dto);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.ErrorMessage!);
            return View(dto);
        }

        TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
        return RedirectToAction("Profile");
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = CurrentUserId;
        if (!userId.HasValue) return RedirectToAction("Login");
        if (!ModelState.IsValid) return View(dto);

        var result = await _authService.ChangePasswordAsync(userId.Value, dto);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.ErrorMessage!);
            return View(dto);
        }

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
        return RedirectToAction("Profile");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var token = await _authService.GeneratePasswordResetTokenAsync(dto.Email);
        if (token != null)
        {
            var resetLink = Url.Action("ResetPassword", "Account", new { email = dto.Email, token = token }, Request.Scheme);
            await _emailService.SendPasswordResetEmailAsync(dto.Email, resetLink!);
        }

        TempData["SuccessMessage"] = "Nếu địa chỉ email chính xác, một liên kết khôi phục mật khẩu đã được gửi đến email của bạn.";
        return RedirectToAction("ForgotPassword");
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        return View(new ResetPasswordDto { Email = email, Token = token });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _authService.ResetPasswordAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.ErrorMessage!);
            return View(dto);
        }

        TempData["SuccessMessage"] = "Mật khẩu của bạn đã được đặt lại thành công. Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }
}