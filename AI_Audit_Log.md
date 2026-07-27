# AI AUDIT LOG - DỰ ÁN HOTEL BOOKING
*(RBL Insight Framework - AI Reflection 30% - FPT University)*

---

## I. THÔNG TIN CHUNG
- **Dự án:** Hệ thống đặt phòng khách sạn (Hotel Booking System)
- **Công nghệ áp dụng:** ASP.NET Core MVC, Entity Framework Core, SQL Server (Identity & View), Cookie Authentication.
- **Bảng thống kê phân bổ DTC (Decomposition, Pattern Recognition, Abstraction, Algorithms):**

| Mã Entry | Loại Prompt | Stage/Component (DTC) | Mô tả tóm tắt quyết định / vấn đề giải quyết |
| :--- | :--- | :--- | :--- |
| **001** | `DECISION` | Decomposition + Research | Lựa chọn kiến trúc phân tầng (Layered Architecture) 3 dự án cho hệ thống. |
| **002** | `DECISION` | Algorithms | Thuật toán kiểm tra phòng trống khả dụng, tránh trùng lịch đặt phòng (Overbooking). |
| **003** | `PROBLEM-SOLVING` | Pattern Recognition | Xử lý lỗi xung đột khóa ngoại FK khi seed dữ liệu mẫu có Identity tự tăng. |
| **004** | `DECISION` | Abstraction | Triển khai phân quyền theo vai trò (Admin/Customer/FrontDesk) qua Cookie Claims. |
| **005** | `VERIFICATION` | Algorithms + Research | **[Phát hiện Hallucination]** Lỗ hổng bảo mật cập nhật thanh toán trực tiếp qua URL GET. |
| **006** | `DECISION` | Pattern Recognition | Ánh xạ Keyless Entity cho Database View SQL báo cáo doanh thu tháng. |
| **007** | `DECISION` | Decomposition | Phân rã nghiệp vụ quản lý và chọn phương án lưu trữ hình ảnh phòng (wwwroot vs DB). |
| **008** | `DECISION` | Abstraction | Thay thế thuật toán băm mật khẩu tự chế bằng lớp trừu tượng `PasswordHasher<T>` mặc định. |
| **009** | `PROBLEM-SOLVING` | Algorithms | Xử lý tranh chấp đặt phòng đồng thời (Concurrency) bằng Transaction Serializable. |

---

## II. CHI TIẾT NHẬT KÝ AI AUDIT LOG (DETAILED ENTRIES)

