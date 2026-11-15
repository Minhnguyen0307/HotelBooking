using System.Threading.Tasks;
using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Api
{
    [ApiController]
    [Route("api/chat")]
    public class ChatApiController : ControllerBase
    {
        private readonly IChatBotService _chatBotService;

        public ChatApiController(IChatBotService chatBotService)
        {
            _chatBotService = chatBotService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                var initial = await _chatBotService.GetInitialSuggestionsAsync();
                return Ok(new ChatResponseDto
                {
                    Reply = "Chào quý khách! Em có thể giúp gì cho quý khách về thông tin phòng hoặc dịch vụ tại HBS Hotel?",
                    SuggestedQuestions = initial
                });
            }

            var response = await _chatBotService.ProcessChatAsync(request);
            return Ok(response);
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions()
        {
            var list = await _chatBotService.GetInitialSuggestionsAsync();
            return Ok(list);
        }
    }
}
