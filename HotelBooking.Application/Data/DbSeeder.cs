using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(HotelBookingDbContext context)
        {
            var hasher = new PasswordHasher<User>();

            var roleNames = new[] { "Customer", "Receptionist", "Manager", "Admin" };
            foreach (var roleName in roleNames)
            {
                if (!await context.Roles.AnyAsync(r => r.RoleName == roleName))
                {
                    context.Roles.Add(new Role { RoleName = roleName });
                }
            }
            await context.SaveChangesAsync();

            var roles = await context.Roles.ToDictionaryAsync(r => r.RoleName, r => r.RoleId);

            var seedUsers = new (string Email, string Password, string FullName, string RoleName)[]
            {
                ("admin@hotel.com",       "Admin@123",       "System Admin",       "Admin"),
                ("manager@hotel.com",     "Manager@123",     "Hotel Manager",      "Manager"),
                ("receptionist@hotel.com","Reception@123",   "Front Desk Staff",   "Receptionist"),
                ("customer1@hotel.com",   "Customer@123",    "Nguyen Van A",       "Customer"),
                ("customer2@hotel.com",   "Customer@123",    "Tran Thi B",         "Customer"),
            };

            foreach (var seed in seedUsers)
            {
                bool exists = await context.Users.AnyAsync(u => u.Email == seed.Email);
                if (exists) continue; 

                var user = new User
                {
                    FullName = seed.FullName,
                    Email = seed.Email,
                    PhoneNumber = null,
                    RoleId = roles[seed.RoleName],
                    IsActive = true,
                    FailedLoginCount = 0,
                    CreatedAt = DateTime.UtcNow
                };
                user.PasswordHash = hasher.HashPassword(user, seed.Password);

                context.Users.Add(user);
            }

            await context.SaveChangesAsync();
        }
    }
}