### Entry #: 001
- **Prompt Type:** `DECISION`
- **Stage/Component:** Decomposition (CT) + Research Stage (RBL)
- **Problem/Context:** Lựa chọn mô hình cấu trúc mã nguồn phù hợp cho dự án để đảm bảo tính phân tách độc lập giữa các tầng giao diện (Web UI), xử lý logic (Business Logic) và truy cập cơ sở dữ liệu (Database Access).
- **Prompt to AI:** `"Compare using a simple MVC pattern vs. a Layered Architecture (separated into Application, Domain, Infrastructure, Web projects) for an ASP.NET Core Hotel Booking application. What are the pros/cons and folder structures?"`
- **AI Response (Summary):** AI gợi ý sử dụng kiến trúc phân tầng (Layered Architecture) với 4 dự án riêng biệt để đạt độ cô lập tốt nhất: Tầng Domain chứa các thực thể nghiệp vụ; Infrastructure chứa DbContext và các thực thi cơ sở dữ liệu; Application chứa DTOs và Services logic; Web chứa Controllers/Views. Cách này giúp dễ viết kiểm thử tự động (Unit Tests) nhưng tăng độ phức tạp khi khởi tạo dự án.
- **Human Delta & Reflection:**
  * **Critical Thinking:** Đề xuất cấu trúc của AI là rất chuẩn mực cho dự án lớn. Tuy nhiên, AI bỏ qua chi tiết về cách cấu hình Dependency Injection giữa các tầng trong lớp khởi chạy của tầng Web, điều này dễ dẫn đến lỗi tham chiếu vòng (Circular Dependency) đối với những người mới học lập trình khi cấu hình các tham chiếu chéo giữa các project.
  * **Contextualization:** Trong thực tế đồ án môn học này, số lượng thực thể chỉ ở mức vừa phải (khoảng 10 bảng dữ liệu). Việc chia quá nhỏ thành 4 dự án riêng biệt (tách biệt Domain và Infrastructure) sẽ làm sinh thêm rất nhiều lớp trung gian (boilerplate code), tăng thời gian cấu hình mà không mang lại nhiều giá trị thực tiễn cho dự án quy mô vừa và nhỏ.
  * **Creative Synthesis:** Tôi đã kết hợp và đơn giản hóa kiến trúc đề xuất của AI thành mô hình 3 dự án:
    1. `HotelBooking` (Web MVC chứa Controllers/Views/Program.cs).
    2. `HotelBooking.Application` (chứa các Interface, DTOs và Services logic nghiệp vụ).
    3. `HotelBooking.Infrastructure` (nơi gộp chung DbContext và các Class thực thể dữ liệu Domain gốc).
    Sự thay đổi này giúp giảm bớt 1 dự án thư viện lớp (Domain), giúp cấu trúc gọn gàng hơn mà vẫn đảm bảo tính phân tách nghiệp vụ chính theo yêu cầu.
  * **Decision Ownership:** Quyết định cấu hình dự án theo mô hình 3 project (Web, Application, Infrastructure) thay vì 4 dự án như AI gợi ý để cân bằng giữa cấu trúc sạch (Clean Architecture) và sự đơn giản của mã nguồn bài tập.

---

