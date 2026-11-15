using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly HotelBookingDbContext _context;

        public CustomerService(HotelBookingDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllCustomersAsync()
        {
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            if (customerRole == null) return new List<User>();

            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == customerRole.RoleId)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.RoleId)
                .ThenBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles
                .OrderBy(r => r.RoleId)
                .ToListAsync();
        }

        public async Task<(bool Success, string? Error)> ChangeUserRoleAsync(int userId, int newRoleId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "Người dùng không tồn tại.");

            var role = await _context.Roles.FindAsync(newRoleId);
            if (role == null) return (false, "Vai trò không hợp lệ.");

            user.RoleId = newRoleId;
            user.UpdatedAt = System.DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ToggleCustomerActiveStatusAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "Người dùng không tồn tại.");

            user.IsActive = !user.IsActive;
            user.UpdatedAt = System.DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, null);
        }

        private static readonly string[] StaffRoles = ["Receptionist", "Manager", "Admin"];

        public async Task<List<User>> GetStaffAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => StaffRoles.Contains(u.Role.RoleName))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<(bool Success, string? Error)> CreateStaffAsync(string fullName, string email, string? phone, string roleName, string password)
        {
            var name = (fullName ?? string.Empty).Trim();
            var mail = (email ?? string.Empty).Trim();
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (name.Length < 2) return (false, "Nhập họ tên nhân viên.");
            if (mail.Length < 5 || !mail.Contains('@')) return (false, "Email nhân viên chưa đúng.");
            if (digits.Length > 0 && (digits.Length != 10 || !digits.StartsWith('0')))
                return (false, "Số điện thoại cần 10 số và bắt đầu bằng 0.");
            if ((password ?? string.Empty).Length < 8) return (false, "Mật khẩu tối thiểu 8 ký tự.");
            if (!StaffRoles.Contains(roleName)) return (false, "Chọn vai trò lễ tân, quản lý hoặc quản trị.");
            if (await _context.Users.AnyAsync(u => u.Email == mail)) return (false, "Email đã được sử dụng.");

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role == null) return (false, "Vai trò không tồn tại.");
            var user = new User
            {
                FullName = name,
                Email = mail,
                PhoneNumber = digits.Length == 0 ? null : digits,
                RoleId = role.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password!);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateStaffAsync(int userId, int actorId, string fullName, string? phone, string roleName, bool isActive)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || !StaffRoles.Contains(user.Role.RoleName)) return (false, "Không tìm thấy nhân viên.");
            var name = (fullName ?? string.Empty).Trim();
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (name.Length < 2) return (false, "Nhập họ tên nhân viên.");
            if (digits.Length > 0 && (digits.Length != 10 || !digits.StartsWith('0')))
                return (false, "Số điện thoại cần 10 số và bắt đầu bằng 0.");
            if (!StaffRoles.Contains(roleName)) return (false, "Chọn vai trò lễ tân, quản lý hoặc quản trị.");
            if (userId == actorId && (!isActive || roleName != user.Role.RoleName))
                return (false, "Không tự khóa hoặc đổi vai trò của chính mình.");

            var role = await _context.Roles.FirstAsync(r => r.RoleName == roleName);
            user.FullName = name;
            user.PhoneNumber = digits.Length == 0 ? null : digits;
            user.RoleId = role.RoleId;
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}
