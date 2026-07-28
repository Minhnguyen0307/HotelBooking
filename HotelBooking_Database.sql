/* =========================================================================
   HOTEL BOOKING SYSTEM (HBS) - SWD392-SE19B02-G8
   SQL Server Database Design
   Bao phu cac Use Case: UC-01 -> UC-32 trong bao cao du an
   ========================================================================= */

IF DB_ID('HotelBookingDB') IS NULL
BEGIN
    CREATE DATABASE HotelBookingDB;
END
GO

USE HotelBookingDB;
GO

/* =========================================================================
   1. ROLES & USERS
   Actor: Customer, Receptionist, Manager, System Administrator (UC-01..06, UC-13)
   ========================================================================= */

CREATE TABLE Roles (
    RoleId      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    NVARCHAR(50) NOT NULL UNIQUE   -- Customer, Receptionist, Manager, Admin
);
GO

CREATE TABLE Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleId          INT NOT NULL,
    FullName        NVARCHAR(150) NOT NULL,
    Email           NVARCHAR(150) NOT NULL UNIQUE,     -- BR-01: Email phai duy nhat
    PasswordHash    NVARCHAR(256) NOT NULL,            -- luu ma hoa, khong luu plain text
    PhoneNumber     NVARCHAR(20)  NULL,                -- BR-04: dinh dang so dien thoai
    IsActive        BIT NOT NULL DEFAULT 1,
    FailedLoginCount INT NOT NULL DEFAULT 0,           -- BR-02: khoa tai khoan sau nhieu lan sai
    LockoutUntil    DATETIME2 NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2 NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

-- UC-04: Forgot Password (token-based reset, BR-03: het han sau thoi gian gioi han)
CREATE TABLE PasswordResetTokens (
    TokenId     INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT NOT NULL,
    Token       NVARCHAR(200) NOT NULL UNIQUE,
    ExpiresAt   DATETIME2 NOT NULL,
    IsUsed      BIT NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PRT_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

/* =========================================================================
   2. ROOM TYPES, ROOMS, AMENITIES, IMAGES
   UC-07..11, UC-23..27
   ========================================================================= */

CREATE TABLE RoomTypes (
    RoomTypeId      INT IDENTITY(1,1) PRIMARY KEY,
    TypeName        NVARCHAR(100) NOT NULL,     -- Standard, Deluxe, Suite...
    Description     NVARCHAR(MAX) NULL,
    BasePrice       DECIMAL(18,2) NOT NULL,
    MaxGuests       INT NOT NULL DEFAULT 2,
    IsActive        BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE Rooms (
    RoomId          INT IDENTITY(1,1) PRIMARY KEY,
    RoomTypeId      INT NOT NULL,
    RoomNumber      NVARCHAR(20) NOT NULL UNIQUE,
    Floor           INT NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Available',
                    -- 'Available', 'Booked', 'Maintenance'  (UC-27, FE-10)
    Description     NVARCHAR(MAX) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2 NULL,
    CONSTRAINT FK_Rooms_RoomTypes FOREIGN KEY (RoomTypeId) REFERENCES RoomTypes(RoomTypeId),
    CONSTRAINT CK_Rooms_Status CHECK (Status IN ('Available','Booked','Maintenance'))
);
GO

-- UC-10: View Room Images
CREATE TABLE RoomImages (
    ImageId     INT IDENTITY(1,1) PRIMARY KEY,
    RoomId      INT NOT NULL,
    ImageUrl    NVARCHAR(500) NOT NULL,
    IsPrimary   BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_RoomImages_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId) ON DELETE CASCADE
);
GO

CREATE TABLE Amenities (
    AmenityId   INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL UNIQUE      -- WiFi, Air Conditioner, TV...
);
GO

-- N-N: mot phong co nhieu tien nghi
CREATE TABLE RoomAmenities (
    RoomId      INT NOT NULL,
    AmenityId   INT NOT NULL,
    PRIMARY KEY (RoomId, AmenityId),
    CONSTRAINT FK_RA_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId) ON DELETE CASCADE,
    CONSTRAINT FK_RA_Amenities FOREIGN KEY (AmenityId) REFERENCES Amenities(AmenityId) ON DELETE CASCADE
);
GO

/* =========================================================================
   3. BOOKINGS
   UC-12..18: Book Room, Confirm, Cancel, View History
   ========================================================================= */

CREATE TABLE Bookings (
    BookingId       INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId      INT NOT NULL,
    CheckInDate     DATE NOT NULL,
    CheckOutDate    DATE NOT NULL,
    NumberOfGuests  INT NOT NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending',
                    -- 'Pending','Confirmed','CheckedIn','CheckedOut','Cancelled'
    TotalPrice      DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CancelledAt     DATETIME2 NULL,
    CancelReason    NVARCHAR(300) NULL,
    CONSTRAINT FK_Bookings_Customer FOREIGN KEY (CustomerId) REFERENCES Users(UserId),
    CONSTRAINT CK_Bookings_Status CHECK (Status IN ('Pending','Confirmed','CheckedIn','CheckedOut','Cancelled')),
    CONSTRAINT CK_Bookings_Dates CHECK (CheckOutDate > CheckInDate)
);
GO

-- Mot booking co the gom nhieu phong; luu gia tai thoi diem dat (chong truong hop doi gia sau nay)
CREATE TABLE BookingRooms (
    BookingRoomId   INT IDENTITY(1,1) PRIMARY KEY,
    BookingId       INT NOT NULL,
    RoomId          INT NOT NULL,
    PricePerNight   DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_BR_Bookings FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId) ON DELETE CASCADE,
    CONSTRAINT FK_BR_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId)
);
GO

