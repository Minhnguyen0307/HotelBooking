using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Application.DTOs;

namespace HotelBooking.Application.Interfaces
{
    public interface IChatBotService
    {
        Task<ChatResponseDto> ProcessChatAsync(ChatRequestDto request);
        Task<List<string>> GetInitialSuggestionsAsync();
    }
}
