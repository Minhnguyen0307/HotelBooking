/*
   HotelBooking feature schema update
   Chạy sau HotelBooking_Database.sql trên database HotelBookingDB.
   Script có thể chạy lại an toàn: chỉ thêm cột/bảng còn thiếu.
*/

USE HotelBookingDB;
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Bookings', N'U') IS NULL
    THROW 50001, 'Khong tim thay dbo.Bookings. Hay chay schema goc truoc.', 1;
IF OBJECT_ID(N'dbo.Rooms', N'U') IS NULL
    THROW 50002, 'Khong tim thay dbo.Rooms. Hay chay schema goc truoc.', 1;
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    THROW 50003, 'Khong tim thay dbo.Users. Hay chay schema goc truoc.', 1;
GO

BEGIN TRANSACTION;

/* Tách số người lớn và trẻ em; dữ liệu cũ được chuyển sang người lớn. */
IF COL_LENGTH(N'dbo.Bookings', N'AdultCount') IS NULL
    EXEC(N'ALTER TABLE dbo.Bookings ADD AdultCount INT NULL;');

EXEC(N'UPDATE dbo.Bookings
       SET AdultCount = CASE WHEN NumberOfGuests > 0 THEN NumberOfGuests ELSE 1 END
       WHERE AdultCount IS NULL;');

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Bookings')
      AND name = N'AdultCount'
      AND is_nullable = 1
)
    EXEC(N'ALTER TABLE dbo.Bookings ALTER COLUMN AdultCount INT NOT NULL;');

IF COL_LENGTH(N'dbo.Bookings', N'ChildCount') IS NULL
    ALTER TABLE dbo.Bookings
        ADD ChildCount INT NOT NULL
            CONSTRAINT DF_Bookings_ChildCount DEFAULT (0);

IF COL_LENGTH(N'dbo.Bookings', N'CancelledAt') IS NULL
    ALTER TABLE dbo.Bookings ADD CancelledAt DATETIME2 NULL;
IF COL_LENGTH(N'dbo.Bookings', N'CancelReason') IS NULL
    ALTER TABLE dbo.Bookings ADD CancelReason NVARCHAR(300) NULL;

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Bookings')
      AND name = N'CK_Bookings_Status'
      AND definition NOT LIKE N'%CancelRequested%'
)
    ALTER TABLE dbo.Bookings DROP CONSTRAINT CK_Bookings_Status;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Bookings')
      AND definition LIKE N'%CancelRequested%'
)
    ALTER TABLE dbo.Bookings
        ADD CONSTRAINT CK_Bookings_Status_HotelBooking
        CHECK (Status IN ('Pending','Confirmed','CheckedIn','CheckedOut','CancelRequested','Cancelled'));

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Payments')
      AND name = N'CK_Payments_Status'
      AND definition NOT LIKE N'%Refunded%'
)
    ALTER TABLE dbo.Payments DROP CONSTRAINT CK_Payments_Status;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Payments')
      AND definition LIKE N'%Refunded%'
)
    ALTER TABLE dbo.Payments
        ADD CONSTRAINT CK_Payments_Status_HotelBooking
        CHECK (PaymentStatus IN ('Pending','Success','Failed','Refunded'));

/* Voucher có giá trị đơn tối thiểu, hạn dùng và giới hạn số lượt. */
IF OBJECT_ID(N'dbo.PromoCodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PromoCodes (
        PromoCodeId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code NVARCHAR(30) NOT NULL UNIQUE,
        DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_PromoCodes_DiscountAmount DEFAULT (0),
        DiscountPercent INT NOT NULL CONSTRAINT DF_PromoCodes_DiscountPercent DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_PromoCodes_IsActive DEFAULT (1),
        MinimumOrderAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_PromoCodes_Minimum DEFAULT (0),
        MaxUses INT NULL,
        UsageCount INT NOT NULL CONSTRAINT DF_PromoCodes_Usage DEFAULT (0),
        ExpiresAt DATETIME2 NULL
    );
END;

IF COL_LENGTH(N'dbo.PromoCodes', N'MinimumOrderAmount') IS NULL
    ALTER TABLE dbo.PromoCodes ADD MinimumOrderAmount DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PromoCodes_Minimum_Compat DEFAULT (0);
IF COL_LENGTH(N'dbo.PromoCodes', N'MaxUses') IS NULL
    ALTER TABLE dbo.PromoCodes ADD MaxUses INT NULL;
IF COL_LENGTH(N'dbo.PromoCodes', N'UsageCount') IS NULL
    ALTER TABLE dbo.PromoCodes ADD UsageCount INT NOT NULL
        CONSTRAINT DF_PromoCodes_Usage_Compat DEFAULT (0);
IF COL_LENGTH(N'dbo.PromoCodes', N'ExpiresAt') IS NULL
    ALTER TABLE dbo.PromoCodes ADD ExpiresAt DATETIME2 NULL;
