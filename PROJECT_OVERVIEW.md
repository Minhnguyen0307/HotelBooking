# HỒ SƠ TỔNG QUAN DỰ ÁN: HOTEL BOOKING SYSTEM (HBS)

> **Môn học:** PRN222 - Ứng dụng .NET / C# (FPT University)  
> **Phiên bản:** 1.0.0  
> **Ngày lập hồ sơ:** 15/11/2026  
> **Trạng thái:** Sẵn sàng phát triển & mở rộng

---

## 1. Giới thiệu tổng quan

**Hotel Booking System (HBS)** là hệ thống quản lý và đặt phòng khách sạn trực tuyến toàn diện, phục vụ cho chuỗi khách sạn/khu nghỉ dưỡng (ví dụ: *HBS Luxury Hotel Đà Nẵng*). Hệ thống cung cấp trải nghiệm đa đối tượng:
- **Khách hàng (Customer):** Tìm kiếm phòng theo nhu cầu (ngày, số người, cơ sở, tiện ích), xem chi tiết phòng và ảnh, đặt phòng kèm dịch vụ bổ sung, áp mã giảm giá, thanh toán qua QR/thẻ, quản lý đơn và đánh giá chất lượng phòng sau khi lưu trú.
- **Lễ tân (Receptionist):** Quản lý luồng khách đến/đi trong ngày, check-in, check-out, thanh toán hoá đơn tổng hợp (Folio/Mini-bar/Dịch vụ), đặt phòng qua điện thoại, cập nhật tình trạng buồng phòng (Housekeeping) và theo dõi sự cố phòng.
- **Quản lý & Quản trị viên (Manager & Admin):** Quản lý buồng phòng, loại phòng, tài khoản khách hàng/nhân viên, theo dõi biểu đồ doanh thu và công suất phòng, xuất báo cáo CSV phục vụ kinh doanh.

---

## 2. Kiến trúc giải pháp (System Architecture)

Dự án áp dụng mô hình kiến trúc phân tầng kết hợp Frontend tách biệt (Client-Server / Single Page Application Architecture):

```
+-----------------------------------------------------------------+
|                        FRONTEND (SPA)                           |
|       React 19 + Vite 6 + React Router v7 (Port 3000)          |
|  - Pages: Home, Search, RoomDetail, FrontDesk, Admin, Reports   |
|  - Auth: JWT Bearer in LocalStorage, Context API                |
+-----------------------------------------------------------------+
                                |
                     RESTful API / SignalR
                                |
+-----------------------------------------------------------------+
|                  BACKEND: ASP.NET Core 8 Web API                |
|                    (Port 5191 / Swagger UI)                     |
+-----------------------------------------------------------------+
| [1] Presentation Layer: HotelBooking                            |
|     - Controllers/Api: Auth, Bookings, FrontDesk, Admin, etc.   |
|     - SignalR Hub: RoomHub (thông báo cập nhật phòng tức thì)  |
|     - Middleware: Exception Handler, JWT Bearer Auth, CORS      |
+-----------------------------------------------------------------+
| [2] Application Layer: HotelBooking.Application                 |
|     - Interfaces: IBookingService, IFrontDeskService, etc.      |
|     - Services: Nghiệp vụ đặt phòng, tính phụ thu, dịch vụ      |
|     - DTOs: Request/Response Data Transfer Objects              |
|     - Pricing Engines: OccupancyPricing, BookingAddOnPricing    |
|     - Seed Data: DbSeeder                                       |
+-----------------------------------------------------------------+
| [3] Infrastructure Layer: HotelBooking.Infrastructure           |
|     - HotelBookingDbContext (Entity Framework Core 8)           |
|     - Entity Models: Room, Booking, User, Payment, Folio...     |
|     - Views: VwMonthlyRevenue, VwRoomOccupancy                  |
+-----------------------------------------------------------------+
| [4] Domain Layer: HotelBooking.Domain                           |
|     - Core business logic / base abstractions                   |
+-----------------------------------------------------------------+
                                |
                         ADO.NET / EF Core
                                |
+-----------------------------------------------------------------+
|                  DATABASE: Microsoft SQL Server                 |
|                    Database: HotelBookingDB                     |
+-----------------------------------------------------------------+
```

---

## 3. Ngăn xếp công nghệ (Tech Stack)

