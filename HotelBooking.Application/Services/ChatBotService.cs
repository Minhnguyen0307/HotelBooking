using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HotelBooking.Application.Services
{
    public class ChatBotService : IChatBotService
    {
        private readonly HotelBookingDbContext _context;
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(25) };

        public ChatBotService(HotelBookingDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public Task<List<string>> GetInitialSuggestionsAsync()
        {
            var list = new List<string>
            {
                "Thủ tục nhận phòng & trả phòng thế nào?",
                "Tôi muốn tìm phòng cho 2 người ở Mỹ Khê",
                "Các bước đặt phòng và phương thức thanh toán",
                "Số điện thoại lễ tân và địa chỉ các cơ sở",
                "Có mã giảm giá nào đang áp dụng không?",
                "Chính sách hủy phòng và phụ thu thêm người"
            };
            return Task.FromResult(list);
        }

        public async Task<ChatResponseDto> ProcessChatAsync(ChatRequestDto request)
        {
            var rawInput = request.Message?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return new ChatResponseDto
                {
                    Reply = "Dạ, em có thể hỗ trợ gì cho quý khách về đặt phòng, thủ tục nhận/trả phòng hoặc thông tin các cơ sở của HBS Hotel ạ?",
                    SuggestedQuestions = await GetInitialSuggestionsAsync()
                };
            }

            var lower = rawInput.ToLowerInvariant();

            // 1. Kiểm tra xem người dùng có nhu cầu tìm / xem phòng không
            var isRoomSearch = CheckIsRoomSearchIntent(lower);
            List<ChatRoomRecommendationDto> recommendedRooms = new();

            if (isRoomSearch)
            {
                recommendedRooms = await SearchMatchingRoomsAsync(lower);
            }

            // 2. Thử gọi Gemini AI nếu có cấu hình API Key
            var geminiApiKey = _configuration["Gemini:ApiKey"]
                ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            if (!string.IsNullOrWhiteSpace(geminiApiKey))
            {
                try
                {
                    var geminiReply = await CallGeminiAsync(geminiApiKey, rawInput, request.History, recommendedRooms);
                    if (!string.IsNullOrWhiteSpace(geminiReply))
                    {
                        return new ChatResponseDto
                        {
                            Reply = geminiReply,
                            SuggestedQuestions = GenerateContextualSuggestions(lower, recommendedRooms.Count > 0),
                            RecommendedRooms = recommendedRooms
                        };
                    }
                }
                catch
                {
                    // Fallback to internal knowledge engine
                }
            }

            // 3. Bộ máy tri thức khách sạn HBS Hotel (Local Rule & Semantic Engine)
            var response = await BuildSmartResponseAsync(lower, rawInput, recommendedRooms);
            return response;
        }

        private bool CheckIsRoomSearchIntent(string text)
        {
            string[] keywords =
            {
                "tìm phòng", "gợi ý phòng", "có phòng", "phòng nào", "xem phòng",
                "đặt phòng", "giá phòng", "phòng đôi", "phòng đơn", "phòng gia đình",
                "phòng suite", "phòng deluxe", "phòng standard", "view biển", "hướng biển",
                "sát biển", "bồn tắm", "sơn trà", "mỹ khê", "hải châu", "ngũ hành sơn",
                "liên chiểu", "ở đà nẵng", "cho 2 người", "cho 4 người", "cho 3 người",
                "cho 1 người", "khách", "giá rẻ", "phòng đẹp", "ban công"
            };

            return keywords.Any(k => text.Contains(k));
        }

        private async Task<List<ChatRoomRecommendationDto>> SearchMatchingRoomsAsync(string query)
        {
            var roomsQuery = _context.Rooms
                .Include(r => r.Property)
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Where(r => r.Status != "Maintenance" && r.HousekeepingStatus != "Maintenance");

            // Lọc theo cơ sở
            if (query.Contains("mỹ khê") || query.Contains("my khe"))
                roomsQuery = roomsQuery.Where(r => r.Property.Name.Contains("Mỹ Khê"));
            else if (query.Contains("sơn trà") || query.Contains("son tra"))
                roomsQuery = roomsQuery.Where(r => r.Property.Name.Contains("Sơn Trà"));
            else if (query.Contains("hải châu") || query.Contains("hai chau"))
                roomsQuery = roomsQuery.Where(r => r.Property.Name.Contains("Hải Châu"));
            else if (query.Contains("ngũ hành sơn") || query.Contains("ngu hanh son"))
                roomsQuery = roomsQuery.Where(r => r.Property.Name.Contains("Ngũ Hành Sơn"));
            else if (query.Contains("liên chiểu") || query.Contains("lien chieu"))
                roomsQuery = roomsQuery.Where(r => r.Property.Name.Contains("Liên Chiểu"));

            // Lọc theo loại phòng
            if (query.Contains("standard") || query.Contains("tiêu chuẩn"))
                roomsQuery = roomsQuery.Where(r => r.RoomType.TypeName == "Standard");
            else if (query.Contains("deluxe") || query.Contains("cao cấp"))
                roomsQuery = roomsQuery.Where(r => r.RoomType.TypeName == "Deluxe");
            else if (query.Contains("suite") || query.Contains("vip") || query.Contains("gia đình"))
                roomsQuery = roomsQuery.Where(r => r.RoomType.TypeName == "Suite");

            // Lọc theo số lượng khách
            var guestMatch = Regex.Match(query, @"(\d+)\s*(người|khách)");
            if (guestMatch.Success && int.TryParse(guestMatch.Groups[1].Value, out int guests))
            {
                roomsQuery = roomsQuery.Where(r => r.RoomType.MaxGuests >= guests);
            }
            else if (query.Contains("gia đình") || query.Contains("4 người"))
            {
                roomsQuery = roomsQuery.Where(r => r.RoomType.MaxGuests >= 4);
            }

            // Lọc theo bồn tắm hoặc view biển trong mô tả
            if (query.Contains("bồn tắm") || query.Contains("bon tam"))
                roomsQuery = roomsQuery.Where(r => r.Description != null && r.Description.Contains("bồn tắm"));

            if (query.Contains("biển") || query.Contains("view biển") || query.Contains("sát biển"))
                roomsQuery = roomsQuery.Where(r => (r.Description != null && r.Description.Contains("biển")) || r.Property.Area.Contains("Mỹ Khê") || r.Property.Area.Contains("Sơn Trà"));

            var list = await roomsQuery
                .OrderBy(r => r.RoomType.BasePrice)
                .Take(4)
                .Select(r => new ChatRoomRecommendationDto
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    TypeName = r.RoomType.TypeName,
                    BasePrice = r.RoomType.BasePrice,
                    MaxGuests = r.RoomType.MaxGuests,
                    IncludedGuests = OccupancyPricing.For(r.RoomType.TypeName, r.RoomType.MaxGuests).IncludedGuests,
                    ExtraGuestFee = OccupancyPricing.For(r.RoomType.TypeName, r.RoomType.MaxGuests).ExtraGuestFeePerNight,
                    PropertyName = r.Property.Name,
                    Area = r.Property.Area,
                    Address = r.Property.Address,
                    PrimaryImageUrl = r.RoomImages.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                        ?? r.RoomImages.Select(i => i.ImageUrl).FirstOrDefault()
                        ?? "/images/room_standard.png",
                    Description = r.Description
                })
                .ToListAsync();

            // Nếu lọc quá chặt không có kết quả, lấy 3 phòng tiêu biểu
            if (list.Count == 0)
            {
                list = await _context.Rooms
                    .Include(r => r.Property)
                    .Include(r => r.RoomType)
                    .Include(r => r.RoomImages)
                    .Where(r => r.Status != "Maintenance" && r.HousekeepingStatus != "Maintenance")
                    .Take(3)
                    .Select(r => new ChatRoomRecommendationDto
                    {
                        RoomId = r.RoomId,
                        RoomNumber = r.RoomNumber,
                        TypeName = r.RoomType.TypeName,
                        BasePrice = r.RoomType.BasePrice,
                        MaxGuests = r.RoomType.MaxGuests,
                        IncludedGuests = OccupancyPricing.For(r.RoomType.TypeName, r.RoomType.MaxGuests).IncludedGuests,
                        ExtraGuestFee = OccupancyPricing.For(r.RoomType.TypeName, r.RoomType.MaxGuests).ExtraGuestFeePerNight,
                        PropertyName = r.Property.Name,
                        Area = r.Property.Area,
                        Address = r.Property.Address,
                        PrimaryImageUrl = r.RoomImages.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                            ?? "/images/room_standard.png",
                        Description = r.Description
                    })
                    .ToListAsync();
            }

            return list;
        }

        private async Task<ChatResponseDto> BuildSmartResponseAsync(string lower, string rawInput, List<ChatRoomRecommendationDto> recommendedRooms)
        {
            var sb = new StringBuilder();
            var suggestions = new List<string>();

            // 1. Nhận phòng (Check-in) & Trả phòng (Check-out)
            if (lower.Contains("nhận phòng") || lower.Contains("check in") || lower.Contains("check-in") || lower.Contains("checkin")
                || lower.Contains("trả phòng") || lower.Contains("check out") || lower.Contains("check-out") || lower.Contains("checkout")
                || lower.Contains("mấy giờ") || lower.Contains("thời gian nhận") || lower.Contains("thời gian trả"))
            {
                sb.AppendLine("🏨 **Quy định giờ Nhận phòng & Trả phòng tại HBS Luxury Hotel:**");
                sb.AppendLine("• **Giờ nhận phòng (Check-in):** Từ **14:00** chiều.");
                sb.AppendLine("• **Giờ trả phòng (Check-out):** Trước **12:00** trưa hôm sau.");
                sb.AppendLine();
                sb.AppendLine("📋 **Giấy tờ cần chuẩn bị khi làm thủ tục:**");
                sb.AppendLine("• Bản gốc **CCCD / CMND** hoặc **Hộ chiếu (Passport)** còn hạn của các khách lưu trú.");
                sb.AppendLine("• Khách sạn có hỗ trợ xuất trình **VNeID** (định danh điện tử mức 2).");
                sb.AppendLine("• Mã đơn đặt phòng (gửi qua email hoặc xem trong mục *Đơn của tôi*).");
                sb.AppendLine();
                sb.AppendLine("🧳 **Nhận phòng sớm / Trả phòng muộn / Giữ hành lý:**");
                sb.AppendLine("• **Nhận phòng sớm (Early Check-in):** Hỗ trợ miễn phí nếu phòng đã sẵn sàng, hoặc phụ thu theo giờ nếu nhận trước 10:00 sáng.");
                sb.AppendLine("• **Trả phòng muộn (Late Check-out):** Trả từ 12:00 - 18:00 phụ thu 30% - 50% tiền phòng; sau 18:00 tính 1 đêm tiền phòng (tùy tình trạng phòng).");
                sb.AppendLine("• Quý khách có thể **gửi hành lý hoàn toàn miễn phí** tại quầy lễ tân nếu đến sớm hoặc muốn dạo chơi trước khi ra sân bay!");

                suggestions.Add("Các phương thức thanh toán tiền phòng?");
                suggestions.Add("Hotline liên hệ lễ tân khi cần đến sớm?");
                suggestions.Add("Tìm phòng còn trống hôm nay");
            }
            // 2. Cách đặt phòng & quy trình thủ tục
            else if (lower.Contains("cách đặt") || lower.Contains("thủ tục đặt") || lower.Contains("hướng dẫn đặt") || lower.Contains("các bước đặt") || lower.Contains("làm sao để đặt"))
            {
                sb.AppendLine("📝 **Hướng dẫn 5 bước đặt phòng nhanh chóng tại HBS Hotel:**");
                sb.AppendLine("1. **Chọn thời gian:** Nhập ngày **Nhận phòng (Check-in)** và **Trả phòng (Check-out)** tại thanh tìm kiếm.");
                sb.AppendLine("2. **Chọn cơ sở & số khách:** Lựa chọn trong 5 cơ sở tại Đà Nẵng (Mỹ Khê, Sơn Trà, Hải Châu, Ngũ Hành Sơn, Liên Chiểu) và số người.");
                sb.AppendLine("3. **Chọn phòng ưng ý:** Xem các hạng phòng (Standard, Deluxe, Suite), hình ảnh, tiện nghi và nhấn **Xem chi tiết / Đặt phòng**.");
                sb.AppendLine("4. **Nhập thông tin:** Điền họ tên, số điện thoại, ghi chú đặc biệt và nhập **Mã giảm giá** (nếu có).");
                sb.AppendLine("5. **Xác nhận thanh toán:** Quý khách có thể chọn **Thanh toán ngay trực tuyến** hoặc **Thanh toán tại quầy lễ tân khi nhận phòng**.");
                sb.AppendLine();
                sb.AppendLine("✨ *Sau khi đặt thành công, hệ thống sẽ gửi email xác nhận kèm mã đặt phòng để quý khách tiện tra cứu.*");

                suggestions.Add("Mã giảm giá hiện có?");
                suggestions.Add("Tìm phòng ở cơ sở Mỹ Khê");
                suggestions.Add("Chính sách hủy phòng thế nào?");
            }
            // 3. Liên hệ ai / Hotline / Địa chỉ các cơ sở
            else if (lower.Contains("liên lạc") || lower.Contains("liên hệ") || lower.Contains("gặp ai") || lower.Contains("gọi ai")
                || lower.Contains("số điện thoại") || lower.Contains("sđt") || lower.Contains("hotline") || lower.Contains("lễ tân")
                || lower.Contains("ở đâu") || lower.Contains("địa chỉ") || lower.Contains("cơ sở") || lower.Contains("email"))
            {
                sb.AppendLine("📞 **Thông tin liên lạc & Hỗ trợ khách hàng (Trực 24/7):**");
                sb.AppendLine("• **Tổng đài Hotline:** **1900 8888** hoặc **0236 3888 999**");
                sb.AppendLine("• **Email hỗ trợ:** support@hbshotel.vn / khangphuc1903@gmail.com");
                sb.AppendLine("• **Bộ phận tiếp nhận:** Quầy lễ tân tại sảnh chính luôn có nhân viên hỗ trợ check-in, gọi xe, đặt tour và giải quyết sự cố 24/24.");
                sb.AppendLine();
                sb.AppendLine("📍 **Hệ thống 5 cơ sở HBS Luxury Hotel tại Đà Nẵng:**");
                sb.AppendLine("1. **HBS Mỹ Khê:** 123 Võ Nguyên Giáp, Sơn Trà *(Sát biển Mỹ Khê, có ban công view biển)*");
                sb.AppendLine("2. **HBS Sơn Trà:** 88 Yết Kiêu, Thọ Quang, Sơn Trà *(Bán đảo Sơn Trà, bungalow nghỉ dưỡng)*");
                sb.AppendLine("3. **HBS Hải Châu:** 50 Bạch Đằng, Hải Châu *(Trung tâm, view sông Hàn & cầu Rồng)*");
                sb.AppendLine("4. **HBS Ngũ Hành Sơn:** 15 Trường Sa, Ngũ Hành Sơn *(Gần danh thắng Non Nước & bãi tắm)*");
                sb.AppendLine("5. **HBS Liên Chiểu:** 200 Nguyễn Tất Thành, Liên Chiểu *(Gần biển, tiện di chuyển sân bay)*");

                suggestions.Add("Thủ tục nhận phòng cần giấy tờ gì?");
                suggestions.Add("Tìm phòng ở cơ sở Hải Châu view sông Hàn");
                suggestions.Add("Các phương thức thanh toán tiền phòng?");
            }
            // 4. Thanh toán (Payment)
            else if (lower.Contains("thanh toán") || lower.Contains("chuyển khoản") || lower.Contains("tiền mặt") || lower.Contains("vnpay")
                || lower.Contains("trả tiền") || lower.Contains("quẹt thẻ") || lower.Contains("tại quầy"))
            {
                sb.AppendLine("💳 **Các phương thức thanh toán linh hoạt tại HBS Hotel:**");
                sb.AppendLine("1. **Thanh toán ngay trực tuyến (Pay Now):** Hỗ trợ thanh toán bảo mật qua VNPAY, quét mã QR ngân hàng hoặc thẻ ATM / Visa / Mastercard.");
                sb.AppendLine("2. **Thanh toán tại quầy khi nhận phòng (Pay on Arrival):** Quý khách có thể giữ phòng trước và thanh toán bằng tiền mặt hoặc quẹt thẻ khi làm thủ tục check-in tại quầy lễ tân.");
                sb.AppendLine();
                sb.AppendLine("💡 *Mẹo: Quý khách hãy nhập mã giảm giá khi thanh toán để được trừ trực tiếp vào hóa đơn!*");

                suggestions.Add("Mã giảm giá đang có là gì?");
                suggestions.Add("Hủy phòng có được hoàn tiền không?");
                suggestions.Add("Thủ tục nhận phòng cần gì?");
            }
            // 5. Hủy phòng / Đổi lịch / Hoàn tiền
            else if (lower.Contains("hủy phòng") || lower.Contains("đổi ngày") || lower.Contains("hoàn tiền") || lower.Contains("hủy đơn")
                || lower.Contains("chính sách hủy") || lower.Contains("đổi lịch"))
            {
                sb.AppendLine("🔄 **Chính sách Hủy phòng & Đổi ngày lưu trú:**");
                sb.AppendLine("• **Hủy phòng miễn phí:** Nếu quý khách gửi yêu cầu hủy trước **24 giờ** tính đến ngày nhận phòng.");
                sb.AppendLine("• **Hủy sát giờ hoặc không đến (No-show):** Khách sạn sẽ thu phí đêm đầu tiên hoặc 50% tổng đơn tùy chính sách hạng phòng.");
                sb.AppendLine("• **Cách thực hiện hủy:** Quý khách vào mục **\"Đơn của tôi\"** trên thanh menu -> chọn đơn cần hủy -> nhấn nút **\"Yêu cầu hủy phòng\"**, hoặc gọi trực tiếp Hotline **1900 8888**.");
                sb.AppendLine("• **Thời gian hoàn tiền:** Tiền hoàn sẽ được chuyển về tài khoản ban đầu trong vòng 3 - 5 ngày làm việc.");

                suggestions.Add("Số điện thoại lễ tân hỗ trợ đổi ngày?");
                suggestions.Add("Thủ tục nhận phòng và trả phòng?");
                suggestions.Add("Tìm phòng trống giá tốt");
            }
            // 6. Phụ thu & Số lượng khách
            else if (lower.Contains("phụ thu") || lower.Contains("thêm người") || lower.Contains("vượt số") || lower.Contains("ở ghép")
                || lower.Contains("trẻ em") || lower.Contains("mấy người"))
            {
                sb.AppendLine("👨‍👩‍👧‍👦 **Chính sách Số lượng khách & Phụ thu quá số người:**");
                sb.AppendLine("• **Phòng Standard:** Tiêu chuẩn cho **2 người lớn**, tối đa 2 người (không kê thêm giường).");
                sb.AppendLine("• **Phòng Deluxe:** Gồm **2 khách cơ bản**, cho phép ở tối đa **3 khách** (Phụ thu khách thứ 3: **150.000 VNĐ/đêm**).");
                sb.AppendLine("• **Phòng Suite (Gia đình):** Gồm **2 - 4 khách cơ bản**, cho phép ở tối đa **4 - 5 khách** (Phụ thu khách thêm: **250.000 VNĐ/đêm**).");
                sb.AppendLine("• **Trẻ em:** Miễn phí cho 01 trẻ em dưới 6 tuổi ngủ chung giường với bố mẹ.");

                suggestions.Add("Gợi ý phòng Suite cho gia đình 4 người");
                suggestions.Add("Gợi ý phòng Deluxe ở Mỹ Khê");
                suggestions.Add("Mã giảm giá đang áp dụng?");
            }
            // 7. Mã giảm giá (Promo codes)
            else if (lower.Contains("mã giảm") || lower.Contains("khuyến mãi") || lower.Contains("voucher") || lower.Contains("promo")
                || lower.Contains("discount") || lower.Contains("ưu đãi") || lower.Contains("code"))
            {
                sb.AppendLine("🎁 **Các mã ưu đãi đặc quyền đang có hiệu lực tại HBS:**");
                sb.AppendLine("• Mã **WELCOME10**: Giảm ngay **10%** tổng hóa đơn tiền phòng cho khách hàng mới.");
                sb.AppendLine("• Mã **GIAM100**: Giảm ngay **100.000 VNĐ** trực tiếp cho mỗi đơn đặt phòng.");
                sb.AppendLine();
                sb.AppendLine("👉 *Cách dùng: Khi đặt phòng ở bước xác nhận, quý khách chỉ cần nhập mã vào ô \"Mã giảm giá\" và nhấn Áp dụng.*");

                suggestions.Add("Tìm phòng ở Mỹ Khê giảm giá");
                suggestions.Add("Các bước đặt phòng");
                suggestions.Add("Giờ check-in và check-out");
            }
            // 8. Nếu là tìm phòng
            else if (recommendedRooms.Count > 0)
            {
                sb.AppendLine($"✨ Em đã tìm thấy **{recommendedRooms.Count} phòng** rất phù hợp với yêu cầu của quý khách:");
                sb.AppendLine("Quý khách có thể xem nhanh thông tin từng phòng bên dưới và nhấn **\"Xem phòng\"** để đặt ngay:");
                sb.AppendLine();

                foreach (var rm in recommendedRooms)
                {
                    sb.AppendLine($"• **Phòng {rm.RoomNumber}** ({rm.TypeName} - {rm.PropertyName}): {rm.BasePrice:N0}đ/đêm · Tối đa {rm.MaxGuests} khách. *{rm.Description}*");
                }

                suggestions.Add("Thủ tục đặt phòng và thanh toán?");
                suggestions.Add("Phòng này có bồn tắm hay view biển không?");
                suggestions.Add("Giờ nhận phòng và trả phòng?");
            }
            // 9. Lời chào & Mặc định
            else
            {
                sb.AppendLine("Dạ xin chào quý khách! Em là **Trợ lý AI của HBS Luxury Hotel**.");
                sb.AppendLine("Em luôn sẵn sàng hỗ trợ quý khách 24/7 với các thông tin sau:");
                sb.AppendLine();
                sb.AppendLine("• 🔑 **Thủ tục nhận phòng (Check-in 14:00) & Trả phòng (Check-out 12:00)**");
                sb.AppendLine("• 📝 **Hướng dẫn các bước đặt phòng & Phương thức thanh toán**");
                sb.AppendLine("• 📞 **Thông tin liên hệ Hotline lễ tân và địa chỉ 5 cơ sở Đà Nẵng**");
                sb.AppendLine("• 🔍 **Tìm kiếm & Gợi ý phòng theo yêu cầu (view biển, bồn tắm, số người, cơ sở)**");
                sb.AppendLine("• 🎁 **Mã giảm giá, chính sách hủy phòng và phụ thu quá số người**");
                sb.AppendLine();
                sb.AppendLine("Quý khách cần em tư vấn cụ thể vấn đề nào ạ?");

                suggestions.AddRange(await GetInitialSuggestionsAsync());
            }

            return new ChatResponseDto
            {
                Reply = sb.ToString().Trim(),
                SuggestedQuestions = suggestions.Distinct().Take(4).ToList(),
                RecommendedRooms = recommendedRooms
            };
        }

        private List<string> GenerateContextualSuggestions(string lower, bool hasRooms)
        {
            if (hasRooms)
            {
                return new List<string>
                {
                    "Thủ tục nhận phòng cần những gì?",
                    "Các bước đặt phòng và thanh toán?",
                    "Mã giảm giá đang có là gì?"
                };
            }

            return new List<string>
            {
                "Giờ nhận phòng và trả phòng?",
                "Tìm phòng view biển ở Mỹ Khê",
                "Số điện thoại lễ tân hỗ trợ 24/7"
            };
        }

        private async Task<string?> CallGeminiAsync(
            string apiKey,
            string userMessage,
            List<ChatMessageDto>? history,
            List<ChatRoomRecommendationDto> recommendedRooms)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={apiKey}";

            var systemPrompt = @"Bạn là Trợ lý AI thông minh và lịch thiệp của hệ thống khách sạn HBS Luxury Hotel tại Đà Nẵng.
