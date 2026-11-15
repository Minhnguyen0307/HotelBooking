using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
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
    }
}