### 3.1. Backend
- **Nền tảng:** .NET 8 (C# 12), ASP.NET Core Web API.
- **ORM:** Entity Framework Core 8.0.3 (`Microsoft.EntityFrameworkCore.SqlServer`, `Tools`, `Design`).
- **Xác thực & Bảo mật:**
  - JWT Bearer Authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`).
  - Chuẩn hoá băm mật khẩu: `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (PBKDF2 SHA-256).
  - Tích hợp đăng nhập bên thứ ba: `Google.Apis.Auth`.
- **Giao tiếp Real-time:** SignalR (`Microsoft.AspNetCore.SignalR`).
- **Tài liệu API:** Swagger UI / OpenAPI (`Swashbuckle.AspNetCore 6.6.2`).
- **Email thông báo:** SMTP Gmail kết hợp `EmailSettings`.

### 3.2. Frontend
- **Framework & Công cụ:** React 19, Vite 6, React Router DOM v7.
- **Thư viện phụ trợ:**
  - `qrcode`: Sinh mã QR động cho luồng thanh toán qua ngân hàng/ví điện tử.
- **Giao diện (UI/UX):** Custom CSS hiện đại (`styles.css`), thiết kế responsive, tông màu khách sạn sang trọng, hỗ trợ modal, dashboard bàn làm việc lễ tân, bảng biểu và thanh thông báo.
- **Giao tiếp API:** Module `src/api.js` hỗ trợ tự động đính kèm JWT token, tự động retry khi gặp sự cố mạng, xử lý timeout và chuẩn hoá thông điệp lỗi tiếng Việt.

### 3.3. Cơ sở dữ liệu
- **Hệ quản trị CSDL:** Microsoft SQL Server.
- **Database Name:** `HotelBookingDB`.
- **Chuỗi kết nối (Connection String):**
  `Server=.\SQLEXPRESS;Database=HotelBookingDB;Trusted_Connection=True;TrustServerCertificate=True;`

---

## 4. Mô hình dữ liệu & Các bảng chính (Database Schema)

| Tên bảng / View | Mô tả chức năng |
| :--- | :--- |
| `Users` | Lưu thông tin người dùng (Khách, Lễ tân, Quản lý, Admin), mật khẩu băm, số lần đăng nhập sai. |
| `Roles` | Danh sách vai trò: `Customer`, `Receptionist`, `Manager`, `Admin`. |
| `Properties` | Danh mục cơ sở khách sạn/khu nghỉ dưỡng (Tên, địa chỉ, số điện thoại, mô tả). |
| `RoomTypes` | Danh mục hạng phòng (Standard, Deluxe, Suite, Family...) kèm giá sàn và sức chứa tiêu chuẩn. |
| `Rooms` | Thông tin phòng cụ thể (Số phòng, tầng, view, giá thực tế, trạng thái phòng `Status`, trạng thái dọn dẹp `HousekeepingStatus`). |
| `RoomImages` | Danh sách URL hình ảnh thực tế của từng phòng. |
| `Amenities` | Danh mục tiện ích (WiFi, máy lạnh, bồn tắm, hồ bơi, minibar...). |
| `Bookings` | Đơn đặt phòng: ngày check-in/out, số người lớn/trẻ em, tổng tiền, giảm giá, trạng thái đơn, hình thức thanh toán (`Now`/`Later`). |
| `BookingRooms` | Bảng liên kết phòng được chỉ định cho từng đơn đặt phòng. |
| `Payments` | Bản ghi giao dịch thanh toán: số tiền, phương thức (`Cash`, `Card`, `Transfer`, `Wallet`), mã giao dịch, trạng thái. |
| `FolioItems` | Chi phí phát sinh tại phòng (Minibar, giặt ủi, đồ ăn nhà hàng, dịch vụ spa, phụ thu làm hỏng thiết bị...). |
| `RoomIssues` | Ghi nhận sự cố hư hỏng phòng của bộ phận buồng phòng/lễ tân (Mô tả, độ khẩn cấp, trạng thái Open/Resolved). |
| `PromoCodes` | Mã giảm giá (% hoặc tiền mặt, đơn tối thiểu, hạn sử dụng, cờ ưu đãi riêng cho khách mới). |
| `Reviews` & `ReviewImages` | Đánh giá sao (1-5), nhận xét và ảnh thực tế từ khách sau khi check-out. |
| `PasswordResetTokens` | Mã token hỗ trợ luồng quên/đặt lại mật khẩu qua email. |
| `AuditLogs` | Nhật ký ghi nhận các hành vi nhạy cảm trên hệ thống. |
| `VwMonthlyRevenue` *(View)* | Báo cáo doanh thu và số đơn thành công theo từng tháng. |
| `VwRoomOccupancy` *(View)* | Báo cáo tần suất đặt phòng và tổng số đêm thuê của từng phòng. |

---

## 5. Phân quyền & Danh sách tài khoản thử nghiệm

Dữ liệu mẫu được tự động khởi tạo thông qua `DbSeeder.cs` mỗi khi ứng dụng khởi chạy nếu chưa tồn tại:

| Vai trò (Role) | Email đăng nhập | Mật khẩu mặc định | Quyền hạn chính |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@hotel.com` | `Admin@123` | Toàn quyền cấu hình hệ thống, quản lý phòng, khách hàng, nhân sự và báo cáo. |
| **Manager** | `manager@hotel.com` | `Manager@123` | Quản lý giá, phòng, loại phòng, duyệt nghiệp vụ, xem báo cáo thống kê doanh thu. |
| **Receptionist** | `receptionist@hotel.com` | `Reception@123` | Bàn làm việc lễ tân (`/frontdesk`), Check-in, Check-out, thu ngân, buồng phòng, đặt phòng qua điện thoại. |
| **Customer 1** | `customer1@hotel.com` | `Customer@123` | Tìm kiếm phòng, đặt phòng, thanh toán, quản lý đơn cá nhân, đánh giá phòng. |
| **Customer 2** | `customer2@hotel.com` | `Customer@123` | Tài khoản khách hàng phụ phục vụ kiểm thử song song. |

---

## 6. Logic nghiệp vụ đặc biệt & Giải thuật cốt lõi

### 6.1. Thuật toán kiểm tra phòng trống & Chống trùng lịch (Overbooking Prevention)
Trong `BookingService.cs`:
- **Quy tắc thời gian:** Khách trước trả phòng trước 12:00, khách sau nhận phòng sau 14:00. Do đó, hai đơn phòng có ngày Check-out trùng với ngày Check-in **không** bị tính là xung đột.
- **Điều kiện LINQ:**
  ```csharp
  br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn
  ```
- **Trạng thái giữ chỗ:** Các đơn ở trạng thái `Pending`, `Confirmed`, `CheckedIn`, và `CancelRequested` đều chiếm chỗ phòng. Chỉ khi đơn là `Cancelled` hoặc `CheckedOut` thì phòng mới được giải phóng.

### 6.2. Động cơ tính giá & Phụ thu theo số lượng khách (`OccupancyPricing`)
Hệ thống linh hoạt tính giá theo đặc tính phòng:
- **Phòng đơn (Standard):** Tiêu chuẩn 1 người, tối đa 2 người. Khách thứ 2 phụ thu +150,000 VNĐ/đêm.
- **Phòng cặp đôi (Deluxe):** Tiêu chuẩn 2 người, tối đa 4 người. Mỗi khách thêm phụ thu +200,000 VNĐ/đêm.
- **Phòng gia đình (Suite):** Tiêu chuẩn 4 người, tối đa 6 người. Mỗi khách thêm phụ thu +250,000 VNĐ/đêm.

### 6.3. Dịch vụ giá trị gia tăng (`BookingAddOnPricing`)
Khách hàng có thể đặt kèm khi book phòng:
- Đưa đón sân bay (350,000 VNĐ/lượt).
- Buffet sáng (180,000 VNĐ/khách/đêm - tự động nhân theo số đêm).
- Spa thư giãn (450,000 VNĐ/suất).
- Kê giường phụ (300,000 VNĐ/giường/đêm).

### 6.4. Bàn lễ tân đa năng (Front Desk Dashboard)
- **Quầy hôm nay:** Hiển thị 3 danh mục rõ rệt: Khách đến hôm nay (Arrivals), Khách đi hôm nay (Departures), Khách đang lưu trú (In-house).
- **Thanh toán Folio:** Hỗ trợ tính gộp tiền phòng và các dịch vụ phát sinh tại phòng (Mini-bar, giặt ủi...) khi Check-out.
- **Buồng phòng:** Cho phép lễ tân/nhân viên dọn phòng cập nhật tức thì trạng thái: `Sạch (Clean)`, `Đang dọn (Cleaning)`, `Chưa dọn (Dirty)`, `Bảo trì (Maintenance)`.

### 6.5. Báo cáo doanh thu theo cơ sở & Xuất file CSV tiếng Việt chuẩn
- **Báo cáo doanh thu theo từng cơ sở (Revenue by Property):**
  - Cung cấp 4 chỉ số KPI tổng quan (Tổng doanh thu, Cơ sở dẫn đầu, Số đơn thành công, Tổng số đêm thuê).
  - Bộ lọc tương tác đa chiều (theo Cơ sở, theo Năm, theo Tháng).
  - Thẻ trực quan (`property-card`) với nhận diện thương hiệu màu sắc từng cơ sở, nhấp để lọc nhanh.
  - Bảng tổng hợp doanh thu theo từng cơ sở với dòng tính tổng cộng (`tfoot`).
  - Bảng diễn biến chi tiết doanh thu từng cơ sở theo từng tháng.
- **Xuất dữ liệu CSV:** Hỗ trợ xuất dữ liệu ra file `.csv` có gắn **BOM UTF-8 (`0xEF, 0xBB, 0xBF`)** giúp mở trực tiếp bằng Microsoft Excel mà không bị lỗi font chữ tiếng Việt:
  - `BaoCaoDoanhThuTheoCoSo.csv`: Báo cáo doanh thu tổng hợp các cơ sở theo bộ lọc.
  - `BaoCaoDoanhThuCoSoTheoThang.csv`: Báo cáo doanh thu từng cơ sở phân bổ theo từng tháng.
  - `BaoCaoDoanhThu.csv`: Báo cáo doanh thu toàn hệ thống theo tháng.
  - `BaoCaoTanSuatPhong.csv`: Báo cáo tần suất buồng phòng.

---

## 7. Cấu trúc thư mục mã nguồn

```
HotelBooking/
│
├── HotelBooking.sln                         # Solution tổng hợp của dự án .NET
├── HotelBooking_Database.sql               # Script khởi tạo cơ sở dữ liệu gốc
├── HotelBooking_Feature_Update.sql         # Script cập nhật mở rộng các cột mới
├── HotelBooking_Properties.sql             # Dữ liệu khởi tạo cho các cơ sở khách sạn
├── AI_Audit_Log.md                         # Nhật ký đối chiếu giải pháp công nghệ AI / DTC
├── PROJECT_OVERVIEW.md                     # Tài liệu tổng quan dự án (File này)
│
├── HotelBooking/                           # [TẦNG WEB API]
│   ├── Controllers/Api/                    # Các REST API Controllers
│   │   ├── AuthApiController.cs            # Đăng nhập, đăng ký, Google OAuth, đổi/quên mật khẩu
│   │   ├── BookingsApiController.cs        # Tạo đơn, xem trước giá, áp mã giảm giá, hủy đơn
│   │   ├── FrontDeskApiController.cs       # API nghiệp vụ lễ tân, folio, buồng phòng
│   │   ├── AdminRoomsApiController.cs      # CRUD phòng & upload ảnh
│   │   ├── AdminRoomTypesApiController.cs  # CRUD loại phòng
│   │   ├── AdminCustomersApiController.cs  # Quản lý khách hàng
│   │   ├── PaymentsApiController.cs        # Xử lý thanh toán QR / cổng thanh toán
│   │   ├── ReportsApiController.cs         # Thống kê & xuất file CSV
│   │   ├── RoomsApiController.cs           # Tra cứu phòng & review công khai
│   │   └── HealthController.cs            # Kiểm tra trạng thái hệ thống
│   ├── Properties/launchSettings.json      # Cấu hình cổng chạy (5191 / 7024)
│   ├── appsettings.json                    # Connection String, cấu hình JWT, Email, Google
│   └── Program.cs                          # Khởi tạo DI, Pipeline, SignalR, DbContext
│
├── HotelBooking.Application/               # [TẦNG BUSINESS LOGIC]
│   ├── Data/DbSeeder.cs                    # Tự động khởi tạo dữ liệu mẫu và tài khoản test
│   ├── DTOs/                               # Data Transfer Objects cho API
│   ├── Hubs/RoomHub.cs                     # SignalR Hub cho đồng bộ buồng phòng real-time
│   ├── Interfaces/                         # Hợp đồng giao tiếp (Interfaces cho Services)
│   ├── Services/                           # Xử lý nghiệp vụ chuyên sâu
│   └── Settings/                           # Lớp ánh xạ cấu hình (EmailSettings, etc.)
│
├── HotelBooking.Infrastructure/            # [TẦNG DATA ACCESS / EF CORE]
│   ├── HotelBookingDbContext.cs            # DbContext kết nối SQL Server
│   ├── Entities/Models (Room, User...)     # Các thực thể dữ liệu ánh xạ bảng
│   └── Views (VwMonthlyRevenue...)         # Ánh xạ View SQL
│
├── HotelBooking.Domain/                    # [TẦNG DOMAIN CỐT LÕI]
│
└── frontend/                               # [GIAO DIỆN CLIENT REACT SPA]
    ├── index.html                          # Trang HTML gốc
    ├── package.json                        # Thư viện React 19, Vite 6, React Router DOM
    ├── vite.config.js                      # Cấu hình proxy đến backend port 5191
    └── src/
        ├── api.js                          # Fetch wrapper (JWT, retry, error handling)
        ├── auth.jsx                        # AuthProvider, Context & custom hook useAuth()
        ├── format.js                       # Hàm tiện ích định dạng tiền VNĐ, ngày tháng
        ├── ui.jsx                          # Các UI Component dùng chung (Modal, Loading...)
        ├── styles.css                      # Toàn bộ mã định kiểu CSS của hệ thống
        └── pages/                          # Các màn hình chức năng:
            ├── HomePage.jsx                # Trang chủ quảng bá khách sạn
            ├── SearchPage.jsx              # Tìm kiếm phòng, lọc theo ngày/tiện ích
            ├── RoomPage.jsx                # Chi tiết phòng, đánh giá, form đặt phòng
            ├── PayPage.jsx                 # Trang thanh toán đơn qua mã QR
            ├── BookingsPage.jsx            # Lịch sử đơn đặt phòng của khách
            ├── FrontDeskPage.jsx           # Dashboard nghiệp vụ toàn diện cho lễ tân
            ├── AdminRoomsPage.jsx          # Quản lý danh sách phòng cho Admin/Manager
            ├── RoomFormPage.jsx            # Form tạo mới/chỉnh sửa thông tin phòng
            ├── RoomTypesPage.jsx           # Quản lý hạng phòng
            ├── CustomersPage.jsx           # Quản lý khách hàng
            ├── ReportsPage.jsx             # Biểu đồ và tải báo cáo doanh thu/công suất
            ├── LoginPage.jsx               # Đăng nhập (thường & Google OAuth)
            ├── RegisterPage.jsx            # Đăng ký tài khoản
            └── ProfilePage.jsx             # Thông tin cá nhân & lịch sử
```

---

## 8. Hướng dẫn cài đặt & Vận hành

### 8.1. Yêu cầu hệ thống
- **.NET SDK:** Phiên bản 8.0 trở lên.
- **Node.js:** Phiên bản 18+ (kèm npm).
- **SQL Server:** SQL Server 2019 / 2022 hoặc SQL Server Express / LocalDB.
- **Visual Studio 2022** hoặc **VS Code**.

### 8.2. Cấu hình cơ sở dữ liệu
1. Mở SQL Server Management Studio (SSMS) hoặc Azure Data Studio.
2. Chạy lần lượt các tệp script theo thứ tự:
   - `HotelBooking_Database.sql` (Khởi tạo bảng cơ sở, quan hệ và views).
   - `HotelBooking_Feature_Update.sql` (Cập nhật các cột tính năng mới).
   - `HotelBooking_Properties.sql` (Khởi tạo dữ liệu cơ sở khách sạn).
3. Kiểm tra chuỗi kết nối trong `HotelBooking/appsettings.json` để khớp với SQL Server của máy tính.

### 8.3. Khởi chạy Backend API
Mở cửa sổ dòng lệnh tại thư mục gốc của project:
```bash
cd HotelBooking
dotnet run
```
- API sẽ lắng nghe tại: `http://localhost:5191` (hoặc `https://localhost:7024`).
- Xem tài liệu Swagger UI tại: `http://localhost:5191/swagger`.

### 8.4. Khởi chạy Frontend React
Mở cửa sổ dòng lệnh thứ hai tại thư mục `frontend`:
```bash
cd frontend
npm install
npm run dev
```
- Giao diện người dùng sẽ chạy tại: `http://localhost:3000`.
- Mọi yêu cầu gọi `/api/*`, `/images/*`, `/uploads/*` đều tự động được Vite proxy sang cổng `5191` của backend.

---

## 9. Đánh giá hiện trạng & Sẵn sàng cho các công việc tiếp theo

1. **Mức độ hoàn thiện:** Dự án đã có đầy đủ khung sườn, toàn bộ các luồng nghiệp vụ cơ bản từ đặt phòng, thanh toán, buồng phòng đến báo cáo đã được xây dựng hoàn chỉnh.
2. **Độ ổn định mã nguồn:**
   - Frontend đã qua kiểm tra biên dịch (`npm run build`), không có lỗi cú pháp hay thiếu package.
   - Backend tuân thủ nghiêm ngặt nguyên lý thiết kế Dependency Injection, Clean Architecture (3-layer), mã hóa mật khẩu bảo mật và xử lý race condition / overbooking.
3. **Môi trường đã sẵn sàng:** Hệ thống đã được khảo sát toàn diện và chuẩn bị đầy đủ tài liệu để bắt đầu thực hiện các yêu cầu phát triển tiếp theo của bạn!