Nhiệm vụ của bạn là tư vấn, giải đáp thắc mắc và hỗ trợ khách hàng về:
1. Thủ tục nhận phòng (Check-in từ 14:00, cần CCCD/Hộ chiếu/VNeID, hỗ trợ nhận phòng sớm nếu trống, giữ hành lý miễn phí).
2. Thủ tục trả phòng (Check-out trước 12:00 trưa, trả phòng muộn phụ thu theo quy định).
3. Hướng dẫn đặt phòng trực tuyến và thanh toán (Thanh toán ngay qua QR/VNPAY hoặc Thanh toán tại quầy lễ tân khi nhận phòng).
4. Mã giảm giá: WELCOME10 (giảm 10%), GIAM100 (giảm 100k).
5. Thông tin liên hệ: Hotline 1900 8888 hoặc 0236 3888 999 (24/7), email support@hbshotel.vn.
6. 5 cơ sở tại Đà Nẵng: HBS Mỹ Khê (sát biển), HBS Sơn Trà (view biển và núi), HBS Hải Châu (trung tâm, sông Hàn), HBS Ngũ Hành Sơn (gần Non Nước), HBS Liên Chiểu (gần biển phía bắc).
7. Gợi ý phòng phù hợp với số khách, sở thích (Standard: 2 khách; Deluxe: 2-3 khách; Suite: 4-5 khách).

Phong cách trả lời: Thân thiện, lịch sự, hiếu khách, rõ ràng, gạch đầu dòng các ý chính bằng tiếng Việt. Nếu có phòng gợi ý, hãy khéo léo giới thiệu để khách bấm xem.";

            var contents = new List<object>();

            if (history != null && history.Count > 0)
            {
                foreach (var h in history.TakeLast(6))
                {
                    contents.Add(new
                    {
                        role = h.Role == "assistant" ? "model" : "user",
                        parts = new[] { new { text = h.Content } }
                    });
                }
            }

            var promptWithContext = userMessage;
            if (recommendedRooms.Count > 0)
            {
                var roomSummary = string.Join("; ", recommendedRooms.Select(r => $"Phòng {r.RoomNumber} ({r.TypeName} tại {r.PropertyName}, giá {r.BasePrice:N0}đ/đêm, tối đa {r.MaxGuests} khách)"));
                promptWithContext += $"\n[Dữ liệu phòng tìm được trong hệ thống: {roomSummary}]";
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = promptWithContext } }
            });

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = contents,
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 800
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, jsonContent);
            if (!response.IsSuccessStatusCode) return null;

            var resBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resBody);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text;
        }
    }
}
