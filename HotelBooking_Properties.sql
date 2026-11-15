IF OBJECT_ID(N'dbo.Properties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Properties (
        PropertyId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Properties PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        Area NVARCHAR(80) NOT NULL,
        Address NVARCHAR(255) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        CONSTRAINT UQ_Properties_Name UNIQUE (Name)
    );
END;
GO

IF COL_LENGTH('dbo.Rooms', 'PropertyId') IS NULL
    ALTER TABLE dbo.Rooms ADD PropertyId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Properties WHERE Name = N'HBS Mỹ Khê')
    INSERT INTO dbo.Properties (Name, Area, Address, Description) VALUES
    (N'HBS Mỹ Khê', N'Mỹ Khê', N'123 Võ Nguyên Giáp, Mỹ Khê, Sơn Trà, Đà Nẵng', N'Cơ sở sát biển Mỹ Khê.'),
    (N'HBS Sơn Trà', N'Sơn Trà', N'88 Yết Kiêu, Thọ Quang, Sơn Trà, Đà Nẵng', N'Cơ sở trên bán đảo Sơn Trà, nhìn vịnh.'),
    (N'HBS Hải Châu', N'Hải Châu', N'50 Bạch Đằng, Hải Châu, Đà Nẵng', N'Cơ sở trung tâm, bên sông Hàn.'),
    (N'HBS Ngũ Hành Sơn', N'Ngũ Hành Sơn', N'15 Trường Sa, Ngũ Hành Sơn, Đà Nẵng', N'Cơ sở gần Ngũ Hành Sơn và biển Non Nước.'),
    (N'HBS Liên Chiểu', N'Liên Chiểu', N'200 Nguyễn Tất Thành, Hòa Hiệp Nam, Liên Chiểu, Đà Nẵng', N'Cơ sở phía bắc, gần sân bay.');
GO

UPDATE r
SET r.PropertyId = p.PropertyId
FROM dbo.Rooms r
CROSS JOIN dbo.Properties p
WHERE r.PropertyId IS NULL AND p.Name = N'HBS Mỹ Khê';
GO

IF EXISTS (SELECT 1 FROM dbo.Rooms WHERE PropertyId IS NULL)
    THROW 50001, N'Con phong chua gan co so.', 1;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Rooms') AND name = N'PropertyId' AND is_nullable = 1
)
    ALTER TABLE dbo.Rooms ALTER COLUMN PropertyId INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Rooms_Properties')
    ALTER TABLE dbo.Rooms
    ADD CONSTRAINT FK_Rooms_Properties FOREIGN KEY (PropertyId) REFERENCES dbo.Properties(PropertyId);
GO
