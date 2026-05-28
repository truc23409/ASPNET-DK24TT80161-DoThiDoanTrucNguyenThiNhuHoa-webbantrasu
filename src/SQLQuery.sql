-- Tạo database WebAppDB
CREATE DATABASE WebAppDB;
GO

-- Sử dụng database WebAppDB
USE WebAppDB;
GO

-- Xóa tất cả các khóa ngoại trước
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' +
               QUOTENAME(OBJECT_NAME(parent_object_id)) + 
               ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.foreign_keys;
EXEC sp_executesql @sql;

-- Xóa tất cả các bảng
DECLARE @sql2 NVARCHAR(MAX) = '';
SELECT @sql2 += 'DROP TABLE IF EXISTS ' + QUOTENAME(OBJECT_SCHEMA_NAME(object_id)) + '.' + 
               QUOTENAME(name) + ';' + CHAR(13)
FROM sys.tables;
EXEC sp_executesql @sql2;
GO

-- 1. Tạo bảng Users
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) UNIQUE NOT NULL,
    Email NVARCHAR(50) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NULL,
    Phone NVARCHAR(15) NULL,
    Address NVARCHAR(255) NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'User',
    OTPCode NVARCHAR(10) NULL,
    OTPExpiry DATETIME NULL,
    ResetToken NVARCHAR(100) NULL,
    ResetTokenExpiry DATETIME NULL,
    AvatarUrl NVARCHAR(255) NULL, 
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

-- Table: TableFood
CREATE TABLE TableFood(
    TableId INT IDENTITY PRIMARY KEY,
    TableName NVARCHAR(100),
    TrangThai NVARCHAR(100)
);
GO

-- Table: AccRole
CREATE TABLE AccRole(
    RoleId INT IDENTITY PRIMARY KEY,
    RoleName NVARCHAR(100) NOT NULL
);
GO

-- Table: Account
CREATE TABLE Account (
    AccountId INT IDENTITY PRIMARY KEY,
    DisplayName NVARCHAR(100) NOT NULL,
    UserName NVARCHAR(50) NOT NULL,
    PassWord NVARCHAR(50) NOT NULL,
    RoleName NVARCHAR(50) not null
);
GO

-- Table: Category
CREATE TABLE Category (
    CategoryId INT PRIMARY KEY IDENTITY,
    CategoryName NVARCHAR(100) NOT NULL
);
GO

-- Table: Ingredient
CREATE TABLE Ingredient (
    IngredientId INT PRIMARY KEY IDENTITY,
    IngredientName NVARCHAR(100) NOT NULL,
    SoLuong INT NOT NULL DEFAULT 0,
    PhanLoai Nvarchar(50) not null,
    ImageURL VARCHAR(255) NOT NULL,
    LastUpdated DATE NOT NULL DEFAULT GETDATE()
);
GO

-- Table: Food
CREATE TABLE Food ( 
    FoodId INT PRIMARY KEY IDENTITY,
    FoodName NVARCHAR(100) NOT NULL,
    CategoryId INT,
    IngredientId INT,
    Price DECIMAL(18, 3) NOT NULL DEFAULT 0,
    Discount DECIMAL(5, 2) DEFAULT 0,
    DiscountPrice AS (Price - (Price * Discount / 100)),
    Stock INT NOT NULL DEFAULT 0,
    Description NVARCHAR(500) NULL,
    ImageURL NVARCHAR(255) NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME NULL,
    Status BIT DEFAULT 1,
    FOREIGN KEY (CategoryId) REFERENCES Category(CategoryId),
    FOREIGN KEY (IngredientId) REFERENCES Ingredient(IngredientId)
);
GO

-- Bảng Size
CREATE TABLE Size (
    SizeID INT IDENTITY(1,1) PRIMARY KEY,
    SizeName NVARCHAR(50) NOT NULL,
    ExtraPrice INT NOT NULL
);
GO

-- Bảng Topping
CREATE TABLE Topping (
    ToppingID INT IDENTITY(1,1) PRIMARY KEY,
    ToppingName NVARCHAR(100) NOT NULL,
    ToppingPrice INT NOT NULL
);
GO

