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

            // Seed Rooms & Amenities if no rooms exist
            if (!await context.Rooms.AnyAsync())
            {
                // Ensure amenities exist
                var wifi = await context.Amenities.FirstOrDefaultAsync(a => a.Name == "WiFi") ?? new Amenity { Name = "WiFi" };
                var tv = await context.Amenities.FirstOrDefaultAsync(a => a.Name == "TV") ?? new Amenity { Name = "TV" };
                var ac = await context.Amenities.FirstOrDefaultAsync(a => a.Name == "Air Conditioner") ?? new Amenity { Name = "Air Conditioner" };
                var bar = await context.Amenities.FirstOrDefaultAsync(a => a.Name == "Mini Bar") ?? new Amenity { Name = "Mini Bar" };
                var bath = await context.Amenities.FirstOrDefaultAsync(a => a.Name == "Bathtub") ?? new Amenity { Name = "Bathtub" };

                if (wifi.AmenityId == 0) context.Amenities.Add(wifi);
                if (tv.AmenityId == 0) context.Amenities.Add(tv);
                if (ac.AmenityId == 0) context.Amenities.Add(ac);
                if (bar.AmenityId == 0) context.Amenities.Add(bar);
                if (bath.AmenityId == 0) context.Amenities.Add(bath);
                await context.SaveChangesAsync();

                // Get Room Types
                var roomTypes = await context.RoomTypes.ToListAsync();
                var stdType = roomTypes.FirstOrDefault(rt => rt.TypeName == "Standard") ?? roomTypes.First();
                var dlxType = roomTypes.FirstOrDefault(rt => rt.TypeName == "Deluxe") ?? roomTypes.First();
                var suiType = roomTypes.FirstOrDefault(rt => rt.TypeName == "Suite") ?? roomTypes.First();

                // Room 101 (Standard)
                var r101 = new Room
                {
                    RoomNumber = "101",
                    Floor = 1,
                    RoomTypeId = stdType.RoomTypeId,
                    Status = "Available",
                    Description = "Phòng tiêu chuẩn ấm cúng, đầy đủ tiện nghi, thiết kế hiện đại, phù hợp cho 1-2 khách nghỉ ngơi.",
                    CreatedAt = DateTime.UtcNow,
                    Amenities = new List<Amenity> { wifi, tv, ac }
                };
                r101.RoomImages.Add(new RoomImage { ImageUrl = "/images/room_standard.png", IsPrimary = true });
                context.Rooms.Add(r101);

                // Room 102 (Standard)
                var r102 = new Room
                {
                    RoomNumber = "102",
                    Floor = 1,
                    RoomTypeId = stdType.RoomTypeId,
                    Status = "Available",
                    Description = "Phòng tiêu chuẩn tiện nghi, không gian thoáng đãng, yên tĩnh, tạo cảm giác dễ chịu và thư giãn.",
                    CreatedAt = DateTime.UtcNow,
                    Amenities = new List<Amenity> { wifi, tv, ac }
                };
                r102.RoomImages.Add(new RoomImage { ImageUrl = "/images/room_standard.png", IsPrimary = true });
                context.Rooms.Add(r102);

                // Room 201 (Deluxe)
                var r201 = new Room
                {
                    RoomNumber = "201",
                    Floor = 2,
                    RoomTypeId = dlxType.RoomTypeId,
                    Status = "Available",
                    Description = "Phòng Deluxe cao cấp với cửa sổ lớn ngắm cảnh thành phố tuyệt đẹp, trang bị Mini Bar sang chảnh.",
                    CreatedAt = DateTime.UtcNow,
                    Amenities = new List<Amenity> { wifi, tv, ac, bar }
                };
                r201.RoomImages.Add(new RoomImage { ImageUrl = "/images/room_deluxe.png", IsPrimary = true });
                context.Rooms.Add(r201);

                // Room 202 (Deluxe)
                var r202 = new Room
                {
                    RoomNumber = "202",
                    Floor = 2,
                    RoomTypeId = dlxType.RoomTypeId,
                    Status = "Available",
                    Description = "Phòng Deluxe sang trọng trang bị bồn tắm nằm thư giãn riêng biệt và đầy đủ đồ uống cao cấp tại Mini Bar.",
                    CreatedAt = DateTime.UtcNow,
                    Amenities = new List<Amenity> { wifi, tv, ac, bar, bath }
                };
                r202.RoomImages.Add(new RoomImage { ImageUrl = "/images/room_deluxe.png", IsPrimary = true });
                context.Rooms.Add(r202);

                // Room 301 (Suite)
                var r301 = new Room
                {
                    RoomNumber = "301",
                    Floor = 3,
                    RoomTypeId = suiType.RoomTypeId,
                    Status = "Available",
                    Description = "Phòng Suite hoàng gia, thiết kế rộng rãi có phòng khách tiếp khách riêng biệt, đầy đủ mọi tiện nghi đẳng cấp nhất.",
                    CreatedAt = DateTime.UtcNow,
                    Amenities = new List<Amenity> { wifi, tv, ac, bar, bath }
                };
                r301.RoomImages.Add(new RoomImage { ImageUrl = "/images/room_suite.png", IsPrimary = true });
                context.Rooms.Add(r301);

                await context.SaveChangesAsync();
            }
        }
    }
}