/* =========================================================================
   4. PAYMENTS & REFUNDS
   UC-19..22, FE-06, FE-07
   ========================================================================= */

CREATE TABLE Payments (
    PaymentId       INT IDENTITY(1,1) PRIMARY KEY,
    BookingId       INT NOT NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    PaymentMethod   NVARCHAR(50) NOT NULL,      -- CreditCard, MoMo, VNPay, Banking...
    PaymentStatus   NVARCHAR(20) NOT NULL DEFAULT 'Pending',
                    -- 'Pending','Success','Failed'
    TransactionId   NVARCHAR(200) NULL,          -- ma giao dich tu cong thanh toan
    PaymentDate     DATETIME2 NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Payments_Bookings FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
    CONSTRAINT CK_Payments_Status CHECK (PaymentStatus IN ('Pending','Success','Failed'))
);
GO

CREATE TABLE Refunds (
    RefundId        INT IDENTITY(1,1) PRIMARY KEY,
    PaymentId       INT NOT NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    Reason          NVARCHAR(300) NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending',
                    -- 'Pending','Approved','Rejected','Completed'
    RequestedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ProcessedAt     DATETIME2 NULL,
    CONSTRAINT FK_Refunds_Payments FOREIGN KEY (PaymentId) REFERENCES Payments(PaymentId),
    CONSTRAINT CK_Refunds_Status CHECK (Status IN ('Pending','Approved','Rejected','Completed'))
);
GO

/* =========================================================================
   5. AUDIT LOG
   FE-15: Provide audit and tracking capabilities
   ========================================================================= */

CREATE TABLE AuditLogs (
    LogId       INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT NULL,
    Action      NVARCHAR(100) NOT NULL,     -- 'CreateBooking','CancelBooking','UpdateRoom'...
    TableName   NVARCHAR(100) NULL,
    RecordId    INT NULL,
    Details     NVARCHAR(MAX) NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

/* =========================================================================
   6. INDEXES - toi uu cho tim kiem phong trong (UC-07, UC-08)
   ========================================================================= */

CREATE INDEX IX_Bookings_Dates ON Bookings(CheckInDate, CheckOutDate);
CREATE INDEX IX_Rooms_Status ON Rooms(Status);
CREATE INDEX IX_Rooms_RoomTypeId ON Rooms(RoomTypeId);
CREATE INDEX IX_BookingRooms_RoomId ON BookingRooms(RoomId);
CREATE INDEX IX_Payments_BookingId ON Payments(BookingId);
GO

/* =========================================================================
   7. SEED DATA CO BAN
   ========================================================================= */

INSERT INTO Roles (RoleName) VALUES ('Customer'), ('Receptionist'), ('Manager'), ('Admin');
GO

INSERT INTO RoomTypes (TypeName, Description, BasePrice, MaxGuests) VALUES
(N'Standard', N'Phong tieu chuan, day du tien nghi co ban', 500000, 2),
(N'Deluxe',   N'Phong cao cap, view dep hon', 800000, 3),
(N'Suite',    N'Phong hang sang, phong khach rieng', 1500000, 4);
GO

INSERT INTO Amenities (Name) VALUES
(N'WiFi'), (N'Air Conditioner'), (N'TV'), (N'Mini Bar'), (N'Bathtub');
GO

/* =========================================================================
   8. VIEWS PHUC VU BAO CAO (UC-30, UC-31, FE-12)
   ========================================================================= */

-- Bao cao doanh thu theo thang
CREATE OR ALTER VIEW vw_MonthlyRevenue AS
SELECT
    YEAR(p.PaymentDate)  AS RevenueYear,
    MONTH(p.PaymentDate) AS RevenueMonth,
    SUM(p.Amount)         AS TotalRevenue,
    COUNT(DISTINCT p.BookingId) AS TotalBookingsPaid
FROM Payments p
WHERE p.PaymentStatus = 'Success'
GROUP BY YEAR(p.PaymentDate), MONTH(p.PaymentDate);
GO

-- Thong ke ty le lap dung phong (occupancy) theo tung phong
CREATE OR ALTER VIEW vw_RoomOccupancy AS
SELECT
    r.RoomId,
    r.RoomNumber,
    COUNT(br.BookingRoomId) AS TotalBookings,
    SUM(DATEDIFF(DAY, b.CheckInDate, b.CheckOutDate)) AS TotalNightsBooked
FROM Rooms r
LEFT JOIN BookingRooms br ON br.RoomId = r.RoomId
LEFT JOIN Bookings b ON b.BookingId = br.BookingId AND b.Status <> 'Cancelled'
GROUP BY r.RoomId, r.RoomNumber;
GO

/* =========================================================================
   9. STORED PROCEDURE MAU: Kiem tra phong trong theo khoang ngay (UC-07)
   ========================================================================= */

CREATE OR ALTER PROCEDURE sp_SearchAvailableRooms
    @CheckIn  DATE,
    @CheckOut DATE,
    @Guests   INT = NULL,
    @RoomTypeId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.RoomId, r.RoomNumber, rt.TypeName, rt.BasePrice, rt.MaxGuests
    FROM Rooms r
    JOIN RoomTypes rt ON rt.RoomTypeId = r.RoomTypeId
    WHERE r.Status <> 'Maintenance'
      AND (@RoomTypeId IS NULL OR r.RoomTypeId = @RoomTypeId)
      AND (@Guests IS NULL OR rt.MaxGuests >= @Guests)
      AND r.RoomId NOT IN (
            SELECT br.RoomId
            FROM BookingRooms br
            JOIN Bookings b ON b.BookingId = br.BookingId
            WHERE b.Status IN ('Pending','Confirmed','CheckedIn','CancelRequested')
              AND b.CheckInDate < @CheckOut
              AND b.CheckOutDate > @CheckIn
      );
END
GO
