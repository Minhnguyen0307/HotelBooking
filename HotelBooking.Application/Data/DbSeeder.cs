using HotelBooking.Application.Services;
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

            await context.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH('Bookings', 'GuestName') IS NULL
                    ALTER TABLE Bookings ADD GuestName NVARCHAR(150) NULL;
                IF COL_LENGTH('Bookings', 'GuestPhone') IS NULL
                    ALTER TABLE Bookings ADD GuestPhone NVARCHAR(20) NULL;
                IF COL_LENGTH('Bookings', 'GuestNote') IS NULL
                    ALTER TABLE Bookings ADD GuestNote NVARCHAR(500) NULL;
                IF COL_LENGTH('Bookings', 'PaymentChoice') IS NULL
                    ALTER TABLE Bookings ADD PaymentChoice NVARCHAR(20) NOT NULL CONSTRAINT DF_Bookings_PaymentChoice DEFAULT 'Now';
                IF COL_LENGTH('Rooms', 'HousekeepingStatus') IS NULL
                    ALTER TABLE Rooms ADD HousekeepingStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Rooms_Housekeeping DEFAULT 'Clean';
                IF OBJECT_ID('FolioItems') IS NULL
                    CREATE TABLE FolioItems (
                        FolioItemId INT IDENTITY(1,1) PRIMARY KEY,
                        BookingId INT NOT NULL,
                        Category NVARCHAR(30) NOT NULL,
                        Description NVARCHAR(200) NOT NULL,
                        Quantity INT NOT NULL,
                        UnitPrice DECIMAL(18,2) NOT NULL,
                        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT FK_Folio_Bookings FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId) ON DELETE CASCADE
                    );
                IF OBJECT_ID('RoomIssues') IS NULL
                    CREATE TABLE RoomIssues (
                        IssueId INT IDENTITY(1,1) PRIMARY KEY,
                        RoomId INT NOT NULL,
                        Description NVARCHAR(300) NOT NULL,
                        Status NVARCHAR(20) NOT NULL DEFAULT 'Open',
                        ReportedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT FK_Issues_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId) ON DELETE CASCADE
                    );
                IF OBJECT_ID('PromoCodes') IS NULL
                    CREATE TABLE PromoCodes (
                        PromoCodeId INT IDENTITY(1,1) PRIMARY KEY,
                        Code NVARCHAR(30) NOT NULL UNIQUE,
                        DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                        DiscountPercent INT NOT NULL DEFAULT 0,
                        IsActive BIT NOT NULL DEFAULT 1
                    );
                """);

            if (!await context.PromoCodes.AnyAsync())
            {
                context.PromoCodes.AddRange(
                    new PromoCode { Code = "WELCOME10", DiscountPercent = 10, IsActive = true },
                    new PromoCode { Code = "GIAM100", DiscountAmount = 100_000m, IsActive = true });
                await context.SaveChangesAsync();
            }

            await EnsureChainAsync(context);
            await EnsureOccupancyAsync(context);
        }

        private static async Task EnsureOccupancyAsync(HotelBookingDbContext context)
        {
            var types = await context.RoomTypes.ToListAsync();
            foreach (var type in types)
            {
                var plan = OccupancyPricing.For(type.TypeName, type.MaxGuests);
                type.MaxGuests = plan.Capacity;
                if (plan.ExtraGuestFeePerNight > 0)
                    type.Description = plan.Rule;
            }
            await context.SaveChangesAsync();
        }

        private static async Task EnsureChainAsync(HotelBookingDbContext context)
        {
            var catalog = new (string Name, string Area, string Address, string Description)[]
            {
                ("HBS Mỹ Khê", "Mỹ Khê", "123 Võ Nguyên Giáp, Mỹ Khê, Sơn Trà, Đà Nẵng", "Cơ sở sát biển Mỹ Khê."),
                ("HBS Sơn Trà", "Sơn Trà", "88 Yết Kiêu, Thọ Quang, Sơn Trà, Đà Nẵng", "Cơ sở trên bán đảo Sơn Trà, nhìn vịnh."),
                ("HBS Hải Châu", "Hải Châu", "50 Bạch Đằng, Hải Châu, Đà Nẵng", "Cơ sở trung tâm, bên sông Hàn."),
                ("HBS Ngũ Hành Sơn", "Ngũ Hành Sơn", "15 Trường Sa, Ngũ Hành Sơn, Đà Nẵng", "Cơ sở gần Ngũ Hành Sơn và biển Non Nước."),
                ("HBS Liên Chiểu", "Liên Chiểu", "200 Nguyễn Tất Thành, Hòa Hiệp Nam, Liên Chiểu, Đà Nẵng", "Cơ sở phía bắc, gần sân bay.")
            };

            foreach (var item in catalog)
            {
                if (await context.Properties.AnyAsync(p => p.Name == item.Name)) continue;
                context.Properties.Add(new Property
                {
                    Name = item.Name,
                    Area = item.Area,
                    Address = item.Address,
                    Description = item.Description
                });
            }
            await context.SaveChangesAsync();

            var properties = await context.Properties.ToDictionaryAsync(p => p.Name);
            var roomTypes = await context.RoomTypes.ToListAsync();
            if (roomTypes.Count == 0) return;

            var wifi = await EnsureAmenity(context, "WiFi");
            var tv = await EnsureAmenity(context, "TV");
            var ac = await EnsureAmenity(context, "Air Conditioner");
            var bar = await EnsureAmenity(context, "Mini Bar");
            var bath = await EnsureAmenity(context, "Bathtub");
            await context.SaveChangesAsync();

            int TypeId(string name) =>
                (roomTypes.FirstOrDefault(rt => rt.TypeName == name) ?? roomTypes[0]).RoomTypeId;

            var rooms = new (string Number, string Property, string Type, int Floor, string Description, string Image, Amenity[] Amenities)[]
            {
                ("101", "HBS Mỹ Khê", "Standard", 1, "Phòng tiêu chuẩn sát biển Mỹ Khê, cửa sổ hướng bãi tắm.", "/images/room_standard.png", new[] { wifi, tv, ac }),
                ("102", "HBS Mỹ Khê", "Standard", 1, "Phòng tiêu chuẩn yên tĩnh trong khu Mỹ Khê, cách biển một con đường.", "/images/room_standard.png", new[] { wifi, tv, ac }),
                ("201", "HBS Mỹ Khê", "Deluxe", 2, "Deluxe Mỹ Khê với ban công nhìn thẳng ra biển.", "/images/room_deluxe.png", new[] { wifi, tv, ac, bar }),
                ("202", "HBS Mỹ Khê", "Deluxe", 2, "Deluxe Mỹ Khê có bồn tắm và mini bar.", "/images/room_deluxe.png", new[] { wifi, tv, ac, bar, bath }),
                ("301", "HBS Mỹ Khê", "Suite", 3, "Suite Mỹ Khê, phòng khách riêng và tầm nhìn toàn bãi biển.", "/images/room_suite.png", new[] { wifi, tv, ac, bar, bath }),

                ("ST-12", "HBS Sơn Trà", "Deluxe", 1, "Bungalow Sơn Trà nhìn vịnh, ban công riêng giữa cây.", "/images/room_deluxe.png", new[] { wifi, tv, ac, bar }),
                ("ST-18", "HBS Sơn Trà", "Deluxe", 1, "Deluxe bán đảo, sân hiên hướng Đông, không có ở cơ sở khác.", "/images/room_deluxe.png", new[] { wifi, tv, ac, bath }),
                ("ST-30", "HBS Sơn Trà", "Suite", 2, "Suite Sơn Trà hai phòng, nhìn rừng và biển.", "/images/room_suite.png", new[] { wifi, tv, ac, bar, bath }),

                ("HC-05", "HBS Hải Châu", "Standard", 5, "Phòng city view sông Hàn, dành cho khách đi trung tâm.", "/images/room_standard.png", new[] { wifi, tv, ac }),
                ("HC-06", "HBS Hải Châu", "Standard", 5, "Phòng đôi Hải Châu, cửa sổ nhìn phố Bạch Đằng.", "/images/room_standard.png", new[] { wifi, tv, ac }),
                ("HC-15", "HBS Hải Châu", "Deluxe", 8, "Deluxe làm việc tầng cao, nhìn cầu Rồng.", "/images/room_deluxe.png", new[] { wifi, tv, ac, bar }),

                ("NHS-03", "HBS Ngũ Hành Sơn", "Standard", 1, "Phòng vườn tầng trệt, gần biển Non Nước.", "/images/room_standard.png", new[] { wifi, tv, ac }),
                ("NHS-14", "HBS Ngũ Hành Sơn", "Deluxe", 2, "Deluxe hướng Ngũ Hành Sơn, ban công rộng.", "/images/room_deluxe.png", new[] { wifi, tv, ac, bar }),
                ("NHS-21", "HBS Ngũ Hành Sơn", "Suite", 3, "Suite gia đình, phòng khách và bếp nhỏ, riêng cơ sở Ngũ Hành Sơn.", "/images/room_suite.png", new[] { wifi, tv, ac, bar, bath }),

                ("LC-07", "HBS Liên Chiểu", "Standard", 2, "Phòng tiêu chuẩn yên tĩnh phía bắc, gần sân bay.", "/images/room_standard.png", new[] { wifi, tv, ac }),
                ("LC-08", "HBS Liên Chiểu", "Standard", 2, "Phòng đôi Liên Chiểu, cửa nhìn đường Nguyễn Tất Thành.", "/images/room_standard.png", new[] { wifi, tv, ac }),
                ("LC-16", "HBS Liên Chiểu", "Deluxe", 4, "Deluxe Liên Chiểu cho khách đi sớm, bàn làm việc và mini bar.", "/images/room_deluxe.png", new[] { wifi, tv, ac, bar })
            };

            foreach (var item in rooms)
            {
                var property = properties[item.Property];
                var existing = await context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == item.Number);
                if (existing != null)
                {
                    existing.PropertyId = property.PropertyId;
                    existing.Address = property.Address;
                    continue;
                }

                var room = new Room
                {
                    RoomNumber = item.Number,
                    Floor = item.Floor,
                    RoomTypeId = TypeId(item.Type),
                    PropertyId = property.PropertyId,
                    Status = "Available",
                    Description = item.Description,
                    Address = property.Address,
                    CreatedAt = DateTime.UtcNow,
                    Amenities = item.Amenities.ToList()
                };
                room.RoomImages.Add(new RoomImage { ImageUrl = item.Image, IsPrimary = true });
                context.Rooms.Add(room);
            }

            await context.SaveChangesAsync();
        }

        private static async Task<Amenity> EnsureAmenity(HotelBookingDbContext context, string name)
        {
            var amenity = await context.Amenities.FirstOrDefaultAsync(a => a.Name == name);
            if (amenity != null) return amenity;
            amenity = new Amenity { Name = name };
            context.Amenities.Add(amenity);
            return amenity;
        }
    }
}