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

        public async Task<(bool Success, string? Error)> ToggleCustomerActiveStatusAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "Khách hàng không tồn tại.");

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}
