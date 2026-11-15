using System.Collections.Generic;

namespace HotelBooking.Application.DTOs
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = "user"; // "user" | "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessageDto>? History { get; set; }
    }

    public class ChatRoomRecommendationDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public int MaxGuests { get; set; }
        public int IncludedGuests { get; set; }
        public decimal ExtraGuestFee { get; set; }
        public string? PropertyName { get; set; }
        public string? Area { get; set; }
        public string? Address { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public string? Description { get; set; }
    }

    public class ChatResponseDto
    {
        public string Reply { get; set; } = string.Empty;
        public List<string> SuggestedQuestions { get; set; } = new();
        public List<ChatRoomRecommendationDto> RecommendedRooms { get; set; } = new();
    }
}