IF COL_LENGTH(N'dbo.PromoCodes', N'IsNewCustomerOnly') IS NULL
    ALTER TABLE dbo.PromoCodes ADD IsNewCustomerOnly BIT NOT NULL
        CONSTRAINT DF_PromoCodes_NewCustomer DEFAULT (0);

/* Add-on services and voucher discounts are stored as folio lines. */
IF OBJECT_ID(N'dbo.FolioItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FolioItems (
        FolioItemId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BookingId INT NOT NULL,
        Category NVARCHAR(30) NOT NULL,
        Description NVARCHAR(200) NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_FolioItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Folio_Bookings FOREIGN KEY (BookingId)
            REFERENCES dbo.Bookings(BookingId) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'dbo.Refunds', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Refunds (
        RefundId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PaymentId INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Reason NVARCHAR(300) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Refunds_Status_Compat DEFAULT 'Pending',
        RequestedAt DATETIME2 NOT NULL CONSTRAINT DF_Refunds_RequestedAt_Compat DEFAULT SYSUTCDATETIME(),
        ProcessedAt DATETIME2 NULL,
        CONSTRAINT FK_Refunds_Payments_Compat FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(PaymentId),
        CONSTRAINT CK_Refunds_Status_Compat CHECK (Status IN ('Pending','Approved','Rejected','Completed'))
    );
END;

/* Public room reviews and optional review photos. */
IF OBJECT_ID(N'dbo.Reviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reviews (
        ReviewId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoomId INT NOT NULL,
        UserId INT NOT NULL,
        Rating INT NOT NULL,
        Comment NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT GETDATE(),
        StaffResponse NVARCHAR(MAX) NULL,
        RespondedByUserId INT NULL,
        RespondedAt DATETIME2 NULL,
        CONSTRAINT FK_Reviews_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(RoomId),
        CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Reviews_Staff FOREIGN KEY (RespondedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5)
    );
END;

IF OBJECT_ID(N'dbo.ReviewImages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReviewImages (
        ReviewImageId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReviewId INT NOT NULL,
        ImageUrl NVARCHAR(500) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ReviewImages_CreatedAt DEFAULT GETDATE(),
        CONSTRAINT FK_ReviewImages_Reviews FOREIGN KEY (ReviewId)
            REFERENCES dbo.Reviews(ReviewId) ON DELETE CASCADE
    );
END;

/* Voucher demo chung và voucher 50k/100k/200k dành riêng cho khách đặt lần đầu. */
IF NOT EXISTS (SELECT 1 FROM dbo.PromoCodes WHERE Code = N'SUMMER2026')
    INSERT dbo.PromoCodes (Code, DiscountAmount, DiscountPercent, IsActive, MinimumOrderAmount, MaxUses, UsageCount, ExpiresAt)
    VALUES (N'SUMMER2026', 0, 10, 1, 1000000, 50, 0, '2026-12-31T23:59:59');

IF NOT EXISTS (SELECT 1 FROM dbo.PromoCodes WHERE Code = N'GIAM200K')
    INSERT dbo.PromoCodes (Code, DiscountAmount, DiscountPercent, IsActive, MinimumOrderAmount, MaxUses, UsageCount, ExpiresAt)
    VALUES (N'GIAM200K', 200000, 0, 1, 1000000, 50, 0, '2026-12-31T23:59:59');

IF NOT EXISTS (SELECT 1 FROM dbo.PromoCodes WHERE Code = N'WELCOME50K')
    INSERT dbo.PromoCodes (Code, DiscountAmount, DiscountPercent, IsActive, MinimumOrderAmount, MaxUses, UsageCount, ExpiresAt, IsNewCustomerOnly)
    VALUES (N'WELCOME50K', 50000, 0, 1, 500000, 100, 0, '2026-12-31T23:59:59', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.PromoCodes WHERE Code = N'WELCOME100K')
    INSERT dbo.PromoCodes (Code, DiscountAmount, DiscountPercent, IsActive, MinimumOrderAmount, MaxUses, UsageCount, ExpiresAt, IsNewCustomerOnly)
    VALUES (N'WELCOME100K', 100000, 0, 1, 500000, 100, 0, '2026-12-31T23:59:59', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.PromoCodes WHERE Code = N'WELCOME200K')
    INSERT dbo.PromoCodes (Code, DiscountAmount, DiscountPercent, IsActive, MinimumOrderAmount, MaxUses, UsageCount, ExpiresAt, IsNewCustomerOnly)
    VALUES (N'WELCOME200K', 200000, 0, 1, 500000, 100, 0, '2026-12-31T23:59:59', 1);

COMMIT TRANSACTION;
GO

PRINT N'HotelBooking feature schema update completed.';
GO