### Entry #: 002
- **Prompt Type:** `DECISION`
- **Stage/Component:** Algorithms (CT)
- **Problem/Context:** Xây dựng thuật toán kiểm tra phòng trống khả dụng trong khoảng thời gian khách chọn để ngăn ngừa tình trạng đặt phòng trùng thời gian (Overbooking).
- **Prompt to AI:** `"Write a LINQ query in Entity Framework Core to check if a specific room is available (not overlapping with existing bookings) for a given CheckIn and CheckOut date range."`
- **AI Response (Summary):** AI đề xuất một câu truy vấn LINQ so sánh thời gian nhận và trả phòng: `booking.CheckInDate <= checkOut && booking.CheckOutDate >= checkIn`. Nếu có bất kỳ bản ghi nào trùng khớp với phòng đó và có trạng thái "Confirmed", phòng được coi là không khả dụng.
- **Human Delta & Reflection:**
  * **Critical Thinking:** Thuật toán của AI bị lỗi logic biên (boundary bug). Quy định của khách sạn là khách trả phòng trước 12:00 trưa và khách mới nhận phòng sau 14:00 chiều cùng ngày. Điều này nghĩa là ngày CheckOut của đơn đặt trước có thể trùng khớp với ngày CheckIn của đơn đặt sau. Phép so sánh chứa dấu bằng (`<=` và `>=`) của AI sẽ coi đây là bị trùng lịch (không khả dụng), dẫn đến từ chối khách đặt phòng một cách sai lầm.
  * **Contextualization:** Ngoài ra, AI chỉ lọc những đơn có trạng thái "Confirmed". Trong nghiệp vụ thực tế, các đơn hàng ở trạng thái "Pending" (chờ thanh toán), "CheckedIn" (đang ở), hay "CancelRequested" (chờ duyệt hủy) vẫn đang giữ chỗ phòng, chỉ khi đơn ở trạng thái "Cancelled" hoặc "CheckedOut" thì phòng mới thực sự được giải phóng để khách khác đặt.
  * **Creative Synthesis:** Tôi sửa đổi câu truy vấn LINQ sử dụng so sánh nghiêm ngặt `<` và `>` để xử lý chính xác trường hợp CheckIn trùng ngày CheckOut của phòng trước đó: `br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn`. Tôi cũng thêm điều kiện lọc danh sách các trạng thái đặt phòng đang giữ chỗ bao gồm: `Pending`, `Confirmed`, `CheckedIn`, và `CancelRequested`.
  * **Decision Ownership:** Áp dụng logic truy vấn sửa đổi này vào hàm [IsRoomAvailableAsync](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking.Application/Services/BookingService.cs#L18-L29) của [BookingService.cs](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking.Application/Services/BookingService.cs). Đây là thuật toán cốt lõi bảo đảm tính toàn vẹn của nghiệp vụ đặt phòng.

---

### Entry #: 003
- **Prompt Type:** `PROBLEM-SOLVING`
- **Stage/Component:** Pattern Recognition (CT)
- **Problem/Context:** Ứng dụng ném ngoại lệ `SqlException: The INSERT statement conflicted with the FOREIGN KEY constraint 'FK_Rooms_RoomTypes'` khi khởi chạy phương thức seeding dữ liệu phòng (Rooms) và loại phòng (RoomTypes) trong cơ sở dữ liệu.
- **Prompt to AI:** `"Why does DbSeeder fail with SqlException: The INSERT statement conflicted with the FOREIGN KEY constraint 'FK_Rooms_RoomTypes' when seeding database?"`
- **AI Response (Summary):** AI giải thích rằng lỗi xảy ra do mã nguồn cố gắng chèn các bản ghi `Room` tham chiếu đến `RoomTypeId` chưa tồn tại trong bảng `RoomTypes`. AI đề xuất giải pháp là gán cứng các giá trị khóa ngoại `RoomTypeId` (ví dụ: 1, 2, 3) tương ứng khi seed.
- **Human Delta & Reflection:**
  * **Critical Thinking:** AI đã nhận diện đúng nguyên nhân lỗi (vi phạm thứ tự chèn dữ liệu). Tuy nhiên, giải pháp gán cứng ID của AI rất dễ lỗi nếu cơ sở dữ liệu sử dụng cột ID tự tăng (Identity Column) trong SQL Server, vì cơ chế SQL Server không đảm bảo ID bắt đầu từ 1 sau khi xóa dữ liệu hoặc khi chuyển sang máy tính khác của thành viên dự án.
  * **Contextualization:** Bảng `RoomTypes` có thuộc tính ID tự tăng. Việc gán cứng giá trị ID thủ công sẽ bị SQL Server từ chối chèn trừ khi bật chế độ `IDENTITY_INSERT ON`.
  * **Creative Synthesis:** Tôi áp dụng mẫu thiết kế tuần tự kết hợp bắt ID động: Chèn `RoomTypes` trước, gọi `_context.SaveChangesAsync()` để SQL Server tự tạo ID. Sau đó lấy danh sách các thực thể đã lưu ra để đọc ID động vừa sinh ra từ DB, từ đó gán chính xác các ID này cho các đối tượng `Room` tương ứng trước khi chèn danh sách `Rooms`.
  * **Decision Ownership:** Viết lại logic trong [DbSeeder.cs](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking.Application/Data/DbSeeder.cs) để nhận dạng các khóa ID động sau khi lưu thay vì gán cứng. Điều này giúp chạy seed dữ liệu thành công trên mọi máy tính của thành viên dự án mà không gặp lỗi khóa ngoại.

---

### Entry #: 004
- **Prompt Type:** `DECISION`
- **Stage/Component:** Abstraction (CT)
- **Problem/Context:** Thiết lập cơ chế bảo mật và phân quyền (Authorization) dựa trên vai trò người dùng (Admin, Customer, FrontDesk) sử dụng Cookie Authentication tích hợp sẵn của .NET Core mà không dùng thư viện ASP.NET Core Identity cồng kềnh.
- **Prompt to AI:** `"How to implement role-based authorization using ASP.NET Core Cookie Authentication without Identity framework? I want to restrict Admin controllers to only admin users."`
- **AI Response (Summary):** AI hướng dẫn cấu hình Cookie Authentication trong `Program.cs` bằng `builder.Services.AddAuthentication()`. Khi đăng nhập thành công, tạo một `ClaimsIdentity` chứa claim `ClaimTypes.Role` đại diện cho vai trò người dùng và dùng thuộc tính `[Authorize(Roles = "Admin")]` trên controller.
- **Human Delta & Reflection:**
  * **Critical Thinking:** AI đưa ra các bước cấu hình cơ bản tốt. Tuy nhiên, AI đã bỏ qua cấu hình xử lý trường hợp Access Denied (từ chối truy cập). Nếu không thiết lập `AccessDeniedPath`, khi người dùng có vai trò "Customer" cố gắng truy cập đường dẫn hành chính của Admin, trình duyệt sẽ bị chuyển hướng mặc định tới đường dẫn không tồn tại hoặc lỗi 403 thô của IIS, ảnh hưởng xấu tới trải nghiệm người dùng.
  * **Contextualization:** Hệ thống có phân cấp vai trò rất rõ ràng: Admin (quản lý hệ thống), FrontDesk (lễ tân xử lý đơn), và Customer (khách hàng xem và đặt phòng). Trải nghiệm chuyển hướng thân thiện khi không đủ quyền truy cập là bắt buộc trong thiết kế UI/UX của dự án.
  * **Creative Synthesis:** Tôi cấu hình chi tiết hơn trong [Program.cs](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking/Program.cs#L24-L30) bằng cách thiết lập rõ ràng:
    ```csharp
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LoginPath = "/Account/Login";
    ```
    Đồng thời tạo một action `AccessDenied` trong [AccountController.cs](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking/Controllers/AccountController.cs) để trả về một view thông báo lỗi được thiết kế giao diện rõ ràng kèm liên kết định hướng lại người dùng.
  * **Decision Ownership:** Quyết định tùy biến luồng chuyển hướng AccessDenied để ngăn chặn trải nghiệm lỗi HTTP 403 thô cho người dùng không có quyền truy cập trang quản trị.

---

### Entry #: 005
- **Prompt Type:** `VERIFICATION` (Hallucination Detection)
- **Stage/Component:** Algorithms (CT) + Research Stage (RBL)
- **Problem/Context:** AI gợi ý giải pháp xử lý kết quả thanh toán từ cổng thanh toán (Mock Gateway Callback) bằng phương thức GET và thực hiện cập nhật cơ sở dữ liệu ngay lập tức, tạo ra lỗ hổng bảo mật vô cùng nghiêm trọng.
- **Prompt to AI:** `"Write a controller action for a Mock Payment Gateway Callback that updates the booking status to 'Confirmed' and creates a payment record."`
- **AI Response (Summary):** AI cung cấp một phương thức GET tại route `/Payment/Callback?bookingId={id}&status=Success` và trực tiếp thực hiện lệnh tìm kiếm Booking trong Database để cập nhật `Status = "Confirmed"`.
- **Human Delta & Reflection:**
  * **Critical Thinking:** **[Hallucination - Security Logic Error]** AI đã mắc lỗi nghiêm trọng về logic an toàn thông tin (Oversimplification & Context Misunderstanding). Việc cho phép một endpoint GET cập nhật trạng thái thanh toán nhạy cảm trực tiếp từ tham số URL mà không có chữ ký mã hóa (Checksum/Signature) hay cơ chế xác thực nguồn gốc yêu cầu (IPN) sẽ mở đường cho lỗ hổng "Bypass Payment". Bất kỳ khách hàng nào cũng có thể tự thay đổi URL trên trình duyệt để đánh dấu đơn hàng của mình là đã thanh toán thành công mà không cần trả tiền thực tế.
  * **Contextualization:** Trong thiết kế ứng dụng web thương mại, phương thức GET chỉ được dùng để đọc và hiển thị dữ liệu (Safe and Idempotent), các thao tác thay đổi trạng thái giao dịch phải thông qua phương thức POST bảo mật và có kiểm thử dữ liệu nghiêm ngặt từ phía server.
  * **Creative Synthesis:** Tôi phát hiện lỗi thiết kế của AI và quyết định tái cấu trúc lại luồng thanh toán giả lập như sau:
    1. Chỉ cập nhật cơ sở dữ liệu (tạo bản ghi Payment và cập nhật trạng thái đặt phòng thành Confirmed) ở phương thức POST `ProcessPayment` sau khi người dùng điền thông tin và xác nhận gửi đi.
    2. Sau khi cập nhật DB thành công từ POST, thực hiện chuyển hướng (`RedirectToAction`) sang phương thức GET `Callback` chỉ làm nhiệm vụ hiển thị giao diện báo kết quả (Read-only) dựa trên dữ liệu đã lưu thực tế trong cơ sở dữ liệu, loại bỏ hoàn toàn khả năng người dùng can thiệp tham số GET URL để thay đổi trạng thái giao dịch.
  * **Decision Ownership:** Loại bỏ phương thức cập nhật dữ liệu qua GET của AI, cấu trúc lại luồng thanh toán an toàn tại [PaymentController.cs](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking/Controllers/PaymentController.cs#L67-L107).

---

### Entry #: 006
- **Prompt Type:** `DECISION`
- **Stage/Component:** Pattern Recognition (CT)
- **Problem/Context:** Ánh xạ Database View `vw_MonthlyRevenue` (báo cáo doanh thu tháng) vào Entity Framework Core để truy vấn lấy dữ liệu vẽ biểu đồ báo cáo doanh thu mà không có cột Khóa chính (Primary Key).
- **Prompt to AI:** `"How do I map a SQL Database View named 'vw_MonthlyRevenue' in Entity Framework Core since views do not have a primary key?"`
- **AI Response (Summary):** AI gợi ý sử dụng cơ chế Keyless Entity Type trong EF Core bằng cách khai báo thuộc tính `.HasNoKey()` trong hàm `OnModelCreating` của DbContext để EF Core hiểu đây là đối tượng chỉ đọc và không cố gắng tìm kiếm trường khóa chính ID.
- **Human Delta & Reflection:**
  * **Critical Thinking:** Đề xuất của AI là đúng về mặt kỹ thuật sử dụng `.HasNoKey()`. Tuy nhiên, AI quên không nhắc đến việc phải chỉ định rõ tên của View SQL bằng cách gọi thêm `.ToView("vw_MonthlyRevenue")`. Nếu thiếu phương thức này, EF Core sẽ mặc định tìm kiếm một bảng vật lý có tên trùng với tên DbSet là `VwMonthlyRevenues`, gây ra lỗi không tìm thấy đối tượng cơ sở dữ liệu khi chạy ứng dụng.
  * **Contextualization:** Cơ sở dữ liệu hiện tại có một View tên là `vw_MonthlyRevenue` được định nghĩa trong file script SQL để tổng hợp dữ liệu doanh thu qua các năm/tháng từ bảng Payments và Bookings.
  * **Creative Synthesis:** Tôi đã áp dụng mẫu khai báo thực thể không khóa và bổ sung cấu hình Fluent API đầy đủ:
    ```csharp
    modelBuilder.Entity<VwMonthlyRevenue>(entity =>
    {
        entity.HasNoKey().ToView("vw_MonthlyRevenue");
    });
    ```
    trong [HotelBookingDbContext.cs](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking.Infrastructure/HotelBookingDbContext.cs#L251-L258).
  * **Decision Ownership:** Áp dụng cấu hình ánh xạ View này giúp hệ thống truy vấn dữ liệu báo cáo doanh thu trực tiếp từ Database View một cách mượt mà thông qua LINQ mà không cần viết các câu lệnh SQL thuần phức tạp.

---

### Entry #: 007
- **Prompt Type:** `DECISION`
- **Stage/Component:** Decomposition (CT)
- **Problem/Context:** Phân rã chức năng tải ảnh phòng lên hệ thống và đưa ra quyết định lưu trữ ảnh (lưu nhị phân byte array trong Database vs lưu file vật lý trong thư mục wwwroot và lưu đường dẫn ảnh trong DB).
- **Prompt to AI:** `"Should I store uploaded room images as byte arrays in the SQL Server database (VARBINARY) or save them as files in wwwroot and store the file paths? Give me the pros/cons for an MVC site."`
- **AI Response (Summary):** AI phân tích: Lưu trong DB giúp dữ liệu đồng bộ, dễ backup nhưng làm phình to DB cực nhanh và tải ảnh chậm. Lưu trong file system (`wwwroot`) giúp giảm tải cho DB, tăng tốc độ phản hồi tĩnh của web, nhưng cần tự quản lý việc xóa file khi xóa phòng để tránh rác đĩa.
- **Human Delta & Reflection:**
  * **Critical Thinking:** AI phân tích ưu nhược điểm chính xác. Song, AI chưa cảnh báo về lỗ hổng bảo mật tải tập tin (File Upload Vulnerability) như chèn mã độc thông qua việc giữ nguyên tên file gốc của người dùng hoặc các ký tự đặc biệt gây lỗi đường dẫn lưu trữ.
  * **Contextualization:** Trong trang quản trị Admin, người dùng quản lý có quyền tải lên nhiều hình ảnh của một phòng. Nếu tải lên file trùng tên sẽ gây ghi đè ảnh phòng khác.
  * **Creative Synthesis:** Tôi quyết định chọn giải pháp lưu file trên ổ đĩa (`wwwroot/images/rooms/`). Để giải quyết vấn đề trùng tên và bảo mật, tôi áp dụng giải thuật sinh tên ngẫu nhiên: sử dụng `Guid.NewGuid().ToString() + Path.GetExtension(file.FileName)` để đổi tên file thành chuỗi duy nhất, sau đó dùng static file middleware để phục vụ ảnh. Tôi cũng tạo cơ chế tự động xóa file vật lý trên đĩa khi bản ghi ảnh phòng tương ứng bị Admin xóa trong Database.
  * **Decision Ownership:** Triển khai giải pháp lưu ảnh dạng file vật lý trên đĩa tại [AdminRoomController.cs](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking/Controllers/AdminRoomController.cs), giúp tối ưu hóa hiệu năng truy xuất ảnh tĩnh và giảm tải dung lượng lưu trữ cơ sở dữ liệu.

---

### Entry #: 008
- **Prompt Type:** `DECISION`
- **Stage/Component:** Abstraction (CT)
- **Problem/Context:** Lựa chọn giải thuật và cơ chế băm mật khẩu (Password Hashing) an toàn khi khách hàng đăng ký tài khoản mới để tránh lưu mật khẩu dạng văn bản gốc (plain text) gây mất an toàn thông tin.
- **Prompt to AI:** `"Write a helper method to hash passwords using BCrypt or SHA256 in C# for user registration and authentication."`
- **AI Response (Summary):** AI gợi ý sử dụng thư viện ngoài `BCrypt.Net-Next` hoặc tự viết một lớp tiện ích mã hóa sử dụng thuật toán `SHA256` kết hợp với muối ngẫu nhiên (Salt) được lưu trữ cùng mật khẩu.
- **Human Delta & Reflection:**
  * **Critical Thinking:** Việc sử dụng SHA256 tự chế muối tuy khả thi nhưng dễ phát sinh lỗi logic bảo mật nếu sinh muối không đủ ngẫu nhiên. Trong khi đó, việc sử dụng `BCrypt` yêu cầu cài đặt thêm thư viện NuGet ngoài. AI đã bỏ sót một công cụ tích hợp sẵn rất mạnh mẽ của ASP.NET Core là lớp trừu tượng `PasswordHasher<TUser>` thuộc namespace `Microsoft.AspNetCore.Identity`.
  * **Contextualization:** Dự án sử dụng mô hình MVC chuẩn của .NET Core. Việc tận dụng tối đa các thư viện hệ thống có sẵn của Microsoft sẽ giúp giảm phụ thuộc bên ngoài và đảm bảo chuẩn bảo mật đã được kiểm chứng tốt hơn.
  * **Creative Synthesis:** Tôi bỏ qua gợi ý viết thuật toán SHA256 thủ công của AI. Thay vào đó, tôi khai báo sử dụng trực tiếp lớp trừu tượng `Microsoft.AspNetCore.Identity.PasswordHasher<User>` trong `AuthService`. Lớp này tự động xử lý tạo muối ngẫu nhiên, áp dụng thuật toán PBKDF2 với độ phức tạp cao, tự lưu muối chung trong chuỗi băm để tiện cho việc xác thực lại.
  * **Decision Ownership:** Triển khai `PasswordHasher<User>` trong [AuthService.cs](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking.Application/Services/AuthService.cs) phục vụ cho đăng ký và đăng nhập, đảm bảo mật khẩu được mã hóa an toàn ở mức cao nhất theo chuẩn của Microsoft.

---

### Entry #: 009
- **Prompt Type:** `PROBLEM-SOLVING`
- **Stage/Component:** Algorithms (CT)
- **Problem/Context:** Xử lý tranh chấp dữ liệu đồng thời (Concurrency Conflict) khi hai người dùng nhấn nút Đặt phòng cho cùng một phòng vào cùng một phần nghìn giây dẫn đến nguy cơ đặt trùng phòng.
- **Prompt to AI:** `"How do I handle concurrency conflicts in Entity Framework Core to prevent two users from booking the same room at the exact same millisecond?"`
- **AI Response (Summary):** AI khuyên dùng cơ chế Optimistic Concurrency bằng cách thêm trường `RowVersion` (Timestamp) vào bảng `Room` để kiểm tra phiên bản dữ liệu trước khi cập nhật trạng thái phòng.
- **Human Delta & Reflection:**
  * **Critical Thinking:** Gợi ý của AI là một lỗi hiểu sai bối cảnh hệ thống (Context Misunderstanding). Trong thiết kế cơ sở dữ liệu của dự án, khi người dùng đặt phòng thành công, chúng ta thêm mới bản ghi vào bảng `Bookings` và `BookingRooms`, còn trạng thái vật lý của bảng `Rooms` vẫn giữ nguyên là "Available" (chỉ cập nhật sang "Occupied" khi khách làm thủ tục check-in tại quầy). Do đó, việc áp dụng `RowVersion` trên bảng `Rooms` hoàn toàn không có tác dụng ngăn cản hai luồng đồng thời thêm hai đơn đặt phòng chồng chéo vào bảng `Bookings`.
  * **Contextualization:** Cần một giải pháp khóa đồng bộ ở mức cơ sở dữ liệu (Database-level Locking) hoặc một phiên giao dịch nghiêm ngặt để đảm bảo quá trình kiểm tra phòng trống và ghi nhận đặt phòng được thực hiện tuần tự tuyệt đối (Atomic Operation).
  * **Creative Synthesis:** Tôi đã loại bỏ giải pháp `RowVersion` của AI. Thay vào đó, tôi sử dụng cơ chế Transaction của EF Core với mức cô lập nghiêm ngặt nhất là `IsolationLevel.Serializable` trong hàm `CreateBookingAsync`:
    ```csharp
    using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
    try {
        // 1. Kiểm tra phòng trống
        // 2. Thêm đơn đặt phòng mới
        // 3. SaveChanges và Commit
    } catch {
        await transaction.RollbackAsync();
    }
    ```
    Điều này đảm bảo SQL Server sẽ khóa bảng liên quan trong suốt quá trình giao dịch, ngăn chặn tuyệt đối tình trạng đặt phòng trùng lặp tại cùng một thời điểm.
  * **Decision Ownership:** Cấu hình sử dụng Transaction Isolation Level Serializable tại [BookingService.cs](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking.Application/Services/BookingService.cs) để bảo vệ tính nhất quán tối cao cho hệ thống giao dịch đặt phòng trực tuyến.

---

## III. MINH CHỨNG PHÁT HIỆN HALLUCINATION (HALLUCINATION EVIDENCE)
Như chi tiết trong **Entry #: 005**, AI đã đưa ra một hướng dẫn gây ra lỗ hổng bảo mật nghiêm trọng liên quan đến kiểm soát luồng giao dịch thanh toán:

- **Loại Hallucination:** `Logic Error` / `Context Misunderstanding` (Sai sót trong thiết kế luồng nghiệp vụ bảo mật của ứng dụng web thực tế).
- **Mã nguồn bị lỗi do AI đề xuất (Bản phác thảo ban đầu):**
  ```csharp
  // GET: /Payment/Callback?bookingId=5&status=Success
  [HttpGet]
  public async Task<IActionResult> Callback(int bookingId, string status)
  {
      if (status == "Success")
      {
          var booking = await _context.Bookings.FindAsync(bookingId);
          booking.Status = "Confirmed"; // Lỗi nghiêm trọng: Cập nhật DB trực tiếp bằng GET param công khai
          await _context.SaveChangesAsync();
      }
      return View();
  }
  ```
- **Hành động khắc phục (Corrective Action):**
  Chuyển toàn bộ logic cập nhật trạng thái giao dịch sang phương thức `HttpPost` bảo mật của hành động [ProcessPayment](file:///c:/Users/Lenovo/Downloads/HotelBooking/HotelBooking/Controllers/PaymentController.cs#L68-L97), còn hàm `Callback` GET chỉ thực hiện đọc và hiển thị kết quả từ cơ sở dữ liệu:
  ```csharp
  // POST: /Payment/ProcessPayment
  [HttpPost]
  public async Task<IActionResult> ProcessPayment(int bookingId, string paymentMethod, string cardNumber)
  {
      // ... Kiểm tra thẻ hợp lệ ...
      // Thực hiện cập nhật trạng thái trong môi trường POST an toàn
      var payment = await _paymentService.CreatePaymentRecordAsync(bookingId, paymentMethod, booking.TotalPrice, transactionId, "Success");
      return RedirectToAction("Callback", new { bookingId, status = "Success", transactionId = payment.TransactionId, paymentMethod = payment.PaymentMethod });
  }

  // GET: /Payment/Callback (Read-Only)
  [HttpGet]
  public IActionResult Callback(int bookingId, string status, string transactionId, string paymentMethod)
  {
      ViewBag.BookingId = bookingId;
      ViewBag.Status = status; // Chỉ hiển thị giao diện, không làm thay đổi trạng thái Database
      // ...
      return View();
  }
  ```

---

## IV. TỰ KIỂM TRA CHECKLIST (SELF-EVALUATION)
- [x] Đã ghi nhận đầy đủ 9 Core Prompts (nằm trong phạm vi quy định 8 - 12).
- [x] Mỗi cấu phần DTC (Decomposition, Pattern Recognition, Abstraction, Algorithms) đều có ít nhất 1 core prompt tương ứng.
- [x] Có ít nhất 1 trường hợp phát hiện và giải quyết Hallucination của AI (Entry 005).
- [x] Mỗi entry đều có cấu trúc 7 phần đầy đủ, đặc biệt phần "Human Delta & Reflection" trả lời rõ ràng cả 4 câu hỏi bắt buộc.
- [x] Liên kết tệp tin mã nguồn có cấu trúc `file:///` hoạt động chính xác.