-- Bảng GioHang
CREATE TABLE GioHang (
    GioHangID INT PRIMARY KEY IDENTITY, 
    Id INT NOT NULL,
    FoodId INT NOT NULL,
    SoLuong INT NOT NULL DEFAULT 1 CHECK (SoLuong > 0),
    SizeID INT NULL,
    TotalPrice DECIMAL(18,3) NOT NULL,
    FOREIGN KEY (Id) REFERENCES Users(Id),
    FOREIGN KEY (FoodId) REFERENCES Food(FoodId),
    FOREIGN KEY (SizeID) REFERENCES Size(SizeID)
);
GO

-- Bảng trung gian GioHang_Topping
CREATE TABLE GioHang_Topping (
    GioHangToppingID INT PRIMARY KEY IDENTITY,
    GioHangID INT NOT NULL,
    ToppingID INT NOT NULL,
    FOREIGN KEY (GioHangID) REFERENCES GioHang(GioHangID) ON DELETE CASCADE,
    FOREIGN KEY (ToppingID) REFERENCES Topping(ToppingID) ON DELETE CASCADE
);
GO

CREATE TABLE PhuongThucThanhToan (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TenPhuongThuc NVARCHAR(255) NOT NULL
);

CREATE TABLE OrderStatus (
    StatusId INT PRIMARY KEY IDENTITY(1,1),
    StatusName NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE DeliveryAddresses (
    AddressId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    IsDefault BIT NOT NULL DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

-- Bảng Orders
CREATE TABLE Orders (
    OrderId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18, 3) NOT NULL,
    PaymentMethodId INT NOT NULL,
    StatusId INT NOT NULL,
    DeliveryAddress NVARCHAR(255) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (PaymentMethodId) REFERENCES PhuongThucThanhToan(Id),
    FOREIGN KEY (StatusId) REFERENCES OrderStatus(StatusId)
);
GO

-- Bảng OrderDetails
CREATE TABLE OrderDetails (
    OrderDetailId INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL,
    FoodId INT NOT NULL,
    SizeId INT NULL,
    ToppingId INT NULL,
    Quantity INT NOT NULL DEFAULT 1 CHECK (Quantity > 0),
    Price DECIMAL(18, 3) NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
    FOREIGN KEY (FoodId) REFERENCES Food(FoodId),
    FOREIGN KEY (SizeId) REFERENCES Size(SizeID),
    FOREIGN KEY (ToppingId) REFERENCES Topping(ToppingID)
);
GO

-- Table: Invoice
CREATE TABLE Invoice (
    InvoiceId INT PRIMARY KEY IDENTITY,
    TableId INT,
    DateCheckIn DATE NOT NULL DEFAULT GETDATE(),
    DateCheckOut DATE,
    TrangThai INT,
    FOREIGN KEY (TableId) REFERENCES TableFood(TableId)
);
GO

-- Table: InvoiceDetail
CREATE TABLE InvoiceDetail(
    InvoiceDetailId INT PRIMARY KEY IDENTITY,
    InvoiceId INT,
    FoodId INT,
    SoLuong INT NOT NULL DEFAULT 0,
    Price DECIMAL(18, 3) NOT NULL,
    FOREIGN KEY (InvoiceId) REFERENCES Invoice(InvoiceId),
    FOREIGN KEY (FoodId) REFERENCES Food(FoodId)
);
GO

-- Table: Staff
CREATE TABLE Staff (
    StaffId INT PRIMARY KEY IDENTITY,
    FullName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(15),
    DateOfBirth DATE NULL,
    Email NVARCHAR(50) NULL,
    Gender NVARCHAR(50) null,
    AccountId INT,
    RoleId INT,
    FOREIGN KEY (AccountId) REFERENCES Account(AccountId),
    FOREIGN KEY (RoleId) REFERENCES AccRole(RoleId)
);
GO

-- Table: Warehouse
CREATE TABLE Warehouse (
    WarehouseId INT PRIMARY KEY IDENTITY,
    IngredientId INT,
    SoLuong INT NOT NULL DEFAULT 0,
    DateUpdate DATE NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (IngredientId) REFERENCES Ingredient(IngredientId)
);
GO

-- Table: FoodIngredient
CREATE TABLE FoodIngredient (
    FoodIngredientId INT PRIMARY KEY IDENTITY,
    FoodId INT,
    IngredientId INT,
    Quantity INT NOT NULL,
    FOREIGN KEY (FoodId) REFERENCES Food(FoodId),
    FOREIGN KEY (IngredientId) REFERENCES Ingredient(IngredientId)
);
GO

-- INSERT DATA với Users mới
INSERT INTO Users (Username, Email, PasswordHash, FullName, Phone, Address, Role, AvatarUrl)
VALUES 
('dotruc', 'dotruc@gmail.com', '1', N'Đỗ Thị Đoan Trúc', '0366413924', N'Long An', 'Admin', '/images/Avatar/a.png'),
('nhuhoa', 'nhuhoa@gmail.com', '1', N'Nguyễn Thị Như Hoa', '0987654321', N'Long An', 'User', '/images/Avatar/a.png');
GO

INSERT INTO TableFood (TableName, TrangThai)
VALUES
('Table 1', N'Đã có khách'),
('Table 2', N'Đã có khách'),
('Table 3', N'Bàn Trống'),
('Table 4', N'Bàn Trống'),
('Table 5', N'Đã có khách'),
('Table 6', N'Bàn Trống'),
('Table 7', N'Bàn Trống'),
('Table 8', N'Đã có khách'),
('Table 9', N'Bàn Trống'),
('Table 10', N'Bàn Trống'),
('Table 11', N'Bàn Trống'),
('Table 12', N'Bàn Trống');
GO

INSERT INTO AccRole (RoleName)
VALUES
(N'Quản lý'),
(N'Pha chế'),
(N'Phục vụ');
GO

INSERT INTO Account (DisplayName, UserName, PassWord, RoleName)
VALUES
('Admin', 'admin', '1', 'Admin');
GO

INSERT INTO Category (CategoryName)
VALUES
(N'Cà phê'),
(N'Trà sữa'),
(N'Thức uống đá xay'),
(N'Bánh & Snack'),
(N'Trà trái cây');
GO

INSERT INTO Ingredient (IngredientName, SoLuong, PhanLoai, ImageURl, LastUpdated)
VALUES
(N'Coffee', 100, N'gói', '/images/nguyenlieu/coffee.png', '2024-08-05'),
(N'Sữa', 500, N'chai', '/images/nguyenlieu/sua.png', '2024-08-05'),
(N'Đường', 200, N'gói', '/images/nguyenlieu/duong.png', '2024-08-05'),
(N'Trà', 75, N'gói', '/images/nguyenlieu/tra.png', '2024-08-05'),
(N'Táo', 50, N'quả', '/images/nguyenlieu/tao.png', '2024-08-05'),
(N'Trân châu', 100, N'túi', '/images/nguyenlieu/tranchau.png', '2024-08-05'),
(N'Matcha', 120, N'gói', '/images/nguyenlieu/matcha.png', '2024-08-05'),
(N'Yến mạch', 130, N'gói', '/images/nguyenlieu/yenmach.png', '2024-08-05'),
(N'Caramel', 140, N'lọ', '/images/nguyenlieu/caramel.png', '2024-08-05'),
(N'Muối', 125, N'gói', '/images/nguyenlieu/muoi.png', '2024-08-05'),
(N'Hạnh nhân', 115, N'lọ', '/images/nguyenlieu/hanhnhan.png', '2024-08-05'),
(N'Bơ', 120, N'quả', '/images/nguyenlieu/bo.png', '2024-08-05'),
(N'Kem', 150, N'hộp', '/images/nguyenlieu/kem.png', '2024-08-05'),
(N'Choco Chip', 130, N'gói', '/images/nguyenlieu/choco_chip.png', '2024-08-05'),
(N'Mochi kem phúc bồn tử', 120, N'cái', '/images/nguyenlieu/mochi_kpbt.png', '2024-08-05'),
(N'Mochi kem việt quất', 120, N'cái', '/images/nguyenlieu/mochi_kvq.png', '2024-08-05'),
(N'Mochi kem chocolate', 120, N'cái', '/images/nguyenlieu/mochi_kemchoco.png', '2024-08-05'),
(N'Mousse gấu chocolate', 110, N'cái', '/images/nguyenlieu/mousse_gau.png', '2024-08-05'),
(N'Bánh mỳ', 150, N'ổ', '/images/nguyenlieu/banhmy.png', '2024-08-05'),
(N'Sữa đặc', 160, N'hộp', '/images/nguyenlieu/suadac.png', '2024-08-05'),
(N'Cam', 135, N'quả', '/images/nguyenlieu/cam.png', '2024-08-05'),
(N'Sả', 125, N'cây', '/images/nguyenlieu/sa.png', '2024-08-05'),
(N'Hạt sen', 115, N'túi', '/images/nguyenlieu/hatsen.png', '2024-08-05'),
(N'Vải', 140, N'quả', '/images/nguyenlieu/vai.png', '2024-08-05'),
(N'Mứt Yuzu', 120, N'lọ', '/images/nguyenlieu/yuzu.png', '2024-08-05'),
(N'Đào', 120, N'hộp', '/images/nguyenlieu/dao.png', '2024-08-05'),
(N'Bánh Gấu', 120, N'gói', '/images/nguyenlieu/banhgau.png', '2024-08-05'),
(N'Sương Sáo', 120, N'cốc', '/images/nguyenlieu/suongsao.png', '2024-08-05'),
(N'Dâu', 120, N'quả', '/images/nguyenlieu/dau.png', '2024-08-05'),
(N'Bim Bim Ngô', 120, N'gói', '/images/nguyenlieu/bimbimngo.png', '2024-08-05'),
(N'Bim Bim Sữa Dừa', 120, N'gói', '/images/nguyenlieu/bimbimsuadua.png', '2024-08-05');
GO

-- Thêm dữ liệu Food (giữ nguyên như cũ nhưng rút gọn để dễ đọc)
INSERT INTO Food (FoodName, CategoryId, IngredientId, Price, Discount, Stock, Description, ImageURL, CreatedDate, UpdatedDate, Status)
VALUES
(N'Trà xanh espresso marble', 1, 1, 45000, 10, 50, N'Trà xanh kết hợp espresso thơm ngon', '/images/Cafe/traxanhespresso.png', GETDATE(), NULL, 1),
(N'Bạc xỉu lắc sữa yến mạch', 1, 1, 50000, 5, 40, N'Cà phê sữa pha cùng sữa yến mạch', '/images/Cafe/bacxiulsyenmach.png', GETDATE(), NULL, 1),
(N'Bạc xỉu lắc caramel muối', 1, 1, 55000, 7, 45, N'Cà phê sữa lắc cùng caramel muối', '/images/Cafe/bacxiulacmuoi.png', GETDATE(), NULL, 1),
(N'Bạc xỉu lắc hạnh nhân nướng', 1, 1, 55000, 8, 35, N'Cà phê sữa kết hợp hạnh nhân nướng', '/images/Cafe/bacxiulachanhnhan.png', GETDATE(), NULL, 1),
(N'Bơ arabica', 1, 1, 60000, 12, 25, N'Cà phê Arabica với vị béo của bơ', '/images/Cafe/bo_arabica.png', GETDATE(), NULL, 1);
GO

-- Thêm dữ liệu Size
INSERT INTO Size (SizeName, ExtraPrice)
VALUES 
    (N'Nhỏ', 0),
    (N'Vừa', 6000),
    (N'Lớn', 16000);
GO

-- Thêm dữ liệu Topping
INSERT INTO Topping (ToppingName, ToppingPrice)
VALUES 
    (N'Thạch Sương Sáo', 10000),
    (N'Thạch Kim Quất', 10000),
    (N'Thạch Cà Phê', 10000),
    (N'Foam Phô Mai', 10000),
    (N'Shot Espresso', 10000),
    (N'Sốt Caramel', 10000),
    (N'Trân châu trắng', 10000),
    (N'Đá miếng', 5000),
    (N'Hạt sen', 10000),
    (N'Trái vải', 10000),
    (N'Kem phô mai Macchiato', 10000);
GO

-- Thêm dữ liệu DeliveryAddresses với địa chỉ ở Long An cho cả 2 user
INSERT INTO DeliveryAddresses (UserId, Address, IsDefault)
SELECT Id, N'123 Đường Láng, Tân An, Long An', 1 FROM Users WHERE Username = 'dotruc'
UNION ALL
SELECT Id, N'456 Nguyễn Trãi, Bến Lức, Long An', 0 FROM Users WHERE Username = 'dotruc'
UNION ALL
SELECT Id, N'789 Lê Lợi, Đức Hòa, Long An', 1 FROM Users WHERE Username = 'nhuhoa';
GO

-- Thêm dữ liệu Staff với tên Staff 1-5
INSERT INTO Staff (FullName, Phone, DateOfBirth, Email, Gender, AccountId, RoleId) 
VALUES
(N'Staff 1', '0985082001', '2004-08-05', 'staff1@gmail.com', N'Nam', NULL, 1),
(N'Staff 2', '0985082002', '2004-08-06', 'staff2@gmail.com', N'Nữ', NULL, 2),
(N'Staff 3', '0985082003', '2004-08-07', 'staff3@gmail.com', N'Nam', NULL, 2),
(N'Staff 4', '0985082004', '2004-08-08', 'staff4@gmail.com', N'Nữ', NULL, 3),
(N'Staff 5', '0985082005', '2004-08-09', 'staff5@gmail.com', N'Nam', NULL, 3);
GO

INSERT INTO Warehouse (IngredientId, SoLuong, DateUpdate)
VALUES
(1, 100, GETDATE()),
(2, 50, GETDATE()),
(3, 200, GETDATE()),
(4, 75, GETDATE());
GO

INSERT INTO PhuongThucThanhToan (TenPhuongThuc)
VALUES 
(N'VN Pay'),
(N'COD');
GO

INSERT INTO OrderStatus (StatusName)
VALUES 
    (N'Đặt hàng thành công'),
    (N'Đang chuẩn bị đơn hàng'),
    (N'Đang giao hàng'),
    (N'Giao hàng thành công'),
    (N'Đã hủy');
GO

-- Kiểm tra kết quả
SELECT * FROM Users;
SELECT * FROM Staff;
SELECT * FROM DeliveryAddresses;

-- Sử dụng database WebAppDB
USE WebAppDB;
GO

-- Thêm dữ liệu Food về Trà sữa (CategoryId = 2)
INSERT INTO Food (FoodName, CategoryId, IngredientId, Price, Discount, Stock, Description, ImageURL, CreatedDate, UpdatedDate, Status)
VALUES
(N'Trà sữa trân châu đường đen', 2, 2, 35000, 10, 40, N'Trà sữa kết hợp trân châu đường đen thơm ngon', '/images/trasua/tstcduongden.png', GETDATE(), NULL, 1),
(N'Trà sữa olong', 2, 2, 30000, 5, 50, N'Trà sữa vị Olong đặc biệt thơm mát', '/images/trasua/ts_olong.png', GETDATE(), NULL, 1),
(N'Trà sữa olong tứ quý bơ', 2, 2, 35000, 7, 30, N'Trà sữa Olong với bơ thơm béo', '/images/trasua/ts_olongtqbo.png', GETDATE(), NULL, 1),
(N'Trà sữa olong nướng sương sáo', 2, 2, 35000, 5, 35, N'Trà sữa Olong kết hợp sương sáo dai giòn', '/images/trasua/ts_olongss.png', GETDATE(), NULL, 1),
(N'Trà đen macchiato', 2, 2, 30000, 5, 45, N'Trà đen phủ lớp macchiato béo mịn', '/images/trasua/tradenmacchiato.png', GETDATE(), NULL, 1),
(N'Hồng trà sữa trân châu', 2, 2, 30000, 5, 50, N'Hồng trà sữa truyền thống với trân châu dai ngon', '/images/trasua/hongtrasua.png', GETDATE(), NULL, 1),
(N'Trà sữa matcha', 2, 2, 40000, 10, 35, N'Trà sữa matcha Nhật Bản thơm ngon', '/images/trasua/ts_matcha.png', GETDATE(), NULL, 1),
(N'Trà sữa khoai môn', 2, 2, 35000, 5, 40, N'Trà sữa khoai môn thơm béo tự nhiên', '/images/trasua/ts_khoaimon.png', GETDATE(), NULL, 1),
(N'Trà sữi dâu', 2, 2, 38000, 8, 35, N'Trà sữa vị dâu tươi mát', '/images/trasua/ts_dau.png', GETDATE(), NULL, 1),
(N'Trà sữa socola', 2, 2, 35000, 5, 45, N'Trà sữa vị socola đậm đà', '/images/trasua/ts_socola.png', GETDATE(), NULL, 1),
(N'Trà sữa lài', 2, 2, 32000, 5, 40, N'Trà sữa hoa lài thơm nhẹ', '/images/trasua/ts_lai.png', GETDATE(), NULL, 1),
(N'Trà sữa hoa đậu biếc', 2, 2, 35000, 8, 35, N'Trà sữa hoa đậu biếc màu sắc đẹp mắt', '/images/trasua/ts_hoaudaubiec.png', GETDATE(), NULL, 1),
(N'Trà sữa trân châu hoàng kim', 2, 2, 40000, 10, 30, N'Trà sữa với trân châu hoàng kim đặc biệt', '/images/trasua/ts_tchoangkim.png', GETDATE(), NULL, 1),
(N'Trà sữa pudding', 2, 2, 38000, 5, 35, N'Trà sữa kết hợp pudding thơm béo', '/images/trasua/ts_pudding.png', GETDATE(), NULL, 1),
(N'Trà sữa phô mai', 2, 2, 42000, 10, 30, N'Trà sữa với lớp kem phô mai mặn mặn', '/images/trasua/ts_phomai.png', GETDATE(), NULL, 1);

GO

-- Thêm nguyên liệu cho các món trà sữa (FoodIngredient)
-- Lưu ý: FoodId sẽ tự động tăng, bạn cần kiểm tra FoodId thực tế sau khi INSERT

-- Lấy danh sách FoodId vừa thêm
DECLARE @FoodTable TABLE (FoodId INT, FoodName NVARCHAR(100));

-- Thêm các món trà sữa và lưu ID
INSERT INTO Food (FoodName, CategoryId, IngredientId, Price, Discount, Stock, Description, ImageURL, CreatedDate, UpdatedDate, Status)
OUTPUT inserted.FoodId, inserted.FoodName INTO @FoodTable
VALUES
(N'Trà sữa trân châu đường đen', 2, 2, 35000, 10, 40, N'Trà sữa kết hợp trân châu đường đen thơm ngon', '/images/trasua/tstcduongden.png', GETDATE(), NULL, 1),
(N'Trà sữa olong', 2, 2, 30000, 5, 50, N'Trà sữa vị Olong đặc biệt thơm mát', '/images/trasua/ts_olong.png', GETDATE(), NULL, 1),
(N'Trà sữa olong tứ quý bơ', 2, 2, 35000, 7, 30, N'Trà sữa Olong với bơ thơm béo', '/images/trasua/ts_olongtqbo.png', GETDATE(), NULL, 1),
(N'Trà sữa olong nướng sương sáo', 2, 2, 35000, 5, 35, N'Trà sữa Olong kết hợp sương sáo dai giòn', '/images/trasua/ts_olongss.png', GETDATE(), NULL, 1),
(N'Trà đen macchiato', 2, 2, 30000, 5, 45, N'Trà đen phủ lớp macchiato béo mịn', '/images/trasua/tradenmacchiato.png', GETDATE(), NULL, 1),
(N'Hồng trà sữa trân châu', 2, 2, 30000, 5, 50, N'Hồng trà sữa truyền thống với trân châu dai ngon', '/images/trasua/hongtrasua.png', GETDATE(), NULL, 1),
(N'Trà sữa matcha', 2, 2, 40000, 10, 35, N'Trà sữa matcha Nhật Bản thơm ngon', '/images/trasua/ts_matcha.png', GETDATE(), NULL, 1),
(N'Trà sữa khoai môn', 2, 2, 35000, 5, 40, N'Trà sữa khoai môn thơm béo tự nhiên', '/images/trasua/ts_khoaimon.png', GETDATE(), NULL, 1),
(N'Trà sữa dâu', 2, 2, 38000, 8, 35, N'Trà sữa vị dâu tươi mát', '/images/trasua/ts_dau.png', GETDATE(), NULL, 1),
(N'Trà sữa socola', 2, 2, 35000, 5, 45, N'Trà sữa vị socola đậm đà', '/images/trasua/ts_socola.png', GETDATE(), NULL, 1),
(N'Trà sữa lài', 2, 2, 32000, 5, 40, N'Trà sữa hoa lài thơm nhẹ', '/images/trasua/ts_lai.png', GETDATE(), NULL, 1),
(N'Trà sữa hoa đậu biếc', 2, 2, 35000, 8, 35, N'Trà sữa hoa đậu biếc màu sắc đẹp mắt', '/images/trasua/ts_hoaudaubiec.png', GETDATE(), NULL, 1),
(N'Trà sữa trân châu hoàng kim', 2, 2, 40000, 10, 30, N'Trà sữa với trân châu hoàng kim đặc biệt', '/images/trasua/ts_tchoangkim.png', GETDATE(), NULL, 1),
(N'Trà sữa pudding', 2, 2, 38000, 5, 35, N'Trà sữa kết hợp pudding thơm béo', '/images/trasua/ts_pudding.png', GETDATE(), NULL, 1),
(N'Trà sữa phô mai', 2, 2, 42000, 10, 30, N'Trà sữa với lớp kem phô mai mặn mặn', '/images/trasua/ts_phomai.png', GETDATE(), NULL, 1);

-- Thêm nguyên liệu cho từng món trà sữa
-- Trà sữa trân châu đường đen (FoodId tương ứng)
INSERT INTO FoodIngredient (FoodId, IngredientId, Quantity)
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa trân châu đường đen'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa trân châu đường đen'
UNION ALL
SELECT FoodId, 6, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa trân châu đường đen'
UNION ALL
SELECT FoodId, 3, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa trân châu đường đen'

UNION ALL
-- Trà sữa olong
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa olong'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa olong'

UNION ALL
-- Trà sữa olong tứ quý bơ
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa olong tứ quý bơ'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa olong tứ quý bơ'
UNION ALL
SELECT FoodId, 12, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa olong tứ quý bơ'

UNION ALL
-- Trà sữa olong nướng sương sáo
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa olong nướng sương sáo'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa olong nướng sương sáo'
UNION ALL
SELECT FoodId, 28, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa olong nướng sương sáo'

UNION ALL
-- Trà đen macchiato
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà đen macchiato'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà đen macchiato'

UNION ALL
-- Hồng trà sữa trân châu
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Hồng trà sữa trân châu'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Hồng trà sữa trân châu'
UNION ALL
SELECT FoodId, 6, 1 FROM @FoodTable WHERE FoodName = N'Hồng trà sữa trân châu'

UNION ALL
-- Trà sữa matcha
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa matcha'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa matcha'
UNION ALL
SELECT FoodId, 7, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa matcha'

UNION ALL
-- Trà sữa khoai môn
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa khoai môn'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa khoai môn'

UNION ALL
-- Trà sữa dâu
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa dâu'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa dâu'
UNION ALL
SELECT FoodId, 29, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa dâu'

UNION ALL
-- Trà sữa socola
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa socola'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa socola'
UNION ALL
SELECT FoodId, 14, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa socola'

UNION ALL
-- Trà sữa lài
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa lài'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa lài'

UNION ALL
-- Trà sữa hoa đậu biếc
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa hoa đậu biếc'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa hoa đậu biếc'

UNION ALL
-- Trà sữa trân châu hoàng kim
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa trân châu hoàng kim'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa trân châu hoàng kim'
UNION ALL
SELECT FoodId, 6, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa trân châu hoàng kim'

UNION ALL
-- Trà sữa pudding
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa pudding'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa pudding'

UNION ALL
-- Trà sữa phô mai
SELECT FoodId, 4, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa phô mai'
UNION ALL
SELECT FoodId, 2, 1 FROM @FoodTable WHERE FoodName = N'Trà sữa phô mai';
GO

-- Kiểm tra kết quả
SELECT f.FoodId, f.FoodName, c.CategoryName, f.Price, f.Discount, f.DiscountPrice, f.Stock
FROM Food f
INNER JOIN Category c ON f.CategoryId = c.CategoryId
WHERE c.CategoryName = N'Trà sữa'
ORDER BY f.FoodId;

-- Sử dụng database WebAppDB
USE WebAppDB;
GO

-- Kiểm tra xem có đơn hàng nào đang sử dụng VNPay không
IF EXISTS (
    SELECT 1 
    FROM Orders o
    INNER JOIN PhuongThucThanhToan p ON o.PaymentMethodId = p.Id
    WHERE p.TenPhuongThuc = N'VN Pay'
)
BEGIN
    PRINT 'Không thể xóa VNPay vì đang có đơn hàng sử dụng phương thức này!';
    
    -- Hiển thị danh sách đơn hàng đang dùng VNPay
    SELECT o.OrderId, o.OrderDate, o.TotalAmount, p.TenPhuongThuc
    FROM Orders o
    INNER JOIN PhuongThucThanhToan p ON o.PaymentMethodId = p.Id
    WHERE p.TenPhuongThuc = N'VN Pay';
END
ELSE
BEGIN
    -- Xóa phương thức thanh toán VNPay
    DELETE FROM PhuongThucThanhToan 
    WHERE TenPhuongThuc = N'VN Pay';
    
    PRINT 'Đã xóa phương thức thanh toán VNPay thành công!';
END
GO

-- Kiểm tra lại danh sách phương thức thanh toán còn lại
SELECT * FROM PhuongThucThanhToan;
GO